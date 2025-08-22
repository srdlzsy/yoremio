using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;

namespace Infrastructure
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddOpenApi(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Yoremio API",
                    Version = "v1",
                    Description = "Yoremio API Swagger Documentation"
                });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Bearer token kullanımı",
                };
                c.AddSecurityDefinition("Bearer", securityScheme);

                // Burada Swagger UI'da token girmek için gereken ayar
                var securityRequirement = new OpenApiSecurityRequirement
                {
                    {
                        securityScheme, new List<string>()
                    }
                };
                c.AddSecurityRequirement(securityRequirement);

                // (İstersen) Controller bazında [Authorize] olan endpointlere işaret koyması için
                c.OperationFilter<SwaggerAuthorizeCheckOperationFilter>();
            });

            return services;
        }

        public static IApplicationBuilder MapOpenApi(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Yoremio API v1");
                c.RoutePrefix = string.Empty;
            });

            return app;
        }
    }
}
