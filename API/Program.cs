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
    .ValidateOnStart();

builder.Services.AddInfrastructure(configuration);
builder.Services.AddApplicationModule(configuration);
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 8 * 1024;
});
builder.Services.AddControllers();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SaticiPolicy", policy => policy.RequireRole(ApplicationRoles.Satici));
    options.AddPolicy("AliciPolicy", policy => policy.RequireRole(ApplicationRoles.Alici));
});

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:4200", "https://localhost:4200" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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
