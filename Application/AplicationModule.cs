using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationModule
{
    public static void AddApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

        // Tüm validator'ları bulunduğu assembly'den otomatik ekler
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

    }
}
