using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Hubs;
using Infrastructure.Options;
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

            services.Configure<SmtpEmailOptions>(configuration.GetSection("Email:Smtp"));
            services.Configure<TwilioSmsOptions>(configuration.GetSection("Sms:Twilio"));
            services.Configure<VerificationOptions>(configuration.GetSection("Verification"));
            // Generic Repository & Service (her Entity için)
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

            // Özel Repository & Service
            services.AddScoped<ISaticiProfiliRepository, SaticiProfiliRepository>();
            services.AddScoped<ISaticiProfiliService, SaticiProfiliService>();
            services.AddScoped<IUrunRepository, UrunRepository>();
            services.AddScoped<IUrunFavoriRepository, UrunFavoriRepository>();
            services.AddScoped<ITalepRepository, TalepRepository>();
            services.AddScoped<IYorumRepository, YorumRepository>();
            services.AddScoped<IPuanRepository, PuanRepository>();



            services.AddScoped<IKategoriRepository, KategoriRepository>();
            services.AddScoped<IKategoriService, KategoriService>();

            services.AddScoped<IAuthService, AuthService>();
            // Uygulama servisleri
            services.AddScoped<IEmailSend, EmailSender>();
            services.AddHttpClient<ISmsSender, SmsSender>();
            services.AddScoped<IUrunService, UrunService>();
            services.AddScoped<ITalepService, TalepService>();
            services.AddScoped<IDosyaKaydetService, DosyaKaydetService>();
            services.AddScoped<IYorumServices, YorumServices>();
            services.AddScoped<IPuanService, PuanService>();
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            return services;
        }
    }
}
