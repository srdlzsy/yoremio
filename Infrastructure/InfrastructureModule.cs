using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Hubs;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<YoremioContext>(options =>

                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Identity
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<YoremioContext>()
                .AddDefaultTokenProviders();
            // Generic Repository & Service (her Entity için)
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

            // Özel Repository & Service
            services.AddScoped<ISaticiProfiliRepository, SaticiProfiliRepository>();
            services.AddScoped<ISaticiProfiliService, SaticiProfiliService>();
            services.AddScoped<IUrunRepository, UrunRepository>();
            services.AddScoped<IYorumRepository, YorumRepository>();
            services.AddScoped<IPuanRepository, PuanRepository>();



            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IServiceManager, ServiceManager>();
            // Uygulama servisleri
            services.AddScoped<IEmailSend, EmailSender>();  // Gerçek ya da sahte implementasyon
            services.AddScoped<ISmsSender, SmsSender>();      // Gerçek ya da sahte implementasyon
            services.AddScoped<IUrunService, UrunService>();
            services.AddScoped<IDosyaKaydetService, DosyaKaydetService>();
            services.AddScoped<IYorumServices, YorumServices>();
            services.AddScoped<IPuanService, PuanService>();

            // Diğer servisler (repo, servis vs) buraya eklenebilir
            // services.AddScoped<IProductRepository, ProductRepository>();

            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();


            return services;
        }
    }
}
