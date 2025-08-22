using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

public class SwaggerAuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = (context.MethodInfo?.DeclaringType?.GetCustomAttributes(true)
                            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                            .Any() ?? false)
                         || (context.MethodInfo?.GetCustomAttributes(true)
                            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                            .Any() ?? false);

        if (!hasAuthorize) return;

        if (operation.Security == null)
        {
            operation.Security = new List<OpenApiSecurityRequirement>();
        }

        var bearerScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [bearerScheme] = new string[] { }
        });
    }
}
