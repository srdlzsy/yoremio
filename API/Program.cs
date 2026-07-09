using API.Middlewares;
using API.Options;
using Application;
using Domain.Constants;
using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
LoadLocalEnvironmentFile();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;
    options.RequestHeaders.Add("X-Correlation-Id");
    options.ResponseHeaders.Add("X-Correlation-Id");
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Gecersiz deger." : e.ErrorMessage));

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Dogrulama hatasi olustu.",
            data = (object?)null,
            errors,
            traceId = context.HttpContext.TraceIdentifier
        });
    };
});

builder.Services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key), "Jwt:Key ayari bos olamaz.")
    .Validate(
        options => builder.Environment.IsDevelopment() ||
                   !options.Key.Contains("super-secret", StringComparison.OrdinalIgnoreCase),
        "Production ortaminda Jwt:Key appsettings icindeki varsayilan deger olamaz.")
    .Validate(
        options => builder.Environment.IsDevelopment() ||
                   !options.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase),
        "Production ortaminda Jwt:Key placeholder deger olamaz.")
    .ValidateOnStart();

if (!builder.Environment.IsDevelopment())
{
    var defaultConnection = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(defaultConnection) ||
        defaultConnection.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Production ortaminda ConnectionStrings:DefaultConnection gercek secret/config ile verilmelidir.");
    }

    ValidateProductionVerificationConfiguration(configuration);
}

builder.Services.AddInfrastructure(configuration);
builder.Services.AddApplicationModule(configuration);
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 8 * 1024;
});
builder.Services.AddControllers();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole(ApplicationRoles.Admin));
    options.AddPolicy("SaticiPolicy", policy => policy.RequireRole(ApplicationRoles.Satici));
    options.AddPolicy("AliciPolicy", policy => policy.RequireRole(ApplicationRoles.Alici));
});

var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var defaultAllowedOrigins = new[]
{
    "http://localhost:4200",
    "https://localhost:4200",
    "http://localhost:5173",
    "https://localhost:5173",
    "http://localhost:3000",
    "https://localhost:3000",
    "https://yoremio.vercel.app",
    "https://www.yoremio.com",
    "https://yoremio.com"
};

var allowedOrigins = configuredOrigins
    .Concat(defaultAllowedOrigins)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(NormalizeCorsOrigin)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var allowedVercelPreviewHostSuffixes = (configuration.GetSection("Cors:AllowedVercelPreviewHostSuffixes").Get<string[]>() ?? Array.Empty<string>())
    .Concat(new[] { "-serdals-projects-e9817ac1.vercel.app" })
    .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
    .Select(suffix => suffix.Trim().TrimStart('*').ToLowerInvariant())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => IsAllowedCorsOrigin(origin, allowedOrigins, allowedVercelPreviewHostSuffixes))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var rateLimitPermitLimit = configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 300;
var rateLimitWindowSeconds = configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitPermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt ayarlari yuklenemedi.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

SwaggerExtensions.AddOpenApi(builder.Services);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000;
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var jwtSettings = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

logger.LogInformation(
    "API baslatiliyor. Ortam: {Environment}, Issuer: {Issuer}, Audience: {Audience}",
    app.Environment.EnvironmentName,
    jwtSettings.Issuer,
    jwtSettings.Audience);
logger.LogInformation("CORS izinli originler: {AllowedOrigins}", string.Join(", ", allowedOrigins));
logger.LogInformation("CORS izinli Vercel preview host suffixleri: {AllowedVercelPreviewHostSuffixes}", string.Join(", ", allowedVercelPreviewHostSuffixes));

var applyMigrations = configuration.GetValue<bool?>("Startup:ApplyMigrations") ?? app.Environment.IsDevelopment();
var seedSampleData = configuration.GetValue<bool?>("Startup:SeedSampleData") ?? app.Environment.IsDevelopment();

using (var scope = app.Services.CreateScope())
{
    await YoremioStartupInitializer.InitializeAsync(
        scope.ServiceProvider,
        logger,
        applyMigrations: applyMigrations,
        seedSampleData: seedSampleData);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(GlobalExceptionMiddleware.HandleAsync);
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<RequestContextMiddleware>();
app.UseHttpLogging();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    await next();
});

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/chathub").RequireAuthorization();
app.MapControllers();

app.Run();

static string NormalizeCorsOrigin(string origin)
{
    return origin.Trim().TrimEnd('/');
}

static void LoadLocalEnvironmentFile()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory != null)
    {
        var envPath = Path.Combine(directory.FullName, ".env.local");
        if (File.Exists(envPath))
        {
            foreach (var rawLine in File.ReadLines(envPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                {
                    line = line["export ".Length..].TrimStart();
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value[1..^1];
                }

                Environment.SetEnvironmentVariable(key, value.Trim());
            }

            return;
        }

        directory = directory.Parent;
    }
}

static bool IsAllowedCorsOrigin(string origin, string[] allowedOrigins, string[] allowedVercelPreviewHostSuffixes)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    var normalizedOrigin = NormalizeCorsOrigin(origin);
    if (allowedOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri) ||
        !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    return uri.Host.StartsWith("yoremio-", StringComparison.OrdinalIgnoreCase) &&
           allowedVercelPreviewHostSuffixes.Any(suffix => uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}

static void ValidateProductionVerificationConfiguration(IConfiguration configuration)
{
    var requireEmail = configuration.GetValue<bool>("Verification:RequireConfirmedEmailForSellerLogin");

    if (requireEmail && configuration.GetValue<bool>("Email:Smtp:UseMockSender"))
    {
        throw new InvalidOperationException("Production ortaminda email dogrulamasi zorunluysa Email:Smtp:UseMockSender=false olmalidir.");
    }

    if (requireEmail &&
        (IsMissingOrPlaceholder(configuration["Email:Smtp:Host"]) ||
         IsMissingOrPlaceholder(configuration["Email:Smtp:UserName"]) ||
         IsMissingOrPlaceholder(configuration["Email:Smtp:Password"]) ||
         IsMissingOrPlaceholder(configuration["Email:Smtp:FromAddress"])))
    {
        throw new InvalidOperationException("Production ortaminda email dogrulamasi zorunluysa gercek Email:Smtp ayarlari verilmelidir.");
    }
}

static bool IsMissingOrPlaceholder(string? value)
{
    return string.IsNullOrWhiteSpace(value) ||
           value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("smtp.example.com", StringComparison.OrdinalIgnoreCase);
}
