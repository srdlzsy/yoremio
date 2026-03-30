using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    public static class YoremioStartupInitializer
    {
        public const string SeedPassword = "Test123!";

        public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger, bool seedSampleData, CancellationToken cancellationToken = default)
        {
            var context = serviceProvider.GetRequiredService<YoremioContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync(cancellationToken);
            await EnsureRolesAsync(roleManager, logger);

            if (!seedSampleData)
            {
                return;
            }

            await SeedSampleDataAsync(context, userManager, logger, cancellationToken);
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            foreach (var role in new[] { ApplicationRoles.Satici, ApplicationRoles.Alici })
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Rol olusturulamadi: {role}. {string.Join(" | ", result.Errors.Select(e => e.Description))}");
                }

                logger.LogInformation("Baslangic rolu olusturuldu: {Role}", role);
            }
        }

        private static async Task SeedSampleDataAsync(
            YoremioContext context,
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var categoryIds = await EnsureCategoriesAsync(context, cancellationToken);
            var sellers = await EnsureSellerUsersAsync(context, userManager, cancellationToken);
            var buyers = await EnsureBuyerUsersAsync(context, userManager, cancellationToken);
            var productIds = await EnsureProductsAsync(context, categoryIds, sellers, cancellationToken);

            await EnsureFavoritesAsync(context, buyers, productIds, cancellationToken);
            await EnsureRatingsAsync(context, buyers, productIds, cancellationToken);
            await EnsureCommentsAsync(context, buyers, productIds, cancellationToken);
            await EnsureDemandsAsync(context, sellers, buyers, productIds, cancellationToken);

            logger.LogInformation("Ornek development verileri hazirlandi. Satici: {SellerCount}, Alici: {BuyerCount}, Urun: {ProductCount}", sellers.Count, buyers.Count, productIds.Count);
        }

        private static async Task<Dictionary<string, int>> EnsureCategoriesAsync(YoremioContext context, CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new CategorySeed("Sebze", "Mevsiminde toplanmis organik sebzeler"),
                new CategorySeed("Meyve", "Taze ve dogal meyveler"),
                new CategorySeed("Sut Urunleri", "Gunluk sut, peynir ve yogurt cesitleri"),
                new CategorySeed("Bakliyat", "Katkisiz kuru gida urunleri"),
                new CategorySeed("Kahvaltilik", "Bal, recel, yumurta ve kahvaltilik urunler")
            };

            foreach (var seed in seeds)
            {
                var category = await context.Kategoriler.SingleOrDefaultAsync(x => x.Adi == seed.Name, cancellationToken);
                if (category == null)
                {
                    category = new Kategori
                    {
                        Adi = seed.Name,
                        Aciklama = seed.Description
                    };

                    context.Kategoriler.Add(category);
                }
                else
                {
                    category.Aciklama = seed.Description;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return await context.Kategoriler
                .Where(x => seeds.Select(s => s.Name).Contains(x.Adi))
                .ToDictionaryAsync(x => x.Adi, x => x.Id, cancellationToken);
        }

        private static async Task<Dictionary<string, ApplicationUser>> EnsureSellerUsersAsync(
            YoremioContext context,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new SellerSeed("ayse@demo.yoremio.local", "Ayse Ciftligi", "10000000001", "+905301000001", "Ardahan Merkez Kume Evleri", "Ardahan", "Merkez", new DateTime(2025, 8, 20, 9, 0, 0, DateTimeKind.Utc)),
                new SellerSeed("mehmet@demo.yoremio.local", "Posof Organik", "10000000002", "+905301000002", "Posof Yayla Yolu 14", "Ardahan", "Posof", new DateTime(2025, 9, 3, 9, 0, 0, DateTimeKind.Utc)),
                new SellerSeed("zeynep@demo.yoremio.local", "Velii Dogal Urunler", "10000000003", "+905301000003", "Gole Pazar Sokak 8", "Ardahan", "Gole", new DateTime(2025, 9, 18, 9, 0, 0, DateTimeKind.Utc))
            };

            var users = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in seeds)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    seed.Email,
                    seed.PhoneNumber,
                    ApplicationRoles.Satici,
                    emailConfirmed: true,
                    phoneConfirmed: true);

                var profile = await context.SaticiProfilleri.SingleOrDefaultAsync(x => x.KullaniciId == user.Id, cancellationToken);
                if (profile == null)
                {
                    profile = new SaticiProfili { KullaniciId = user.Id };
                    context.SaticiProfilleri.Add(profile);
                }

                profile.MagazaAdi = seed.StoreName;
                profile.VergiNo = seed.TaxNumber;
                profile.Adres = seed.Address;
                profile.Sehir = seed.City;
                profile.Ilce = seed.District;
                profile.AktifMi = true;
                profile.KayitTarihi = seed.CreatedAt;

                users[seed.Email] = user;
            }

            await context.SaveChangesAsync(cancellationToken);
            return users;
        }

        private static async Task<Dictionary<string, ApplicationUser>> EnsureBuyerUsersAsync(
            YoremioContext context,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new BuyerSeed("elif@demo.yoremio.local", "+905321000001", "Ardahan Merkez / Yeni Mahalle", "Sebze", new DateTime(2025, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
                new BuyerSeed("can@demo.yoremio.local", "+905321000002", "Kars Merkez / Istasyon", "Kahvaltilik", new DateTime(2025, 10, 7, 9, 0, 0, DateTimeKind.Utc)),
                new BuyerSeed("selin@demo.yoremio.local", "+905321000003", "Erzurum Yakutiye / Lalapasa", "Meyve", new DateTime(2025, 10, 15, 9, 0, 0, DateTimeKind.Utc))
            };

            var users = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in seeds)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    seed.Email,
                    seed.PhoneNumber,
                    ApplicationRoles.Alici,
                    emailConfirmed: true,
                    phoneConfirmed: true);

                var profile = await context.AliciProfilleri.SingleOrDefaultAsync(x => x.KullaniciId == user.Id, cancellationToken);
                if (profile == null)
                {
                    profile = new AliciProfili { KullaniciId = user.Id };
                    context.AliciProfilleri.Add(profile);
                }

                profile.Adres = seed.Address;
                profile.FavoriKategori = seed.FavoriteCategory;
                profile.AktifMi = true;
                profile.KayitTarihi = seed.CreatedAt;

                users[seed.Email] = user;
            }

            await context.SaveChangesAsync(cancellationToken);
            return users;
        }

        private static async Task<Dictionary<string, int>> EnsureProductsAsync(
            YoremioContext context,
            IReadOnlyDictionary<string, int> categoryIds,
            IReadOnlyDictionary<string, ApplicationUser> sellers,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new ProductSeed(
                    "Dag Cilegi Receli",
                    "Sekersiz, bakir kazanda kucuk partiler halinde hazirlanan dag cilegi receli.",
                    185m,
                    24,
                    "Kahvaltilik",
                    "ayse@demo.yoremio.local",
                    DemoImageUrls("dag-cilegi-receli", 3),
                    DemoVideoUrls("dag-cilegi-receli", 1),
                    new DateTime(2026, 2, 6, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Yayla Bali",
                    "Posof yaylalarindan toplanan cicek bali, yogun aroma ve koyu kivam.",
                    420m,
                    12,
                    "Kahvaltilik",
                    "mehmet@demo.yoremio.local",
                    DemoImageUrls("yayla-bali", 4),
                    DemoVideoUrls("yayla-bali", 1),
                    new DateTime(2026, 2, 8, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Koy Yumurtasi 30'lu",
                    "Serbest gezen tavuklardan gunluk toplanan kahverengi koy yumurtasi.",
                    210m,
                    30,
                    "Kahvaltilik",
                    "zeynep@demo.yoremio.local",
                    DemoImageUrls("koy-yumurtasi", 4),
                    DemoVideoUrls("koy-yumurtasi", 1),
                    new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Organik Karsari Peyniri",
                    "Uzun olgunlasma sureli, kahvaltilik ve izgara icin uygun tam yagli peynir.",
                    340m,
                    18,
                    "Sut Urunleri",
                    "ayse@demo.yoremio.local",
                    DemoImageUrls("organik-karsari-peyniri", 4),
                    DemoVideoUrls("organik-karsari-peyniri", 1),
                    new DateTime(2026, 2, 12, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Taze Kivircik Marul",
                    "Sabah erken saatte hasat edilmis gevrek kivircik marul.",
                    45m,
                    80,
                    "Sebze",
                    "mehmet@demo.yoremio.local",
                    DemoImageUrls("taze-kivircik-marul", 3),
                    DemoVideoUrls("taze-kivircik-marul", 1),
                    new DateTime(2026, 2, 14, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Ata Tohumu Domates",
                    "Yogun kokulu, ince kabuklu ata tohumu pembe domates.",
                    95m,
                    55,
                    "Sebze",
                    "ayse@demo.yoremio.local",
                    DemoImageUrls("ata-tohumu-domates", 4),
                    DemoVideoUrls("ata-tohumu-domates", 1),
                    new DateTime(2026, 2, 17, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Kurutulmus Elma Dilimi",
                    "Katkisiz, dusuk isida kurutulmus elma dilimleri; atistirmalik veya granola icin ideal.",
                    130m,
                    40,
                    "Meyve",
                    "zeynep@demo.yoremio.local",
                    DemoImageUrls("kurutulmus-elma-dilimi", 3),
                    DemoVideoUrls("kurutulmus-elma-dilimi", 1),
                    new DateTime(2026, 2, 19, 8, 0, 0, DateTimeKind.Utc)),
                new ProductSeed(
                    "Tas Degirmen Kirmizi Mercimek",
                    "Tas degirmende cekilmis, iri taneli ve katkisiz kirmizi mercimek.",
                    160m,
                    65,
                    "Bakliyat",
                    "mehmet@demo.yoremio.local",
                    DemoImageUrls("tas-degirmen-kirmizi-mercimek", 3),
                    DemoVideoUrls("tas-degirmen-kirmizi-mercimek", 1),
                    new DateTime(2026, 2, 22, 8, 0, 0, DateTimeKind.Utc))
            };

            var productIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in seeds)
            {
                var sellerId = sellers[seed.SellerEmail].Id;
                var categoryId = categoryIds[seed.CategoryName];

                var product = await context.Urunler
                    .Include(x => x.Resimler)
                    .Include(x => x.Videolar)
                    .SingleOrDefaultAsync(x => x.SaticiId == sellerId && x.Adi == seed.Name, cancellationToken);

                if (product == null)
                {
                    product = new Urun
                    {
                        SaticiId = sellerId,
                        Adi = seed.Name
                    };

                    context.Urunler.Add(product);
                }

                product.Aciklama = seed.Description;
                product.Fiyat = seed.Price;
                product.StokMiktari = seed.Stock;
                product.KategoriId = categoryId;
                product.AktifMi = true;
                product.OlusturmaTarihi = seed.CreatedAt;
                product.GuncellemeTarihi = seed.CreatedAt.AddDays(4);

                var obsoleteImages = product.Resimler
                    .Where(x => !seed.ImageUrls.Contains(x.Url, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (obsoleteImages.Count > 0)
                {
                    context.UrunResimler.RemoveRange(obsoleteImages);
                }

                var obsoleteVideos = product.Videolar
                    .Where(x => !seed.VideoUrls.Contains(x.Url, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (obsoleteVideos.Count > 0)
                {
                    context.UrunVideolar.RemoveRange(obsoleteVideos);
                }

                foreach (var imageUrl in seed.ImageUrls)
                {
                    if (product.Resimler.All(x => x.Url != imageUrl))
                    {
                        product.Resimler.Add(new UrunResim { Url = imageUrl });
                    }
                }

                foreach (var videoUrl in seed.VideoUrls)
                {
                    if (product.Videolar.All(x => x.Url != videoUrl))
                    {
                        product.Videolar.Add(new UrunVideo { Url = videoUrl });
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
                productIds[seed.Name] = product.Id;
            }

            return productIds;
        }

        private static async Task EnsureFavoritesAsync(
            YoremioContext context,
            IReadOnlyDictionary<string, ApplicationUser> buyers,
            IReadOnlyDictionary<string, int> productIds,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new FavoriteSeed("elif@demo.yoremio.local", "Ata Tohumu Domates"),
                new FavoriteSeed("elif@demo.yoremio.local", "Yayla Bali"),
                new FavoriteSeed("can@demo.yoremio.local", "Koy Yumurtasi 30'lu"),
                new FavoriteSeed("can@demo.yoremio.local", "Organik Karsari Peyniri"),
                new FavoriteSeed("selin@demo.yoremio.local", "Kurutulmus Elma Dilimi"),
                new FavoriteSeed("selin@demo.yoremio.local", "Dag Cilegi Receli")
            };

            foreach (var seed in seeds)
            {
                var userId = buyers[seed.BuyerEmail].Id;
                var productId = productIds[seed.ProductName];

                var favoriteExists = await context.UrunFavoriler
                    .AnyAsync(x => x.KullaniciId == userId && x.UrunId == productId, cancellationToken);

                if (!favoriteExists)
                {
                    context.UrunFavoriler.Add(new UrunFavori
                    {
                        KullaniciId = userId,
                        UrunId = productId,
                        EklenmeTarihi = DateTime.UtcNow.AddDays(-7)
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureRatingsAsync(
            YoremioContext context,
            IReadOnlyDictionary<string, ApplicationUser> buyers,
            IReadOnlyDictionary<string, int> productIds,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new RatingSeed("elif@demo.yoremio.local", "Ata Tohumu Domates", 5, new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("can@demo.yoremio.local", "Ata Tohumu Domates", 4, new DateTime(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("selin@demo.yoremio.local", "Dag Cilegi Receli", 5, new DateTime(2026, 3, 4, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("elif@demo.yoremio.local", "Yayla Bali", 5, new DateTime(2026, 3, 5, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("can@demo.yoremio.local", "Organik Karsari Peyniri", 4, new DateTime(2026, 3, 6, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("selin@demo.yoremio.local", "Kurutulmus Elma Dilimi", 5, new DateTime(2026, 3, 7, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("can@demo.yoremio.local", "Koy Yumurtasi 30'lu", 5, new DateTime(2026, 3, 8, 8, 0, 0, DateTimeKind.Utc)),
                new RatingSeed("elif@demo.yoremio.local", "Tas Degirmen Kirmizi Mercimek", 4, new DateTime(2026, 3, 9, 8, 0, 0, DateTimeKind.Utc))
            };

            foreach (var seed in seeds)
            {
                var userId = buyers[seed.BuyerEmail].Id;
                var productId = productIds[seed.ProductName];

                var rating = await context.Puanlar
                    .SingleOrDefaultAsync(x => x.KullaniciId == userId && x.UrunId == productId, cancellationToken);

                if (rating == null)
                {
                    rating = new Puan
                    {
                        KullaniciId = userId,
                        UrunId = productId
                    };

                    context.Puanlar.Add(rating);
                }

                rating.PuanDegeri = seed.Score;
                rating.PuanTarihi = seed.CreatedAt;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureCommentsAsync(
            YoremioContext context,
            IReadOnlyDictionary<string, ApplicationUser> buyers,
            IReadOnlyDictionary<string, int> productIds,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new CommentSeed("elif@demo.yoremio.local", "Ata Tohumu Domates", "Paketleme temizdi, domatesler kokusuyla bile farkini belli ediyor.", new DateTime(2026, 3, 2, 8, 30, 0, DateTimeKind.Utc)),
                new CommentSeed("can@demo.yoremio.local", "Ata Tohumu Domates", "Salatada kullandik, bekledigimizden daha lezzetli cikti.", new DateTime(2026, 3, 3, 8, 30, 0, DateTimeKind.Utc)),
                new CommentSeed("selin@demo.yoremio.local", "Dag Cilegi Receli", "Kahvaltida hizla bitti, seker yuklu olmayan recel arayanlar icin cok iyi.", new DateTime(2026, 3, 4, 8, 30, 0, DateTimeKind.Utc)),
                new CommentSeed("elif@demo.yoremio.local", "Yayla Bali", "Yogun aromali ve gercekten dogal hissettiriyor.", new DateTime(2026, 3, 5, 8, 30, 0, DateTimeKind.Utc)),
                new CommentSeed("can@demo.yoremio.local", "Organik Karsari Peyniri", "Hem tostta hem kahvaltida kullandik, kivami cok basarili.", new DateTime(2026, 3, 6, 8, 30, 0, DateTimeKind.Utc)),
                new CommentSeed("selin@demo.yoremio.local", "Kurutulmus Elma Dilimi", "Cocuklar icin saglikli atistirmalik oldu, tekrar alirim.", new DateTime(2026, 3, 7, 8, 30, 0, DateTimeKind.Utc))
            };

            foreach (var seed in seeds)
            {
                var userId = buyers[seed.BuyerEmail].Id;
                var productId = productIds[seed.ProductName];

                var exists = await context.Yorumlar.AnyAsync(
                    x => x.YorumYapanKullaniciId == userId && x.UrunId == productId && x.Icerik == seed.Content,
                    cancellationToken);

                if (!exists)
                {
                    context.Yorumlar.Add(new Yorum
                    {
                        YorumYapanKullaniciId = userId,
                        UrunId = productId,
                        Icerik = seed.Content,
                        Tarih = seed.CreatedAt
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureDemandsAsync(
            YoremioContext context,
            IReadOnlyDictionary<string, ApplicationUser> sellers,
            IReadOnlyDictionary<string, ApplicationUser> buyers,
            IReadOnlyDictionary<string, int> productIds,
            CancellationToken cancellationToken)
        {
            var demandSeeds = new[]
            {
                new DemandSeed("elif@demo.yoremio.local", "Yayla Bali", 3, "Kucuk kafe icin duzenli alim dusunuyorum, toplu fiyat bilgisi rica ederim.", new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc), "mehmet@demo.yoremio.local", 395m, "3 kavanoz ve uzeri icin indirim uygulayabilirim.", Domain.Constants.TalepDurumlari.Anlasildi, Domain.Constants.TalepTeklifDurumlari.Kabul),
                new DemandSeed("can@demo.yoremio.local", "Koy Yumurtasi 30'lu", 2, "Haftalik teslimat secenegi var mi?", new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc), "zeynep@demo.yoremio.local", 198m, "Her cuma teslim edecek sekilde planlayabiliriz.", Domain.Constants.TalepDurumlari.Acik, Domain.Constants.TalepTeklifDurumlari.Beklemede),
                new DemandSeed("selin@demo.yoremio.local", "Tas Degirmen Kirmizi Mercimek", 5, "Kargo ile 5 paket gonderim olur mu?", new DateTime(2026, 3, 14, 8, 0, 0, DateTimeKind.Utc), "mehmet@demo.yoremio.local", 150m, "Hafta ici kargoya veririm, 5 paket icin stok hazir.", Domain.Constants.TalepDurumlari.Acik, Domain.Constants.TalepTeklifDurumlari.Beklemede)
            };

            foreach (var seed in demandSeeds)
            {
                var buyerId = buyers[seed.BuyerEmail].Id;
                var productId = productIds[seed.ProductName];
                var sellerId = sellers[seed.SellerEmail].Id;

                var demand = await context.Talepler
                    .Include(x => x.Teklifler)
                    .SingleOrDefaultAsync(
                        x => x.AliciId == buyerId && x.UrunId == productId && x.Not == seed.Note,
                        cancellationToken);

                if (demand == null)
                {
                    demand = new Talep
                    {
                        AliciId = buyerId,
                        UrunId = productId,
                        Not = seed.Note
                    };

                    context.Talepler.Add(demand);
                }

                demand.Miktar = seed.Quantity;
                demand.Durum = seed.DemandStatus;
                demand.OlusturmaTarihi = seed.CreatedAt;
                demand.GuncellemeTarihi = seed.CreatedAt.AddDays(1);

                await context.SaveChangesAsync(cancellationToken);

                var offer = demand.Teklifler.SingleOrDefault(x => x.SaticiId == sellerId);
                if (offer == null)
                {
                    offer = new TalepTeklif
                    {
                        TalepId = demand.Id,
                        SaticiId = sellerId
                    };

                    context.TalepTeklifler.Add(offer);
                }

                offer.BirimFiyat = seed.UnitPrice;
                offer.Mesaj = seed.Message;
                offer.Durum = seed.OfferStatus;
                offer.OlusturmaTarihi = seed.CreatedAt.AddHours(6);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string? phoneNumber,
            string role,
            bool emailConfirmed,
            bool phoneConfirmed)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = emailConfirmed,
                    PhoneNumberConfirmed = phoneConfirmed
                };

                var createResult = await userManager.CreateAsync(user, SeedPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Seed kullanicisi olusturulamadi: {email}. {string.Join(" | ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                user.UserName = email;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
                user.EmailConfirmed = emailConfirmed;
                user.PhoneNumberConfirmed = phoneConfirmed;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException($"Seed kullanicisi guncellenemedi: {email}. {string.Join(" | ", updateResult.Errors.Select(e => e.Description))}");
                }

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, resetToken, SeedPassword);
                if (!resetResult.Succeeded)
                {
                    throw new InvalidOperationException($"Seed kullanicisi sifresi guncellenemedi: {email}. {string.Join(" | ", resetResult.Errors.Select(e => e.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Seed kullanicisina rol atanamadi: {email}. {string.Join(" | ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            return user;
        }

        private static string[] DemoImageUrls(string slug, int count)
        {
            return Enumerable.Range(1, count)
                .Select(index => $"/demo-media/{slug}/resimler/{index}.jpg")
                .ToArray();
        }

        private static string[] DemoVideoUrls(string slug, int count)
        {
            return Enumerable.Range(1, count)
                .Select(index => $"/demo-media/{slug}/videolar/{index}.mp4")
                .ToArray();
        }

        private sealed record CategorySeed(string Name, string Description);

        private sealed record SellerSeed(
            string Email,
            string StoreName,
            string TaxNumber,
            string PhoneNumber,
            string Address,
            string City,
            string District,
            DateTime CreatedAt);

        private sealed record BuyerSeed(
            string Email,
            string PhoneNumber,
            string Address,
            string FavoriteCategory,
            DateTime CreatedAt);

        private sealed record ProductSeed(
            string Name,
            string Description,
            decimal Price,
            int Stock,
            string CategoryName,
            string SellerEmail,
            IReadOnlyList<string> ImageUrls,
            IReadOnlyList<string> VideoUrls,
            DateTime CreatedAt);

        private sealed record FavoriteSeed(string BuyerEmail, string ProductName);

        private sealed record RatingSeed(string BuyerEmail, string ProductName, int Score, DateTime CreatedAt);

        private sealed record CommentSeed(string BuyerEmail, string ProductName, string Content, DateTime CreatedAt);

        private sealed record DemandSeed(
            string BuyerEmail,
            string ProductName,
            int Quantity,
            string Note,
            DateTime CreatedAt,
            string SellerEmail,
            decimal UnitPrice,
            string Message,
            string DemandStatus,
            string OfferStatus);
    }
}
