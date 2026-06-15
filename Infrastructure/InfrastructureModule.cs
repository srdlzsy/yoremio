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
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection ayari bos olamaz.");
            }

            services.AddDbContext<YoremioContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
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
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();



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
            services.AddScoped<IChatService, ChatService>();
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            return services;
        }
    }
}
