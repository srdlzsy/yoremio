using Application;
using Infrastructure;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


// JWT claim mapping'ini temizle
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddInfrastructure(configuration);
builder.Services.AddApplicationModule(configuration); // << BURADA ÇAÐIRIYORSUN

builder.Services.AddSignalR();

builder.Services.AddControllers();

// Authentication & JWT Bearer ayarlarý
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

 .AddJwtBearer(options =>
 {
     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateLifetime = true,
         ValidateIssuerSigningKey = true,
         // BURASI ÖNEMLÝ
         NameClaimType = ClaimTypes.NameIdentifier, // ??
         ValidIssuer = configuration["Jwt:Issuer"],
         ValidAudience = configuration["Jwt:Audience"],
         IssuerSigningKey = new SymmetricSecurityKey(
             Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
     };


     // SignalR için WebSocket baðlantýlarýnda token okuma
     options.Events = new JwtBearerEvents
     {
         OnMessageReceived = context =>
         {
             var accessToken = context.Request.Query["access_token"];
             var path = context.HttpContext.Request.Path;

             if (!string.IsNullOrEmpty(accessToken) &&
                 path.StartsWithSegments("/chathub"))
             {
                 context.Token = accessToken;
             }
             return Task.CompletedTask;
         }
     };
 });

// CORS Politikasý (Güvenlik Ayarlarý)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
// Swagger OpenAPI ayarlarý (extension'dan geliyor)
Infrastructure.SwaggerExtensions.AddOpenApi(builder.Services);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();

app.UseCors("AllowAll");       // BURASI HTTPS'den ÖNCE OLMALI

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();