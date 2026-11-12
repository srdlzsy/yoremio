using API.Options;
using Application;
using Application.DTOs;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class EfCoreAndDiGuardsTests
{
    private const string DummyConnectionString = "Host=localhost;Port=5432;Database=yoremio_query_check;Username=postgres;Password=postgres";

    public static IReadOnlyCollection<(string Name, Func<Task> Test)> GetTests()
    {
        return new (string Name, Func<Task> Test)[]
        {
            ("Tum EF Core repository ve AdminService sorgulari PostgreSQL uyumlu ve Include uyarisiz derlenmeli", AllRepositoryAndAdminQueries_Should_CompileWithoutWarnings),
            ("YoremioContext modeli son EF Core migration snapshot ile uyumlu olmali", EfCoreModel_Should_HaveNoPendingChanges),
            ("DI konteyneri tum API controllerlarini, ChatHub, repository ve servisleri hatasiz cozmeli", DependencyInjection_Should_ResolveAllControllersHubsAndServices)
        };
    }

    private static YoremioContext CreateCompileOnlyContext()
    {
        var options = new DbContextOptionsBuilder<YoremioContext>()
            .UseNpgsql(DummyConnectionString)
            .Options;

        return new YoremioContext(options);
    }

    private static Task AllRepositoryAndAdminQueries_Should_CompileWithoutWarnings()
    {
        using var db = CreateCompileOnlyContext();

        // 1. TalepRepository: tum detay, listeleme ve teklif sorgulari (eski NavigationBaseIncludeIgnored regresyonu dahil)
        var talepRepo = new TalepRepository(db);
        AssertCompiles(talepRepo.BuildDetailsQuery(false).Where(t => t.Id == 1));
        AssertCompiles(talepRepo.BuildDetailsQuery(true).Where(t => t.Id == 1));
        AssertCompiles(talepRepo.BuildAliciTalepleriQuery("buyer-1", TalepDurumlari.Acik));
        AssertCompiles(talepRepo.BuildAliciTalepleriQuery("buyer-1", null));
        AssertCompiles(talepRepo.BuildSaticiTalepleriQuery("seller-1", TalepDurumlari.Anlasildi));
        AssertCompiles(talepRepo.BuildAcikPazarForSellerQuery("seller-1", null));
        AssertCompiles(talepRepo.BuildTeklifDetailsQuery().Where(t => t.Id == 1));

        // 2. UrunRepository: detay, oneri ve tum filtre + 12 siralama secenegi
        var urunRepo = new UrunRepository(db);
        AssertCompiles(urunRepo.BuildDetailsQuery(asNoTracking: false, includeRatingUsers: false).Where(u => u.Id == 1));
        AssertCompiles(urunRepo.BuildDetailsQuery(asNoTracking: true, includeRatingUsers: false).Where(u => u.Id == 1));
        AssertCompiles(urunRepo.BuildDetailsQuery(asNoTracking: true, includeRatingUsers: true).Where(u => u.Id == 1));

        var sortModes = new[]
        {
            "relevance",
            "price_asc",
            "price_desc",
            "name_asc",
            "name_desc",
            "top_rated",
            "most_reviewed",
            "most_favorited",
            "most_viewed",
            "newest",
            "oldest",
            ""
        };

        foreach (var sort in sortModes)
        {
            AssertCompiles(urunRepo.BuildSearchQuery(new UrunQuery
            {
                Q = "Posof göğermiş peyniri",
                SaticiId = "seller-1",
                Sehir = "Ardahan",
                Ilce = "Posof",
                UretimSehir = "Ardahan",
                TeslimatSehir = "Bursa",
                KategoriId = 1,
                MinFiyat = 100m,
                MaxFiyat = 1000m,
                SadeceAktif = true,
                SadeceStoktaOlanlar = true,
                SadeceYeniHasat = true,
                SadeceEldenTeslim = true,
                SadeceKargolu = true,
                SadeceDogrulanmisSatici = true,
                MinOrtalamaPuan = 4.0,
                Sort = sort,
                Page = 1,
                PageSize = 12
            }));
        }

        // relevance siralama TeslimatSehir olmadan da derlenmeli
        AssertCompiles(urunRepo.BuildSearchQuery(new UrunQuery { Q = "bal", Sort = "relevance" }));
        AssertCompiles(urunRepo.BuildRecommendedQuery(new[] { 1, 2 }, new[] { 99 }, "Bursa", "Nilüfer"));
        AssertCompiles(urunRepo.BuildRecommendedQuery(Array.Empty<int>(), Array.Empty<int>(), null, null));

        // 3. OrtakAlimRepository
        var ortakAlimRepo = new OrtakAlimRepository(db);
        AssertCompiles(ortakAlimRepo.BuildDetailsQuery(false).Where(o => o.Id == 1));
        AssertCompiles(ortakAlimRepo.BuildDetailsQuery(true).Where(o => o.Id == 1));
        AssertCompiles(ortakAlimRepo.BuildFilterQuery("Bursa", "Ardahan", "peynir", OrtakAlimDurumlari.Acik));
        AssertCompiles(ortakAlimRepo.BuildMatchingOpenGroupBuysQuery("peynir", "Bursa"));

        // 4. PlanliGonderimRepository
        var planliRepo = new PlanliGonderimRepository(db);
        AssertCompiles(planliRepo.BuildDetailsQuery().Where(p => p.Id == 1));
        AssertCompiles(planliRepo.BuildUpcomingQuery("Bursa", "Ardahan", "seller-1"));

        // 5. SaticiProfiliRepository & SaticiTakipRepository
        var saticiRepo = new SaticiProfiliRepository(db);
        AssertCompiles(saticiRepo.BuildPublicDetailsQuery().Where(p => p.KullaniciId == "seller-1" && p.AktifMi));
        AssertCompiles(saticiRepo.BuildFeaturedSellersQuery(24));

        var takipRepo = new SaticiTakipRepository(db);
        AssertCompiles(takipRepo.BuildByAliciQuery("buyer-1"));

        // 6. Chat, Alarm, Favori ve Diger Repository sorgulari
        var chatRepo = new ChatMessageRepository(db);
        AssertCompiles(chatRepo.BuildRecentMessagesQuery("user-1"));
        AssertCompiles(chatRepo.BuildConversationMessagesQuery("u1", "u2", urunId: 5, talepId: 10));

        var fiyatAlarmRepo = new FiyatAlarmiRepository(db);
        AssertCompiles(fiyatAlarmRepo.BuildByAliciWithProductQuery("buyer-1"));

        var stokAlarmRepo = new StokAlarmiRepository(db);
        AssertCompiles(stokAlarmRepo.BuildByAliciWithProductQuery("buyer-1"));

        // 7. AdminService sorgulari
        var adminService = new AdminService(db, new NoopBildirimService());
        AssertCompiles(adminService.BuildKullanicilarQuery(new AdminKullaniciQuery
        {
            Q = "mandira",
            Rol = ApplicationRoles.Satici,
            SadeceBekleyenSaticilar = true
        }));
        AssertCompiles(adminService.BuildUrunlerQuery(new AdminUrunQuery
        {
            Q = "peynir",
            KategoriId = 1,
            SaticiId = "seller-1",
            AktifMi = true
        }));
        AssertCompiles(adminService.BuildAdminUrunDetailsQuery(asNoTracking: false).Where(u => u.Id == 1));

        // 8. Ek iliski sorgulari (Kategori / Yorum / Puan / Favori)
        AssertCompiles(db.Set<UrunFavori>().AsNoTracking().Include(x => x.Urun).Where(x => x.KullaniciId == "buyer-1"));
        AssertCompiles(db.Set<Yorum>().AsNoTracking().Include(y => y.Urun).Include(y => y.YorumYapanKullanici).Where(y => y.UrunId == 1));
        AssertCompiles(db.Set<Puan>().AsNoTracking().Include(p => p.Kullanici).Where(p => p.UrunId == 1));

        return Task.CompletedTask;
    }

    private static Task EfCoreModel_Should_HaveNoPendingChanges()
    {
        using var db = CreateCompileOnlyContext();
        if (db.Database.HasPendingModelChanges())
        {
            throw new InvalidOperationException(
                "YoremioContext modelinde henuz EF Core migration olusturulmamis bekleyen degisiklik var (HasPendingModelChanges == true).");
        }

        return Task.CompletedTask;
    }

    private static async Task DependencyInjection_Should_ResolveAllControllersHubsAndServices()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = DummyConnectionString,
            ["Jwt:Key"] = "test-only-super-long-jwt-signing-key-256-bits-min-1234567890",
            ["Jwt:Issuer"] = "yoremio-test",
            ["Jwt:Audience"] = "yoremio-test-client",
            ["Email:Smtp:Host"] = "localhost",
            ["Email:Smtp:Port"] = "1025",
            ["Cloudinary:Enabled"] = "false",
            ["Verification:CodeLength"] = "6"
        };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.Configuration.AddInMemoryCollection(configValues);
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplicationModule(builder.Configuration);
        builder.Services.AddSignalR();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(API.Controllers.UrunController).Assembly)
            .AddControllersAsServices();

        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var controllerTypes = typeof(API.Controllers.UrunController).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .ToArray();

        if (controllerTypes.Length == 0)
        {
            throw new InvalidOperationException("Hic API controller tipi bulunamadi.");
        }

        foreach (var controllerType in controllerTypes)
        {
            _ = sp.GetRequiredService(controllerType);
        }

        _ = ActivatorUtilities.CreateInstance<ChatHub>(sp);

        var domainInterfaces = typeof(ITalepRepository).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.Namespace == "Domain.Interfaces" && !t.IsGenericTypeDefinition)
            .ToArray();

        foreach (var repoInterface in domainInterfaces)
        {
            _ = sp.GetRequiredService(repoInterface);
        }

        var appInterfaces = typeof(Application.Interfaces.ITalepService).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.Namespace == "Application.Interfaces" && !t.IsGenericTypeDefinition)
            .ToArray();

        foreach (var serviceInterface in appInterfaces)
        {
            _ = sp.GetRequiredService(serviceInterface);
        }
    }

    private static void AssertCompiles<T>(IQueryable<T> query)
    {
        var sql = query.ToQueryString();
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException($"ToQueryString bos SQL uretti: {typeof(T).Name}");
        }
    }

    private sealed class NoopBildirimService : Application.Interfaces.IBildirimService
    {
        public Task<BildirimDto> BildirimOlusturAsync(BildirimOlusturDto dto) => Task.FromResult(new BildirimDto());
        public Task<BildirimPagedResultDto> GetBildirimlerAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize) => Task.FromResult(new BildirimPagedResultDto());
        public Task<int> GetOkunmamisSayisiAsync(string kullaniciId) => Task.FromResult(0);
        public Task<BildirimDto> OkunduIsaretleAsync(long bildirimId, string kullaniciId) => Task.FromResult(new BildirimDto());
        public Task<int> TumunuOkunduIsaretleAsync(string kullaniciId) => Task.FromResult(0);
    }
}
