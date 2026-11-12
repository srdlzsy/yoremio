using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Text;

internal static class ServiceBehaviorTests
{
    public static IReadOnlyCollection<(string Name, Func<Task> Test)> GetTests()
    {
        return new (string Name, Func<Task> Test)[]
        {
            ("TalepService kendi urunune talep acilmasini engellemeli", TalepService_Should_RejectSelfDemand),
            ("TalepService teklif reddetme ve talep iptal akislarini yonetmeli", TalepService_Should_RejectOfferAndCancelDemand),
            ("TalepService talep detayinda yetkisiz ucuncu kullaniciyi engellemeli", TalepService_Should_BlockUnauthorizedDetailAccess),
            ("PuanService saticinin kendi urunune puan vermesini engellemeli", PuanService_Should_RejectSelfRating),
            ("SaticiProfiliService tum endpointler icin ayni guven skorunu hesaplamali", SaticiProfiliService_Should_ComputeConsistentTrustScore),
            ("ChatService konusma listesinde N+1 olmadan okunmamis sayisi ve magaza adini donmeli", ChatService_Should_UseBatchUnreadCountsAndStoreName),
            ("UrunService bulunamayan kayitlarda 404 icin KeyNotFoundException firlatmali", UrunService_Should_ThrowKeyNotFound_AndRollbackOrphanProduct),
            ("KategoriService tekil isim ve urunlu kategori silme kontrolu yapmali", KategoriService_Should_EnforceUniqueNameAndProductGuard),
            ("BildirimService olusturma ve okundu islemlerinde SignalR olayi yayinlamali", BildirimService_Should_DispatchRealtimeNotificationAndUnreadCount),
            ("SaticiProfiliService kapak resmi yukleyip aciklamayi kaydetmeli", SaticiProfiliService_Should_UploadCoverAndSaveDescription),
            ("KategoriService ve UrunService aktif urun sayisi ile lokasyonlari donmeli", KategoriAndUrunService_Should_ReturnProductCountsAndLocations),
            ("YorumServices puanlama yetkisi ve mevcut puani donmeli", YorumServices_Should_ReturnRatingEligibilityAndCurrentScore),
            ("TalepService aliciAdi, durum filtresi ve sayfalama desteklemeli", TalepService_Should_FilterAndPaginateWithBuyerName),
            ("TalepService acik pazar talebinde akilli uretici eslestirmesi yapmali", TalepService_Should_MatchRelevantSellers_OnOpenMarketDemand),
            ("TalepService tek dokunusla talep tekrari ve anlasma onayi saglamali", TalepService_Should_RepeatDemand_AndConfirmDealCompletion),
            ("SaticiProfiliService takip edilen satici yeni urun bildirimini tetiklemeli", SaticiTakip_Should_NotifyFollower_WhenSellerAddsNewProduct),
            ("UrunService toplu stok guncelleme ve fiyat alarmi tetiklemeli", UrunService_Should_BulkUpdateStock_AndTriggerPriceAlert),
            ("UrunService detay okumasinda goruntulenme ve kart alanlarini donmeli", UrunService_Should_IncrementViews_AndExposeCardMetadata),
            ("UrunService konum bazli kesifte sonuc yoksa akilli alternatif ve Benim Icin Bul onermeli", UrunService_Kesfet_Should_ReturnAlternativesAndFindForMePrompt_WhenEmpty),
            ("UrunService stok sifirdan yukselince stok alarmi bildirimini tetiklemeli", UrunService_Should_TriggerStokAlarmi_WhenProductRestocked),
            ("OrtakAlimService alici onerisi satici onayi ve hedef katilimciya ulasmayi yonetmeli", OrtakAlimService_Should_SupportProposalApprovalAndParticipantTarget),
            ("TalepService suresi dolan talebe teklif reddetmeli ve kabulde idempotent calismali", TalepService_Should_RejectExpiredOfferDeadline_AndBeIdempotentOnAccept),
            ("ChatService urun ve talep baglamini mesaj ve konusma ozetinde saklamali", ChatService_Should_PersistProductAndDemandContext),
            ("PlanliGonderimService sehrime gelen urunler sevkiyatini olusturup takipcilere bildirmeli", PlanliGonderimService_Should_CreateAndNotifyFollowers)
        };
    }

    private static async Task TalepService_Should_RejectSelfDemand()
    {
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 10, Adi = "Organik Bal", SaticiId = "seller-1", AktifMi = true, KategoriId = 1, Fiyat = 250 });

        var talepRepo = new FakeTalepRepository();
        var bildirim = new FakeBildirimService();
        var service = new TalepService(talepRepo, urunRepo, bildirim);

        await AssertThrowsAsync<UnauthorizedAccessException>(() =>
            service.TalepOlusturAsync("seller-1", new TalepOlusturDto { UrunId = 10, Miktar = 2 }));
    }

    private static async Task TalepService_Should_RejectOfferAndCancelDemand()
    {
        var urun = new Urun { Id = 11, Adi = "Yayla Tereyagi", SaticiId = "seller-1", AktifMi = true, KategoriId = 1, Fiyat = 400 };
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(urun);

        var talepRepo = new FakeTalepRepository(urunRepo);
        var bildirim = new FakeBildirimService();
        var service = new TalepService(talepRepo, urunRepo, bildirim);

        var created = await service.TalepOlusturAsync("buyer-1", new TalepOlusturDto { UrunId = 11, Miktar = 3, Not = "Taze olsun" });
        if (created.Durum != TalepDurumlari.Acik || bildirim.Created.Count != 1)
            throw new InvalidOperationException("Talep olusturma veya satici bildirimi beklendigi gibi calismadi.");

        var offer = await service.TeklifVerAsync(created.Id, "seller-1", new TalepTeklifOlusturDto { BirimFiyat = 380, Mesaj = "Kabuldur" });
        var afterReject = await service.TeklifReddetAsync(offer.Id, "buyer-1");

        if (afterReject.Durum != TalepDurumlari.Acik || afterReject.Teklifler.Single().Durum != TalepTeklifDurumlari.Red)
            throw new InvalidOperationException("Teklif reddedildiginde talep acik kalmali ve teklif RED olmalidir.");

        // Satici revize teklif gonderebilmeli
        await service.TeklifVerAsync(created.Id, "seller-1", new TalepTeklifOlusturDto { BirimFiyat = 360, Mesaj = "Yeni teklif" });
        var afterCancel = await service.TalepIptalEtAsync(created.Id, "buyer-1");

        if (afterCancel.Durum != TalepDurumlari.Iptal || afterCancel.Teklifler.Single().Durum != TalepTeklifDurumlari.Red)
            throw new InvalidOperationException("Talep iptal edildiginde durum IPTAL, bekleyen teklifler RED olmalidir.");
    }

    private static async Task TalepService_Should_BlockUnauthorizedDetailAccess()
    {
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 12, Adi = "Koy Peyniri", SaticiId = "seller-1", AktifMi = true, KategoriId = 1, Fiyat = 300 });

        var talepRepo = new FakeTalepRepository(urunRepo);
        var bildirim = new FakeBildirimService();
        var service = new TalepService(talepRepo, urunRepo, bildirim);

        var created = await service.TalepOlusturAsync("buyer-1", new TalepOlusturDto { UrunId = 12, Miktar = 1 });
        var buyerView = await service.GetTalepByIdAsync(created.Id, "buyer-1");
        var sellerView = await service.GetTalepByIdAsync(created.Id, "seller-1");

        if (buyerView.Id != created.Id || sellerView.Id != created.Id)
            throw new InvalidOperationException("Alici ve satici talep detayini gorebilmelidir.");

        await AssertThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetTalepByIdAsync(created.Id, "stranger-99"));
    }

    private static async Task PuanService_Should_RejectSelfRating()
    {
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 20, Adi = "Dut Pekmezi", SaticiId = "seller-1", AktifMi = true, KategoriId = 1, Fiyat = 200 });

        var service = new PuanService(new FakePuanRepository(), urunRepo, new FakeTalepRepository());

        await AssertThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetOrCreatePuanAsync(20, "seller-1", 5));

        await AssertThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetOrCreatePuanAsync(20, "buyer-no-deal", 5));
    }

    private static async Task SaticiProfiliService_Should_ComputeConsistentTrustScore()
    {
        var seller = new SaticiProfili
        {
            KullaniciId = "seller-1",
            MagazaAdi = "Kars Yayla",
            VergiNo = "1234567890",
            AktifMi = true,
            KayitTarihi = DateTime.UtcNow.AddDays(-180),
            Kullanici = new ApplicationUser { Id = "seller-1", EmailConfirmed = true }
        };

        var urun = new Urun
        {
            Id = 30,
            Adi = "Kars Kasari",
            SaticiId = "seller-1",
            AktifMi = true,
            Satici = seller,
            Puanlar = new List<Puan> { new() { PuanDegeri = 5 }, new() { PuanDegeri = 4 } },
            Yorumlar = new List<Yorum> { new() { Icerik = "Harika" } },
            Favoriler = new List<UrunFavori> { new(), new() }
        };
        seller.Urunler.Add(urun);

        var saticiRepo = new FakeSaticiProfiliRepository(seller);
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(urun);

        var service = new SaticiProfiliService(saticiRepo, urunRepo);

        var guvenSkoruDto = await service.GetGuvenSkoruAsync("seller-1");
        var publicDetay = await service.GetPublicSaticiDetayAsync("seller-1");
        var oneCikanlar = await service.GetOneCikanSaticilarAsync(6);

        if (guvenSkoruDto == null || publicDetay == null || oneCikanlar.Count != 1)
            throw new InvalidOperationException("Satici verileri getirilemedi.");

        if (guvenSkoruDto.GuvenSkoru != publicDetay.GuvenSkoru || guvenSkoruDto.GuvenSkoru != oneCikanlar.First().GuvenSkoru)
            throw new InvalidOperationException($"Guven skoru tutarsiz: {guvenSkoruDto.GuvenSkoru} vs {publicDetay.GuvenSkoru}");
    }

    private static async Task ChatService_Should_UseBatchUnreadCountsAndStoreName()
    {
        var chatRepo = new FakeChatMessageRepository();
        var sellerUser = new ApplicationUser
        {
            Id = "seller-1",
            UserName = "ayse@demo.local",
            Email = "ayse@demo.local",
            SaticiProfili = new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Ayse Ciftligi" }
        };
        var buyerUser = new ApplicationUser
        {
            Id = "buyer-1",
            UserName = "elif@demo.local",
            Email = "elif@demo.local"
        };

        chatRepo.Users["seller-1"] = sellerUser;
        chatRepo.Users["buyer-1"] = buyerUser;
        chatRepo.Messages.AddRange(new[]
        {
            new ChatMessage { Id = 1, SenderId = "seller-1", ReceiverId = "buyer-1", Message = "Merhaba", SentAt = DateTime.UtcNow.AddMinutes(-2), Sender = sellerUser, Receiver = buyerUser },
            new ChatMessage { Id = 2, SenderId = "seller-1", ReceiverId = "buyer-1", Message = "Hazir", SentAt = DateTime.UtcNow.AddMinutes(-1), Sender = sellerUser, Receiver = buyerUser }
        });

        var service = new ChatService(chatRepo, new FakeBildirimService());
        var conversations = await service.GetConversationsAsync("buyer-1");

        if (conversations.Count != 1)
            throw new InvalidOperationException("Konusma sayisi 1 olmali.");

        var conversation = conversations.Single();
        if (conversation.UnreadCount != 2 || conversation.MagazaAdi != "Ayse Ciftligi")
            throw new InvalidOperationException("UnreadCount veya MagazaAdi dogru eslenmedi.");

        if (chatRepo.SingleUnreadCalls != 0 || chatRepo.BatchUnreadCalls != 1)
            throw new InvalidOperationException("ChatService tekil N+1 sorgusu yerine toplu unread sorgusu kullanmali.");
    }

    private static async Task UrunService_Should_ThrowKeyNotFound_AndRollbackOrphanProduct()
    {
        var urunRepo = new FakeUrunRepository();
        var saticiRepo = new FakeSaticiProfiliRepository(new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Test Magaza" });
        var kategoriRepo = new FakeKategoriRepository();
        kategoriRepo.Items.Add(new Kategori { Id = 1, Adi = "Sut Urunleri" });

        var failingUpload = new FailingDosyaKaydetService();
        var service = new UrunService(urunRepo, new FakeUrunFavoriRepository(), saticiRepo, kategoriRepo, failingUpload);

        await AssertThrowsAsync<KeyNotFoundException>(() => service.UrunSilAsync(999, "seller-1"));
        await AssertThrowsAsync<KeyNotFoundException>(() => service.UrunResimSilAsync(999, 1, "seller-1"));

        var formFile = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("img")), 0, 3, "file", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        await AssertThrowsAsync<InvalidOperationException>(() =>
            service.UrunEkleAsync("seller-1", new UrunEkleDto
            {
                Adi = "Deneme Urun",
                Fiyat = 100,
                StokMiktari = 5,
                KategoriId = 1,
                Resimler = new List<IFormFile> { formFile }
            }));

        if (urunRepo.Items.Count != 0)
            throw new InvalidOperationException("Medya upload hatasi sonrasinda yetim urun silinmeli.");
    }

    private static async Task KategoriService_Should_EnforceUniqueNameAndProductGuard()
    {
        var repo = new FakeKategoriRepository();
        repo.Items.Add(new Kategori { Id = 1, Adi = "Sebze" });
        repo.CategoryIdsWithProducts.Add(1);

        var service = new KategoriService(repo);

        await AssertThrowsAsync<ArgumentException>(() =>
            service.AddAsync(new KategoriCreateDto { Adi = "sebze" }));

        await AssertThrowsAsync<ArgumentException>(() =>
            service.DeleteAsync(1));
    }

    private static async Task BildirimService_Should_DispatchRealtimeNotificationAndUnreadCount()
    {
        var repo = new FakeBildirimRepository();
        var dispatcher = new FakeBildirimRealtimeDispatcher();
        var service = new BildirimService(repo, dispatcher);

        var created = await service.BildirimOlusturAsync(new BildirimOlusturDto
        {
            KullaniciId = "user-1",
            Tur = BildirimTurleri.Teklif,
            Baslik = "Yeni teklif",
            Mesaj = "Urununuz icin yeni teklif geldi."
        });

        if (dispatcher.Notifications.Count != 1 || dispatcher.Notifications[0].UnreadCount != 1 || created.OkunmamisSayisi != 1)
            throw new InvalidOperationException("BildirimService NotificationReceived olayini veya okunmamis sayisini yayinlamadi.");

        await service.OkunduIsaretleAsync(created.Id, "user-1");
        if (dispatcher.UnreadCountUpdates.LastOrDefault() != ("user-1", 0))
            throw new InvalidOperationException("Bildirim okundu isaretlenince UnreadNotificationCountUpdated 0 olmali.");
    }

    private static async Task SaticiProfiliService_Should_UploadCoverAndSaveDescription()
    {
        var seller = new SaticiProfili
        {
            KullaniciId = "seller-1",
            MagazaAdi = "Kars Yayla",
            VergiNo = "1234567890",
            AktifMi = true,
            KapakResimUrl = "/uploads/old-cover.jpg",
            Kullanici = new ApplicationUser { Id = "seller-1", EmailConfirmed = true }
        };

        var saticiRepo = new FakeSaticiProfiliRepository(seller);
        var urunRepo = new FakeUrunRepository();
        var dosya = new InMemoryDosyaKaydetService();
        var service = new SaticiProfiliService(saticiRepo, urunRepo, dosya);

        var formFile = new FormFile(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }), 0, 4, "kapakResim", "kapak.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var updated = await service.UpdateProfilAsync("seller-1", new SaticiProfilGuncelleDto
        {
            Aciklama = "Dogal yayla ureticisi",
            KapakResim = formFile
        });

        if (updated == null || updated.Aciklama != "Dogal yayla ureticisi" || string.IsNullOrWhiteSpace(updated.KapakResimUrl))
            throw new InvalidOperationException("Satici profil aciklamasi veya kapak resmi kaydedilmedi.");

        if (!dosya.DeletedUrls.Contains("/uploads/old-cover.jpg"))
            throw new InvalidOperationException("Eski kapak resmi temizlenmedi.");

        var ozet = await service.GetPublicSaticiDetayAsync("seller-1");
        if (ozet?.KapakResimUrl != updated.KapakResimUrl || ozet.Aciklama != "Dogal yayla ureticisi")
            throw new InvalidOperationException("Public satici vitrin ozetinde ozel kapak veya aciklama donmedi.");
    }

    private static async Task KategoriAndUrunService_Should_ReturnProductCountsAndLocations()
    {
        var kategoriRepo = new FakeKategoriRepository();
        kategoriRepo.Items.Add(new Kategori { Id = 1, Adi = "Bal" });
        kategoriRepo.ProductCounts[1] = 4;

        var kategoriService = new KategoriService(kategoriRepo);
        var kategoriler = (await kategoriService.GetAllDtosAsync()).ToList();
        if (kategoriler.Single().UrunSayisi != 4)
            throw new InvalidOperationException("KategoriDto.UrunSayisi dogru donmedi.");

        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 1, AktifMi = true, Satici = new SaticiProfili { Sehir = "Ardahan", Ilce = "Posof" } });
        urunRepo.Items.Add(new Urun { Id = 2, AktifMi = true, Satici = new SaticiProfili { Sehir = "Ardahan", Ilce = "Merkez" } });

        var urunService = new UrunService(urunRepo, new FakeUrunFavoriRepository(), new FakeSaticiProfiliRepository(), kategoriRepo, new InMemoryDosyaKaydetService());
        var locations = await urunService.GetActiveLocationsAsync();
        if (locations.Count != 1 || locations.Single().Ilceler.Count != 2)
            throw new InvalidOperationException("Aktif sehir/ilce gruplamasi beklendigi gibi donmedi.");
    }

    private static async Task YorumServices_Should_ReturnRatingEligibilityAndCurrentScore()
    {
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 50, Adi = "Bal", SaticiId = "seller-1", AktifMi = true });

        var talepRepo = new FakeTalepRepository(urunRepo);
        talepRepo.Talepler.Add(new Talep { Id = 1, AliciId = "buyer-1", UrunId = 50, Durum = TalepDurumlari.Anlasildi });

        var puanRepo = new FakePuanRepository
        {
            ExistingPuan = new Puan { Id = 1, UrunId = 50, KullaniciId = "buyer-1", PuanDegeri = 4 }
        };

        var yorumService = new YorumServices(new FakeYorumRepository(), urunRepo, talepRepo, puanRepo);
        var yetki = await yorumService.GetYorumYetkisiAsync(50, "buyer-1");

        if (!yetki.YorumYapabilir || !yetki.Puanlayabilir || yetki.MevcutPuan != 4)
            throw new InvalidOperationException("YorumYetkisiDto puanlayabilir veya mevcutPuan alanini dogru donmedi.");
    }

    private static async Task TalepService_Should_FilterAndPaginateWithBuyerName()
    {
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 60, Adi = "Peynir", SaticiId = "seller-1", AktifMi = true, Fiyat = 200 });

        var talepRepo = new FakeTalepRepository(urunRepo);
        var alici = new ApplicationUser { Id = "buyer-1", UserName = "serdal_alici", Email = "serdal@example.com" };
        talepRepo.Talepler.Add(new Talep { Id = 1, AliciId = "buyer-1", Alici = alici, UrunId = 60, Urun = urunRepo.Items[0], Miktar = 1, Durum = TalepDurumlari.Acik, OlusturmaTarihi = DateTime.UtcNow.AddMinutes(-5) });
        talepRepo.Talepler.Add(new Talep { Id = 2, AliciId = "buyer-1", Alici = alici, UrunId = 60, Urun = urunRepo.Items[0], Miktar = 2, Durum = TalepDurumlari.Iptal, OlusturmaTarihi = DateTime.UtcNow });

        var service = new TalepService(talepRepo, urunRepo, new FakeBildirimService());
        var paged = await service.GetSaticiTalepleriPagedAsync("seller-1", TalepDurumlari.Acik, 1, 10);

        if (paged.TotalCount != 1 || paged.Items.Single().AliciAdi != "serdal_alici")
            throw new InvalidOperationException("TalepService durum filtresi, sayfalama veya AliciAdi eslemesi hatali.");
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Beklenen exception {typeof(TException).Name}, gelen: {ex.GetType().Name} ({ex.Message})");
        }

        throw new InvalidOperationException($"Beklenen exception {typeof(TException).Name} firlatilmadi.");
    }

    private sealed class FakeBildirimService : IBildirimService
    {
        public List<BildirimOlusturDto> Created { get; } = new();

        public Task<BildirimDto> BildirimOlusturAsync(BildirimOlusturDto dto)
        {
            Created.Add(dto);
            return Task.FromResult(new BildirimDto { Id = Created.Count, Tur = dto.Tur, Baslik = dto.Baslik, Mesaj = dto.Mesaj });
        }

        public Task<BildirimPagedResultDto> GetBildirimlerAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize)
            => Task.FromResult(new BildirimPagedResultDto());

        public Task<int> GetOkunmamisSayisiAsync(string kullaniciId) => Task.FromResult(0);
        public Task<BildirimDto> OkunduIsaretleAsync(long bildirimId, string kullaniciId) => Task.FromResult(new BildirimDto());
        public Task<int> TumunuOkunduIsaretleAsync(string kullaniciId) => Task.FromResult(0);
    }

    private sealed class FakeUrunRepository : IUrunRepository
    {
        private int _nextId = 100;
        public List<Urun> Items { get; } = new();

        public Task AddAsync(Urun entity)
        {
            if (entity.Id == 0) entity.Id = _nextId++;
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task<Urun> EkleUrunAsync(Urun urun, List<string> resimUrlListesi, List<string> videoUrlListesi)
        {
            foreach (var r in resimUrlListesi) urun.Resimler.Add(new UrunResim { Url = r, UrunId = urun.Id });
            foreach (var v in videoUrlListesi) urun.Videolar.Add(new UrunVideo { Url = v, UrunId = urun.Id });
            return Task.FromResult(urun);
        }

        public Task<IEnumerable<Urun>> FindAsync(Expression<Func<Urun, bool>> predicate)
            => Task.FromResult<IEnumerable<Urun>>(Items.Where(predicate.Compile()).ToList());

        public Task<IEnumerable<Urun>> GetAllAsync() => Task.FromResult<IEnumerable<Urun>>(Items);
        public Task<Urun?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<Urun?> GetByIdForUpdateAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<Urun?> GetByIdWithRelationsAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task IncrementViewCountAsync(int id)
        {
            var item = Items.FirstOrDefault(x => x.Id == id);
            if (item != null) item.GoruntulenmeSayisi += 1;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyCollection<Urun>> GetRecommendedAsync(
            IReadOnlyCollection<int> kategoriIdleri,
            IReadOnlyCollection<int> haricUrunIdleri,
            int take,
            string? tercihEdilenSehir = null,
            string? tercihEdilenIlce = null)
            => Task.FromResult<IReadOnlyCollection<Urun>>(Items.Take(take).ToList());
        public Task<IReadOnlyCollection<(string Sehir, string? Ilce)>> GetActiveSellerLocationsAsync()
            => Task.FromResult<IReadOnlyCollection<(string Sehir, string? Ilce)>>(
                Items
                    .Where(u => u.AktifMi && !string.IsNullOrWhiteSpace(u.Satici?.Sehir))
                    .Select(u => (u.Satici.Sehir!, u.Satici.Ilce))
                    .Distinct()
                    .ToList());
        public Task<PagedResult<Urun>> QueryAsync(UrunQuery query)
        {
            var filtered = Items.Where(u => u.AktifMi).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var q = query.Q.Trim();
                filtered = filtered.Where(u =>
                    u.Adi.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (u.Aciklama != null && u.Aciklama.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (u.UretimSehir != null && u.UretimSehir.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(query.TeslimatSehir))
            {
                var city = query.TeslimatSehir.Trim();
                filtered = filtered.Where(u =>
                    (u.Satici != null && string.Equals(u.Satici.Sehir, city, StringComparison.OrdinalIgnoreCase)) ||
                    (u.KargoVarMi && (string.IsNullOrWhiteSpace(u.GonderimSehirleri) ||
                        u.GonderimSehirleri.Contains("Türkiye", StringComparison.OrdinalIgnoreCase) ||
                        u.GonderimSehirleri.Contains(city, StringComparison.OrdinalIgnoreCase))));
            }

            if (!string.IsNullOrWhiteSpace(query.UretimSehir))
            {
                var origin = query.UretimSehir.Trim();
                filtered = filtered.Where(u =>
                    (u.UretimSehir != null && u.UretimSehir.Contains(origin, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Satici != null && u.Satici.Sehir != null && u.Satici.Sehir.Contains(origin, StringComparison.OrdinalIgnoreCase)));
            }

            if (query.SadeceStoktaOlanlar == true)
            {
                filtered = filtered.Where(u => u.StokMiktari > 0);
            }

            var list = filtered.ToList();
            return Task.FromResult(new PagedResult<Urun>
            {
                Items = list.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = list.Count
            });
        }
        public void Remove(Urun entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(Urun entity) { }
    }

    private sealed class FakeTalepRepository : ITalepRepository
    {
        private readonly FakeUrunRepository? _urunRepository;
        private int _nextTalepId = 1;
        private int _nextTeklifId = 1;
        public List<Talep> Talepler { get; } = new();
        public List<TalepTeklif> Teklifler { get; } = new();

        public FakeTalepRepository(FakeUrunRepository? urunRepository = null)
        {
            _urunRepository = urunRepository;
        }

        public Task AddAsync(Talep entity)
        {
            if (entity.Id == 0) entity.Id = _nextTalepId++;
            if (entity.Urun == null && _urunRepository != null)
            {
                entity.Urun = _urunRepository.Items.FirstOrDefault(u => u.Id == entity.UrunId);
            }
            Talepler.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddTeklifAsync(TalepTeklif teklif)
        {
            if (teklif.Id == 0) teklif.Id = _nextTeklifId++;
            Teklifler.Add(teklif);
            var talep = Talepler.First(t => t.Id == teklif.TalepId);
            teklif.Talep = talep;
            talep.Teklifler.Add(teklif);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Talep>> FindAsync(Expression<Func<Talep, bool>> predicate)
            => Task.FromResult<IEnumerable<Talep>>(Talepler.Where(predicate.Compile()).ToList());

        public Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId, string? durum = null)
            => Task.FromResult<IReadOnlyCollection<Talep>>(FilterTalepler(Talepler.Where(t => t.AliciId == aliciId), durum));

        public Task<PagedResult<Talep>> GetAliciTalepleriPagedAsync(string aliciId, string? durum, int page, int pageSize)
        {
            var filtered = FilterTalepler(Talepler.Where(t => t.AliciId == aliciId), durum);
            return Task.FromResult(new PagedResult<Talep>
            {
                Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
                Page = page,
                PageSize = pageSize,
                TotalCount = filtered.Count
            });
        }

        public Task<IEnumerable<Talep>> GetAllAsync() => Task.FromResult<IEnumerable<Talep>>(Talepler);
        public Task<Talep?> GetByIdAsync(object id) => Task.FromResult(Talepler.FirstOrDefault(t => t.Id == Convert.ToInt32(id)));
        public Task<Talep?> GetByIdWithDetailsAsync(int talepId) => Task.FromResult(Talepler.FirstOrDefault(t => t.Id == talepId));
        public Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId, string? durum = null)
            => Task.FromResult<IReadOnlyCollection<Talep>>(FilterTalepler(
                Talepler.Where(t => t.Urun?.SaticiId == saticiId || t.Eslesmeler.Any(e => e.SaticiId == saticiId) || t.Teklifler.Any(o => o.SaticiId == saticiId)),
                durum));

        public Task<PagedResult<Talep>> GetSaticiTalepleriPagedAsync(string saticiId, string? durum, int page, int pageSize)
        {
            var filtered = FilterTalepler(
                Talepler.Where(t => t.Urun?.SaticiId == saticiId || t.Eslesmeler.Any(e => e.SaticiId == saticiId) || t.Teklifler.Any(o => o.SaticiId == saticiId)),
                durum);
            return Task.FromResult(new PagedResult<Talep>
            {
                Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
                Page = page,
                PageSize = pageSize,
                TotalCount = filtered.Count
            });
        }

        public Task<PagedResult<Talep>> GetAcikPazarTalepleriForSellerAsync(string saticiId, string? durum, int page, int pageSize)
        {
            var effectiveDurum = string.IsNullOrWhiteSpace(durum) ? TalepDurumlari.Acik : durum;
            var filtered = FilterTalepler(
                Talepler.Where(t => t.TalepTuru == TalepTurleri.AcikPazar && t.AliciId != saticiId),
                effectiveDurum);
            return Task.FromResult(new PagedResult<Talep>
            {
                Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
                Page = page,
                PageSize = pageSize,
                TotalCount = filtered.Count
            });
        }

        public Task AddEslesmelerAsync(IEnumerable<TalepEslesme> eslesmeler)
        {
            foreach (var eslesme in eslesmeler)
            {
                var talep = Talepler.FirstOrDefault(t => t.Id == eslesme.TalepId);
                talep?.Eslesmeler.Add(eslesme);
            }
            return Task.CompletedTask;
        }

        private static List<Talep> FilterTalepler(IEnumerable<Talep> query, string? durum)
        {
            if (!string.IsNullOrWhiteSpace(durum))
            {
                var normalized = durum.Trim().ToUpperInvariant();
                query = query.Where(t => t.Durum == normalized);
            }

            return query.OrderByDescending(t => t.OlusturmaTarihi).ToList();
        }
        public Task<TalepTeklif?> GetTeklifByIdWithDetailsAsync(int teklifId) => Task.FromResult(Teklifler.FirstOrDefault(t => t.Id == teklifId));
        public Task<TalepTeklif?> GetTeklifByTalepVeSaticiAsync(int talepId, string saticiId)
            => Task.FromResult(Teklifler.FirstOrDefault(t => t.TalepId == talepId && t.SaticiId == saticiId));
        public Task<bool> HasAcceptedDemandForProductAsync(string aliciId, int urunId)
            => Task.FromResult(Talepler.Any(t => t.AliciId == aliciId && t.UrunId == urunId && t.Durum == TalepDurumlari.Anlasildi));
        public void Remove(Talep entity) => Talepler.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(Talep entity) { }
    }

    private sealed class FakePuanRepository : IPuanRepository
    {
        public Puan? ExistingPuan { get; set; }
        public Task AddAsync(Puan entity) => Task.CompletedTask;
        public Task<IEnumerable<Puan>> FindAsync(Expression<Func<Puan, bool>> predicate) => Task.FromResult<IEnumerable<Puan>>(Array.Empty<Puan>());
        public Task<IEnumerable<Puan>> GetAllAsync() => Task.FromResult<IEnumerable<Puan>>(Array.Empty<Puan>());
        public Task<Puan?> GetByIdAsync(object id) => Task.FromResult<Puan?>(null);
        public Task<int> GetOrCreatePuanAsync(int urunId, string kullaniciId, int puanDegeri) => Task.FromResult(puanDegeri);
        public Task<double> GetOrtalamaPuanByUrunIdAsync(int urunId) => Task.FromResult(0d);
        public Task<Puan?> GetPuanByUrunIdAndKullaniciIdAsync(int urunId, string kullaniciId) => Task.FromResult(ExistingPuan);
        public Task<IEnumerable<Puan>> GetPuanlarByKullaniciIdAsync(string kullaniciId) => Task.FromResult<IEnumerable<Puan>>(Array.Empty<Puan>());
        public Task<IEnumerable<Puan>> GetPuanlarByUrunIdAsync(int urunId) => Task.FromResult<IEnumerable<Puan>>(Array.Empty<Puan>());
        public Task<bool> PuanVarmiAsync(int urunId, string kullaniciId) => Task.FromResult(false);
        public void Remove(Puan entity) { }
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(Puan entity) { }
    }

    private sealed class FakeSaticiProfiliRepository : ISaticiProfiliRepository
    {
        private readonly List<SaticiProfili> _items;
        public List<SaticiProfili> Items => _items;
        public FakeSaticiProfiliRepository(params SaticiProfili[] items) => _items = items.ToList();

        public Task AddAsync(SaticiProfili entity) { _items.Add(entity); return Task.CompletedTask; }
        public Task<IEnumerable<SaticiProfili>> FindAsync(Expression<Func<SaticiProfili, bool>> predicate)
            => Task.FromResult<IEnumerable<SaticiProfili>>(_items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<SaticiProfili>> GetAllAsync() => Task.FromResult<IEnumerable<SaticiProfili>>(_items);
        public Task<SaticiProfili?> GetByIdAsync(object id) => Task.FromResult(_items.FirstOrDefault(s => s.KullaniciId == id.ToString()));
        public Task<IReadOnlyCollection<SaticiProfili>> GetOneCikanSaticilarAsync(int take) => Task.FromResult<IReadOnlyCollection<SaticiProfili>>(_items.Take(take).ToList());
        public Task<SaticiProfili?> GetSaticiPublicDetailsByIdAsync(string kullaniciId) => Task.FromResult(_items.FirstOrDefault(s => s.KullaniciId == kullaniciId && s.AktifMi));
        public Task<SaticiProfili?> GetSaticiWithUserByIdAsync(string kullaniciId) => Task.FromResult(_items.FirstOrDefault(s => s.KullaniciId == kullaniciId));
        public void Remove(SaticiProfili entity) => _items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(SaticiProfili entity) { }
    }

    private sealed class FakeChatMessageRepository : IChatMessageRepository
    {
        public Dictionary<string, ApplicationUser> Users { get; } = new();
        public List<ChatMessage> Messages { get; } = new();
        public int SingleUnreadCalls { get; private set; }
        public int BatchUnreadCalls { get; private set; }

        public Task AddAsync(ChatMessage entity) { Messages.Add(entity); return Task.CompletedTask; }
        public Task<IEnumerable<ChatMessage>> FindAsync(Expression<Func<ChatMessage, bool>> predicate)
            => Task.FromResult<IEnumerable<ChatMessage>>(Messages.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<ChatMessage>> GetAllAsync() => Task.FromResult<IEnumerable<ChatMessage>>(Messages);
        public Task<ChatMessage?> GetByIdAsync(object id) => Task.FromResult(Messages.FirstOrDefault(m => m.Id == Convert.ToInt64(id)));
        public Task<ChatMessage?> GetByIdWithUsersAsync(long id) => Task.FromResult(Messages.FirstOrDefault(m => m.Id == id));
        public Task<int> GetConversationMessageCountAsync(string firstUserId, string secondUserId, int? urunId = null, int? talepId = null)
        {
            var q = Messages.Where(m =>
                (m.SenderId == firstUserId && m.ReceiverId == secondUserId) ||
                (m.SenderId == secondUserId && m.ReceiverId == firstUserId));
            if (urunId.HasValue) q = q.Where(m => m.UrunId == urunId.Value);
            if (talepId.HasValue) q = q.Where(m => m.TalepId == talepId.Value);
            return Task.FromResult(q.Count());
        }
        public Task<IReadOnlyCollection<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId, int skip, int take, int? urunId = null, int? talepId = null)
        {
            var q = Messages.Where(m =>
                (m.SenderId == firstUserId && m.ReceiverId == secondUserId) ||
                (m.SenderId == secondUserId && m.ReceiverId == firstUserId));
            if (urunId.HasValue) q = q.Where(m => m.UrunId == urunId.Value);
            if (talepId.HasValue) q = q.Where(m => m.TalepId == talepId.Value);
            return Task.FromResult<IReadOnlyCollection<ChatMessage>>(q.Skip(skip).Take(take).ToList());
        }
        public Task<IReadOnlyCollection<ChatMessage>> GetRecentMessagesForUserAsync(string userId, int take)
            => Task.FromResult<IReadOnlyCollection<ChatMessage>>(Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId).Take(take).ToList());
        public Task<int> GetUnreadCountAsync(string receiverId, string senderId)
        {
            SingleUnreadCalls++;
            return Task.FromResult(Messages.Count(m => m.ReceiverId == receiverId && m.SenderId == senderId && m.ReadAt == null));
        }
        public Task<Dictionary<string, int>> GetUnreadCountsBySenderAsync(string receiverId)
        {
            BatchUnreadCalls++;
            var dict = Messages
                .Where(m => m.ReceiverId == receiverId && m.ReadAt == null)
                .GroupBy(m => m.SenderId)
                .ToDictionary(g => g.Key, g => g.Count());
            return Task.FromResult(dict);
        }
        public Task<int> MarkConversationReadAsync(string receiverId, string senderId, DateTime readAt) => Task.FromResult(0);
        public void Remove(ChatMessage entity) => Messages.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(ChatMessage entity) { }
        public Task<bool> UserExistsAsync(string userId) => Task.FromResult(Users.ContainsKey(userId));
    }

    private sealed class FakeKategoriRepository : IKategoriRepository
    {
        public List<Kategori> Items { get; } = new();
        public HashSet<int> CategoryIdsWithProducts { get; } = new();

        public Dictionary<int, int> ProductCounts { get; } = new();

        public Task AddAsync(Kategori entity) { Items.Add(entity); return Task.CompletedTask; }
        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
            => Task.FromResult(Items.Any(k => string.Equals(k.Adi, name.Trim(), StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || k.Id != excludeId.Value)));
        public Task<IEnumerable<Kategori>> FindAsync(Expression<Func<Kategori, bool>> predicate)
            => Task.FromResult<IEnumerable<Kategori>>(Items.Where(predicate.Compile()).ToList());
        public Task<IReadOnlyDictionary<int, int>> GetActiveProductCountsAsync() => Task.FromResult<IReadOnlyDictionary<int, int>>(ProductCounts);
        public Task<IEnumerable<Kategori>> GetAllAsync() => Task.FromResult<IEnumerable<Kategori>>(Items);
        public Task<Kategori?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(k => k.Id == Convert.ToInt32(id)));
        public Task<bool> HasProductsAsync(int kategoriId) => Task.FromResult(CategoryIdsWithProducts.Contains(kategoriId));
        public void Remove(Kategori entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(Kategori entity) { }
    }

    private sealed class FakeUrunFavoriRepository : IUrunFavoriRepository
    {
        public Task AddAsync(UrunFavori entity) => Task.CompletedTask;
        public Task<IEnumerable<UrunFavori>> FindAsync(Expression<Func<UrunFavori, bool>> predicate) => Task.FromResult<IEnumerable<UrunFavori>>(Array.Empty<UrunFavori>());
        public Task<IEnumerable<UrunFavori>> GetAllAsync() => Task.FromResult<IEnumerable<UrunFavori>>(Array.Empty<UrunFavori>());
        public Task<UrunFavori?> GetByIdAsync(object id) => Task.FromResult<UrunFavori?>(null);
        public Task<UrunFavori?> GetByKullaniciVeUrunAsync(string kullaniciId, int urunId) => Task.FromResult<UrunFavori?>(null);
        public Task<IReadOnlyCollection<int>> GetFavoriUrunIdleriAsync(string kullaniciId) => Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());
        public Task<IReadOnlyCollection<Urun>> GetFavoriUrunlerAsync(string kullaniciId) => Task.FromResult<IReadOnlyCollection<Urun>>(Array.Empty<Urun>());
        public Task<IReadOnlyCollection<int>> GetKullaniciFavoriKategoriIdleriAsync(string kullaniciId, int take) => Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());
        public void Remove(UrunFavori entity) { }
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(UrunFavori entity) { }
    }

    private sealed class FailingDosyaKaydetService : IDosyaKaydetService
    {
        public Task<string> KaydetDosyaAsync(IFormFile dosya, string klasor)
            => throw new InvalidOperationException("Upload simule hatasi");

        public Task SilDosyaAsync(string url) => Task.CompletedTask;
    }

    private sealed class InMemoryDosyaKaydetService : IDosyaKaydetService
    {
        public List<string> DeletedUrls { get; } = new();

        public Task<string> KaydetDosyaAsync(IFormFile dosya, string klasor)
            => Task.FromResult($"/uploads/{klasor}/{dosya.FileName}".Replace("\\", "/"));

        public Task SilDosyaAsync(string url)
        {
            DeletedUrls.Add(url);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBildirimRepository : IBildirimRepository
    {
        private long _nextId = 1;
        public List<Bildirim> Items { get; } = new();

        public Task AddAsync(Bildirim entity)
        {
            if (entity.Id == 0) entity.Id = _nextId++;
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Bildirim>> FindAsync(Expression<Func<Bildirim, bool>> predicate) => Task.FromResult<IEnumerable<Bildirim>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<Bildirim>> GetAllAsync() => Task.FromResult<IEnumerable<Bildirim>>(Items);
        public Task<Bildirim?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt64(id)));
        public Task<Bildirim?> GetKullaniciBildirimiAsync(long bildirimId, string kullaniciId) => Task.FromResult(Items.FirstOrDefault(x => x.Id == bildirimId && x.KullaniciId == kullaniciId));

        public Task<PagedResult<Bildirim>> GetKullaniciBildirimleriAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize)
        {
            var query = Items.Where(x => x.KullaniciId == kullaniciId && (!sadeceOkunmamis || !x.OkunmaTarihi.HasValue)).ToList();
            return Task.FromResult(new PagedResult<Bildirim> { Items = query, Page = page, PageSize = pageSize, TotalCount = query.Count });
        }

        public Task<int> GetOkunmamisSayisiAsync(string kullaniciId) => Task.FromResult(Items.Count(x => x.KullaniciId == kullaniciId && !x.OkunmaTarihi.HasValue));

        public Task<int> TumunuOkunduIsaretleAsync(string kullaniciId, DateTime readAt)
        {
            var unread = Items.Where(x => x.KullaniciId == kullaniciId && !x.OkunmaTarihi.HasValue).ToList();
            foreach (var item in unread) item.OkunmaTarihi = readAt;
            return Task.FromResult(unread.Count);
        }

        public void Remove(Bildirim entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(Bildirim entity) { }
    }

    private sealed class FakeBildirimRealtimeDispatcher : IBildirimRealtimeDispatcher
    {
        public List<(string UserId, BildirimDto Dto, int UnreadCount)> Notifications { get; } = new();
        public List<(string UserId, int UnreadCount)> UnreadCountUpdates { get; } = new();

        public Task DispatchNotificationAsync(string kullaniciId, BildirimDto bildirim, int unreadCount)
        {
            Notifications.Add((kullaniciId, bildirim, unreadCount));
            return Task.CompletedTask;
        }

        public Task DispatchUnreadCountAsync(string kullaniciId, int unreadCount)
        {
            UnreadCountUpdates.Add((kullaniciId, unreadCount));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeYorumRepository : IYorumRepository
    {
        public List<Yorum> Items { get; } = new();
        public Task AddAsync(Yorum entity) { Items.Add(entity); return Task.CompletedTask; }
        public Task<IEnumerable<Yorum>> FindAsync(Expression<Func<Yorum, bool>> predicate) => Task.FromResult<IEnumerable<Yorum>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<Yorum>> GetAllAsync() => Task.FromResult<IEnumerable<Yorum>>(Items);
        public Task<Yorum?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<Yorum?> GetByIdWithUserAsync(int yorumId) => Task.FromResult(Items.FirstOrDefault(x => x.Id == yorumId));
        public Task<IEnumerable<Yorum>> GetYorumlarByUrunIdAsync(int urunId) => Task.FromResult<IEnumerable<Yorum>>(Items.Where(x => x.UrunId == urunId && x.AktifMi).ToList());
        public void Remove(Yorum entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(Yorum entity) { }
    }

    private sealed class FakeStokAlarmiRepository : IStokAlarmiRepository
    {
        public List<StokAlarmi> Items { get; } = new();
        private readonly FakeUrunRepository? _urunRepo;

        public FakeStokAlarmiRepository(FakeUrunRepository? urunRepo = null)
        {
            _urunRepo = urunRepo;
        }

        public Task AddAsync(StokAlarmi entity) { Items.Add(entity); return Task.CompletedTask; }
        public Task<IEnumerable<StokAlarmi>> FindAsync(Expression<Func<StokAlarmi, bool>> predicate) => Task.FromResult<IEnumerable<StokAlarmi>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<StokAlarmi>> GetAllAsync() => Task.FromResult<IEnumerable<StokAlarmi>>(Items);
        public Task<StokAlarmi?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<StokAlarmi?> GetAsync(string aliciId, int urunId) => Task.FromResult(Items.FirstOrDefault(x => x.AliciId == aliciId && x.UrunId == urunId));
        public Task<IReadOnlyCollection<StokAlarmi>> GetByAliciWithProductAsync(string aliciId)
        {
            foreach (var x in Items.Where(i => i.AliciId == aliciId && i.Urun == null && _urunRepo != null))
            {
                x.Urun = _urunRepo!.Items.FirstOrDefault(u => u.Id == x.UrunId);
            }
            return Task.FromResult<IReadOnlyCollection<StokAlarmi>>(Items.Where(x => x.AliciId == aliciId && x.AktifMi).ToList());
        }
        public Task<IReadOnlyCollection<StokAlarmi>> GetActiveByProductAsync(int urunId)
            => Task.FromResult<IReadOnlyCollection<StokAlarmi>>(Items.Where(x => x.UrunId == urunId && x.AktifMi).ToList());
        public void Remove(StokAlarmi entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(StokAlarmi entity) { }
    }

    private sealed class FakeOrtakAlimRepository : IOrtakAlimRepository
    {
        public List<OrtakAlim> Items { get; } = new();
        private int _nextId = 1;

        public Task AddAsync(OrtakAlim entity)
        {
            if (entity.Id == 0) entity.Id = _nextId++;
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddKatilimAsync(OrtakAlimKatilim katilim)
        {
            var group = Items.First(x => x.Id == katilim.OrtakAlimId);
            if (!group.Katilimlar.Contains(katilim))
            {
                group.Katilimlar.Add(katilim);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<OrtakAlim>> FindAsync(Expression<Func<OrtakAlim, bool>> predicate) => Task.FromResult<IEnumerable<OrtakAlim>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<OrtakAlim>> GetAllAsync() => Task.FromResult<IEnumerable<OrtakAlim>>(Items);
        public Task<OrtakAlim?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<OrtakAlim?> GetByIdWithDetailsAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<OrtakAlimKatilim?> GetKatilimAsync(int ortakAlimId, string aliciId)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == ortakAlimId)?.Katilimlar.FirstOrDefault(k => k.AliciId == aliciId));

        public Task<IReadOnlyCollection<OrtakAlim>> GetMatchingOpenGroupBuysAsync(string? q, string? hedefSehir, int take = 5)
        {
            var filtered = Items.Where(o => o.Durum == OrtakAlimDurumlari.Acik || o.Durum == OrtakAlimDurumlari.HedefTamamlandi);
            if (!string.IsNullOrWhiteSpace(hedefSehir))
            {
                filtered = filtered.Where(o => o.HedefSehir.Contains(hedefSehir.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                filtered = filtered.Where(o => o.Baslik.Contains(term, StringComparison.OrdinalIgnoreCase) || o.MenseiSehir.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult<IReadOnlyCollection<OrtakAlim>>(filtered.Take(take).ToList());
        }

        public Task<PagedResult<OrtakAlim>> QueryAsync(string? hedefSehir, string? menseiSehir, string? q, string? durum, int page, int pageSize)
        {
            var filtered = Items.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(hedefSehir))
                filtered = filtered.Where(o => o.HedefSehir.Contains(hedefSehir.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(menseiSehir))
                filtered = filtered.Where(o => o.MenseiSehir.Contains(menseiSehir.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(durum))
                filtered = filtered.Where(o => string.Equals(o.Durum, durum.Trim(), StringComparison.OrdinalIgnoreCase));
            var list = filtered.ToList();
            return Task.FromResult(new PagedResult<OrtakAlim>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = list.Count
            });
        }

        public void Remove(OrtakAlim entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(OrtakAlim entity) { }
    }

    private sealed class FakeSaticiTakipRepository : ISaticiTakipRepository
    {
        public List<SaticiTakip> Items { get; } = new();
        private readonly FakeSaticiProfiliRepository? _saticiRepo;

        public FakeSaticiTakipRepository(FakeSaticiProfiliRepository? saticiRepo = null)
        {
            _saticiRepo = saticiRepo;
        }

        public Task AddAsync(SaticiTakip entity) { Items.Add(entity); return Task.CompletedTask; }
        public Task<IEnumerable<SaticiTakip>> FindAsync(Expression<Func<SaticiTakip, bool>> predicate) => Task.FromResult<IEnumerable<SaticiTakip>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<SaticiTakip>> GetAllAsync() => Task.FromResult<IEnumerable<SaticiTakip>>(Items);
        public Task<SaticiTakip?> GetAsync(string aliciId, string saticiId) => Task.FromResult(Items.FirstOrDefault(x => x.AliciId == aliciId && x.SaticiId == saticiId));
        public Task<IReadOnlyCollection<SaticiTakip>> GetByAliciAsync(string aliciId)
        {
            var list = Items.Where(x => x.AliciId == aliciId).ToList();
            if (_saticiRepo != null)
            {
                foreach (var item in list)
                {
                    item.Satici ??= _saticiRepo.Items.FirstOrDefault(s => s.KullaniciId == item.SaticiId);
                }
            }
            return Task.FromResult<IReadOnlyCollection<SaticiTakip>>(list);
        }
        public Task<SaticiTakip?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<int> GetFollowerCountAsync(string saticiId) => Task.FromResult(Items.Count(x => x.SaticiId == saticiId));
        public Task<IReadOnlyCollection<SaticiTakip>> GetFollowersWithNotificationAsync(string saticiId)
            => Task.FromResult<IReadOnlyCollection<SaticiTakip>>(Items.Where(x => x.SaticiId == saticiId && x.BildirimAcikMi).ToList());
        public void Remove(SaticiTakip entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(SaticiTakip entity) { }
    }

    private sealed class FakeFiyatAlarmiRepository : IFiyatAlarmiRepository
    {
        public List<FiyatAlarmi> Items { get; } = new();
        private int _nextId = 1;

        public Task AddAsync(FiyatAlarmi entity) { if (entity.Id == 0) entity.Id = _nextId++; Items.Add(entity); return Task.CompletedTask; }
        public Task<IEnumerable<FiyatAlarmi>> FindAsync(Expression<Func<FiyatAlarmi, bool>> predicate) => Task.FromResult<IEnumerable<FiyatAlarmi>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<FiyatAlarmi>> GetAllAsync() => Task.FromResult<IEnumerable<FiyatAlarmi>>(Items);
        public Task<FiyatAlarmi?> GetAsync(string aliciId, int urunId) => Task.FromResult(Items.FirstOrDefault(x => x.AliciId == aliciId && x.UrunId == urunId));
        public Task<IReadOnlyCollection<FiyatAlarmi>> GetByAliciWithProductAsync(string aliciId)
            => Task.FromResult<IReadOnlyCollection<FiyatAlarmi>>(Items.Where(x => x.AliciId == aliciId).ToList());
        public Task<FiyatAlarmi?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<IReadOnlyCollection<FiyatAlarmi>> GetMatchingActiveAlertsAsync(int urunId, decimal yeniFiyat)
            => Task.FromResult<IReadOnlyCollection<FiyatAlarmi>>(Items.Where(x =>
                x.UrunId == urunId &&
                x.AktifMi &&
                yeniFiyat <= x.HedefFiyat &&
                (x.SonTetiklenenFiyat == null || yeniFiyat < x.SonTetiklenenFiyat)).ToList());
        public void Remove(FiyatAlarmi entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(FiyatAlarmi entity) { }
    }

    private static async Task TalepService_Should_MatchRelevantSellers_OnOpenMarketDemand()
    {
        var urunRepo = new FakeUrunRepository();
        var saticiRepo = new FakeSaticiProfiliRepository();
        var butterSeller = new SaticiProfili { KullaniciId = "butter-seller", MagazaAdi = "Erzurum Tereyagi", Sehir = "Erzurum", AktifMi = true, VergiNo = "1234567890", Kullanici = new ApplicationUser { Id = "butter-seller", EmailConfirmed = true } };
        var oliveSeller = new SaticiProfili { KullaniciId = "olive-seller", MagazaAdi = "Ege Zeytin", Sehir = "Izmir", AktifMi = true, VergiNo = "9876543210", Kullanici = new ApplicationUser { Id = "olive-seller", EmailConfirmed = true } };
        saticiRepo.Items.AddRange(new[] { butterSeller, oliveSeller });

        urunRepo.Items.Add(new Urun { Id = 1, Adi = "Yayla Tereyagi", Aciklama = "Taze köy tereyagi", KategoriId = 2, SaticiId = "butter-seller", Satici = butterSeller, StokMiktari = 25, Fiyat = 380m, AktifMi = true });
        urunRepo.Items.Add(new Urun { Id = 2, Adi = "Sizma Zeytinyagi", Aciklama = "Soguk sikim", KategoriId = 5, SaticiId = "olive-seller", Satici = oliveSeller, StokMiktari = 10, Fiyat = 450m, AktifMi = true });

        var talepRepo = new FakeTalepRepository(urunRepo);
        var bildirimService = new FakeBildirimService();
        var service = new TalepService(talepRepo, urunRepo, bildirimService, saticiRepo);

        var created = await service.AcikTalepOlusturAsync("buyer-1", new AcikTalepOlusturDto
        {
            ArananUrunAdi = "Tereyagi",
            KategoriId = 2,
            Miktar = 10,
            Birim = "kg",
            TeslimatSehir = "Istanbul",
            TercihEdilenSehirler = "Erzurum, Trabzon",
            TalepTipi = TalepTipleri.Kurumsal
        });

        if (created.EslesenSaticiSayisi != 1 || bildirimService.Created.Count != 1 || bildirimService.Created[0].KullaniciId != "butter-seller")
        {
            throw new InvalidOperationException("Acik pazar talebi yalnizca kategori/sehir ile eslesen ureticiye iletilmeliydi.");
        }

        var teklif = await service.TeklifVerAsync(created.Id, "butter-seller", new TalepTeklifOlusturDto
        {
            BirimFiyat = 355m,
            Mesaj = "2 gunde kargolarim.",
            TeslimatSuresiGun = 2,
            KargoDahilMi = true
        });

        if (teklif.TeslimatSuresiGun != 2 || teklif.GonderimSehir != "Erzurum")
        {
            throw new InvalidOperationException("Acik pazar teklifinde teslimat ve sehir alanlari donmeliydi.");
        }
    }

    private static async Task TalepService_Should_RepeatDemand_AndConfirmDealCompletion()
    {
        var urunRepo = new FakeUrunRepository();
        var bal = new Urun { Id = 33, Adi = "Cicek Bali", SaticiId = "seller-1", AktifMi = true, BirimMiktar = "850 g", StokMiktari = 15, Fiyat = 420m };
        urunRepo.Items.Add(bal);

        var talepRepo = new FakeTalepRepository(urunRepo);
        var bildirimService = new FakeBildirimService();
        var service = new TalepService(talepRepo, urunRepo, bildirimService);

        var first = await service.TalepOlusturAsync("buyer-1", new TalepOlusturDto { UrunId = 33, Miktar = 2, Not = "Ilk siparis" });
        var offer = await service.TeklifVerAsync(first.Id, "seller-1", new TalepTeklifOlusturDto { BirimFiyat = 400m, Mesaj = "Kabul" });
        await service.TeklifKabulEtAsync(offer.Id, "buyer-1");

        var buyerConfirmed = await service.AnlasmaOnayiVerAsync(first.Id, "buyer-1", new AnlasmaOnayiDto { GerceklestiMi = true });
        if (buyerConfirmed.AliciGerceklestiOnayi != true)
        {
            throw new InvalidOperationException("Alici gerceklesme onayi kaydedilmeliydi.");
        }

        var repeated = await service.TalepTekrarlaAsync(first.Id, "buyer-1", new TalepTekrarlaDto { Miktar = 4 });
        if (repeated.Id == first.Id || repeated.Miktar != 4 || repeated.UrunId != 33)
        {
            throw new InvalidOperationException("Tekrar siparis yeni acik talep olarak olusturulmaliydi.");
        }
    }

    private static async Task SaticiTakip_Should_NotifyFollower_WhenSellerAddsNewProduct()
    {
        var saticiRepo = new FakeSaticiProfiliRepository();
        var seller = new SaticiProfili { KullaniciId = "seller-follow", MagazaAdi = "Kars Mandira", Sehir = "Kars", AktifMi = true };
        saticiRepo.Items.Add(seller);

        var takipRepo = new FakeSaticiTakipRepository(saticiRepo);
        var urunRepo = new FakeUrunRepository();
        var saticiService = new SaticiProfiliService(saticiRepo, urunRepo, saticiTakipRepository: takipRepo);

        var added = await saticiService.SaticiTakipEtAsync("buyer-1", "seller-follow", bildirimAcikMi: true);
        if (!added)
        {
            throw new InvalidOperationException("Satici takip kaydi olusturulmaliydi.");
        }

        var bildirimService = new FakeBildirimService();
        var kategoriRepo = new FakeKategoriRepository();
        kategoriRepo.Items.Add(new Kategori { Id = 1, Adi = "Peynir" });

        var urunService = new UrunService(
            urunRepo,
            new FakeUrunFavoriRepository(),
            saticiRepo,
            kategoriRepo,
            new InMemoryDosyaKaydetService(),
            saticiTakipRepository: takipRepo,
            bildirimService: bildirimService);

        await urunService.UrunEkleAsync("seller-follow", new UrunEkleDto
        {
            Adi = "Eski Kars Kasari",
            Fiyat = 550m,
            StokMiktari = 10,
            KategoriId = 1,
            YeniHasatMi = true,
            BirimMiktar = "1 kg"
        });

        if (bildirimService.Created.Count != 1 || bildirimService.Created[0].Tur != BildirimTurleri.Hasat || bildirimService.Created[0].KullaniciId != "buyer-1")
        {
            throw new InvalidOperationException("Yeni hasat urunu eklendiginde takipciye bildirim gitmeliydi.");
        }
    }

    private static async Task UrunService_Should_BulkUpdateStock_AndTriggerPriceAlert()
    {
        var saticiRepo = new FakeSaticiProfiliRepository();
        var seller = new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Toros Bal", Sehir = "Antalya", AktifMi = true };
        saticiRepo.Items.Add(seller);

        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 71, Adi = "Cam Bali", Fiyat = 500m, StokMiktari = 20, SaticiId = "seller-1", Satici = seller, AktifMi = true });

        var alarmRepo = new FakeFiyatAlarmiRepository();
        var bildirimService = new FakeBildirimService();
        var urunService = new UrunService(
            urunRepo,
            new FakeUrunFavoriRepository(),
            saticiRepo,
            new FakeKategoriRepository(),
            new InMemoryDosyaKaydetService(),
            fiyatAlarmiRepository: alarmRepo,
            bildirimService: bildirimService);

        await urunService.FiyatAlarmiKaydetAsync("buyer-1", 71, new FiyatAlarmiOlusturDto { HedefFiyat = 420m });

        var updated = await urunService.TopluStokGuncelleAsync("seller-1", new TopluStokGuncelleDto
        {
            Kalemler = new List<TopluStokKalemiDto>
            {
                new() { UrunId = 71, StokMiktari = 8, Fiyat = 410m, YeniHasatMi = true }
            }
        });

        var item = updated.Single();
        if (item.StokMiktari != 8 || item.Fiyat != 410m || !item.YeniHasatMi)
        {
            throw new InvalidOperationException("Toplu stok/fiyat/hasat guncellemesi uygulanmaliydi.");
        }

        if (bildirimService.Created.Count != 1 || bildirimService.Created[0].Tur != BildirimTurleri.FiyatAlarmi)
        {
            throw new InvalidOperationException("Fiyat hedef seviyenin altina dustugunde fiyat alarmi bildirimi gonderilmeliydi.");
        }
    }

    private static async Task UrunService_Should_IncrementViews_AndExposeCardMetadata()
    {
        var urunRepo = new FakeUrunRepository();
        var seller = new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Posof Organik", Sehir = "Ardahan", Ilce = "Posof", AktifMi = true };
        urunRepo.Items.Add(new Urun
        {
            Id = 88,
            Adi = "Yayla Cicek Bali",
            Fiyat = 490m,
            StokMiktari = 5,
            BirimMiktar = "850 g",
            TeslimatSecenekleri = "Kargo, Elden Teslim",
            YeniHasatMi = true,
            SaticiId = "seller-1",
            Satici = seller,
            AktifMi = true
        });

        var service = new UrunService(
            urunRepo,
            new FakeUrunFavoriRepository(),
            new FakeSaticiProfiliRepository(),
            new FakeKategoriRepository(),
            new InMemoryDosyaKaydetService());

        var dto = await service.GetUrunByIdAsync(88, incrementView: true);
        if (dto == null || dto.GoruntulenmeSayisi != 1 || dto.BirimMiktar != "850 g" || !dto.YeniHasatMi || string.IsNullOrWhiteSpace(dto.Slug))
        {
            throw new InvalidOperationException("Urun detayi goruntulenme sayisini ve zengin urun karti alanlarini donmeliydi.");
        }
    }

    private static async Task UrunService_Kesfet_Should_ReturnAlternativesAndFindForMePrompt_WhenEmpty()
    {
        var urunRepo = new FakeUrunRepository();
        var ortakAlimRepo = new FakeOrtakAlimRepository();

        var ardahanSeller = new SaticiProfili { KullaniciId = "s-ard", MagazaAdi = "Posof Mandira", Sehir = "Ardahan", Ilce = "Posof", AktifMi = true, AdminDogrulandiMi = true };
        urunRepo.Items.Add(new Urun
        {
            Id = 201,
            Adi = "Posof Gogermis Peyniri",
            Fiyat = 380m,
            StokMiktari = 0, // Out of stock, ships only to Ankara
            UretimSehir = "Ardahan",
            UretimIlce = "Posof",
            GonderimSehirleri = "Ardahan, Ankara",
            KargoVarMi = true,
            SaticiId = "s-ard",
            Satici = ardahanSeller,
            AktifMi = true
        });
        urunRepo.Items.Add(new Urun
        {
            Id = 202,
            Adi = "Ardahan Cicek Bali",
            Fiyat = 450m,
            StokMiktari = 12,
            UretimSehir = "Ardahan",
            UretimIlce = "Merkez",
            GonderimSehirleri = "Bursa, Istanbul",
            KargoVarMi = true,
            SaticiId = "s-ard",
            Satici = ardahanSeller,
            AktifMi = true
        });

        ortakAlimRepo.Items.Add(new OrtakAlim
        {
            Id = 5,
            SaticiId = "s-ard",
            Satici = ardahanSeller,
            Baslik = "Posof Peyniri Bursa Ortak Alimi",
            MenseiSehir = "Ardahan",
            HedefSehir = "Bursa",
            BirimFiyat = 340m,
            MinimumKatilimci = 5,
            Durum = OrtakAlimDurumlari.Acik,
            SonKatilimTarihi = DateTime.UtcNow.AddDays(5)
        });

        var service = new UrunService(
            urunRepo,
            new FakeUrunFavoriRepository(),
            new FakeSaticiProfiliRepository(),
            new FakeKategoriRepository(),
            new InMemoryDosyaKaydetService(),
            ortakAlimRepository: ortakAlimRepo);

        // Search "Posof peyniri" in Bursa -> 0 direct active shipping matches for Bursa -> returns smart alternatives
        var result = await service.KesfetAsync(new UrunQuery
        {
            Q = "Posof",
            TeslimatSehir = "Bursa",
            Page = 1,
            PageSize = 12
        });

        if (result.Sonuclar.TotalCount != 0 || result.Alternatifler == null)
        {
            throw new InvalidOperationException("Direkt sonuc olmadiginda akilli alternatifler uretilmeliydi.");
        }

        if (result.Alternatifler.SehreGonderimYapanlar.Count == 0 ||
            result.Alternatifler.StoguTukenenUrunler.Count == 0 ||
            result.Alternatifler.AktifOrtakAlimlar.Count == 0 ||
            !result.Alternatifler.BenimIcinBulOnerisi.UrunBulunamadi)
        {
            throw new InvalidOperationException("Kesfet alternatiflerinde sehrime gonderenler, stogu tukenen, aktif ortak alim ve Benim Icin Bul yer almaliydi.");
        }
    }

    private static async Task UrunService_Should_TriggerStokAlarmi_WhenProductRestocked()
    {
        var urunRepo = new FakeUrunRepository();
        var stokAlarmRepo = new FakeStokAlarmiRepository(urunRepo);
        var saticiRepo = new FakeSaticiProfiliRepository();
        var bildirim = new FakeBildirimService();

        var seller = new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Kars Mandira", Sehir = "Kars", AktifMi = true };
        saticiRepo.Items.Add(seller);
        urunRepo.Items.Add(new Urun
        {
            Id = 91,
            Adi = "Kars Eski Kasar",
            Fiyat = 520m,
            StokMiktari = 0,
            SaticiId = "seller-1",
            Satici = seller,
            AktifMi = true
        });

        var service = new UrunService(
            urunRepo,
            new FakeUrunFavoriRepository(),
            saticiRepo,
            new FakeKategoriRepository(),
            new InMemoryDosyaKaydetService(),
            bildirimService: bildirim,
            stokAlarmiRepository: stokAlarmRepo);

        await service.StokAlarmiKaydetAsync("buyer-1", 91);

        await service.TopluStokGuncelleAsync("seller-1", new TopluStokGuncelleDto
        {
            Kalemler = new List<TopluStokKalemiDto>
            {
                new() { UrunId = 91, StokMiktari = 15 }
            }
        });

        if (bildirim.Created.Count != 1 || bildirim.Created[0].Tur != BildirimTurleri.StokAlarmi)
        {
            throw new InvalidOperationException("Stok 0'dan pozitif degerine gecince StokAlarmi bildirimi gonderilmeliydi.");
        }
    }

    private static async Task OrtakAlimService_Should_SupportProposalApprovalAndParticipantTarget()
    {
        var repo = new FakeOrtakAlimRepository();
        var saticiRepo = new FakeSaticiProfiliRepository();
        var bildirim = new FakeBildirimService();

        var seller = new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Posof Ciftligi", Sehir = "Ardahan", AktifMi = true, AdminDogrulandiMi = true };
        saticiRepo.Items.Add(seller);

        var service = new OrtakAlimService(repo, saticiRepo, new FakeUrunRepository(), bildirim);

        // Buyer proposes a group buy -> starts as ONAY_BEKLIYOR and notifies seller
        var proposal = await service.OlusturAsync("buyer-1", new[] { ApplicationRoles.Alici }, new OrtakAlimOlusturDto
        {
            SaticiId = "seller-1",
            Baslik = "Posof Peyniri Bursa Ortak Siparisi",
            MenseiSehir = "Ardahan",
            HedefSehir = "Bursa",
            BirimFiyat = 350m,
            MinimumKatilimci = 2,
            SogukZincirGerekliMi = true,
            TeslimatKosullari = "Soguk zincirli kargo ile Bursa Nilufer teslim",
            IptalKosullari = "Minimum sayiya ulasmazsa otomatik iptal"
        });

        if (proposal.Durum != OrtakAlimDurumlari.OnayBekliyor)
        {
            throw new InvalidOperationException("Alici tarafindan acilan ortak alim ONAY_BEKLIYOR baslamaliydi.");
        }

        // Seller approves -> becomes ACIK
        var approved = await service.OnaylaAsync(proposal.Id, "seller-1");
        if (approved.Durum != OrtakAlimDurumlari.Acik)
        {
            throw new InvalidOperationException("Satici onayi sonrasinda ortak alim ACIK olmaliydi.");
        }

        // Two buyers join -> reaches MinimumKatilimci (2) -> HEDEF_TAMAMLANDI
        await service.KatilAsync(proposal.Id, "buyer-1", new OrtakAlimKatilDto { KatilimMiktari = 2 });
        var full = await service.KatilAsync(proposal.Id, "buyer-2", new OrtakAlimKatilDto { KatilimMiktari = 3 });

        if (full.Durum != OrtakAlimDurumlari.HedefTamamlandi || full.MevcutKatilimci != 2 || full.ToplamTalepMiktari != 5)
        {
            throw new InvalidOperationException("Minimum katilimciya ulasilinca ortak alim HEDEF_TAMAMLANDI olmaliydi.");
        }
    }

    private sealed class FakePlanliGonderimRepository : IPlanliGonderimRepository
    {
        public List<PlanliGonderim> Items { get; } = new();
        private int _nextId = 1;

        public Task AddAsync(PlanliGonderim entity)
        {
            if (entity.Id == 0) entity.Id = _nextId++;
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<PlanliGonderim>> FindAsync(Expression<Func<PlanliGonderim, bool>> predicate)
            => Task.FromResult<IEnumerable<PlanliGonderim>>(Items.Where(predicate.Compile()).ToList());
        public Task<IEnumerable<PlanliGonderim>> GetAllAsync() => Task.FromResult<IEnumerable<PlanliGonderim>>(Items);
        public Task<PlanliGonderim?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == Convert.ToInt32(id)));
        public Task<PlanliGonderim?> GetByIdWithSellerAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<PlanliGonderim>> GetUpcomingAsync(string? hedefSehir = null, string? menseiSehir = null, string? saticiId = null, int take = 20)
        {
            var q = Items.Where(p => p.AktifMi);
            if (!string.IsNullOrWhiteSpace(hedefSehir))
                q = q.Where(p => p.HedefSehir.Contains(hedefSehir.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyCollection<PlanliGonderim>>(q.Take(take).ToList());
        }
        public void Remove(PlanliGonderim entity) => Items.Remove(entity);
        public Task<bool> SaveChangesAsync() => Task.FromResult(true);
        public void Update(PlanliGonderim entity) { }
    }

    private static async Task TalepService_Should_RejectExpiredOfferDeadline_AndBeIdempotentOnAccept()
    {
        var urunRepo = new FakeUrunRepository();
        urunRepo.Items.Add(new Urun { Id = 301, Adi = "Yayla Tereyagi", SaticiId = "seller-1", AktifMi = true, Fiyat = 320m });

        var talepRepo = new FakeTalepRepository(urunRepo);
        talepRepo.Talepler.Add(new Talep
        {
            Id = 500,
            AliciId = "buyer-1",
            UrunId = 301,
            Urun = urunRepo.Items[0],
            Miktar = 5,
            Durum = TalepDurumlari.Acik,
            SonTeklifTarihi = DateTime.UtcNow.AddMinutes(-10) // Suresi dolmus
        });

        var service = new TalepService(talepRepo, urunRepo, new FakeBildirimService());

        await AssertThrowsAsync<ArgumentException>(() =>
            service.TeklifVerAsync(500, "seller-1", new TalepTeklifOlusturDto { BirimFiyat = 300m, Mesaj = "Gec teklif" }));

        // Gecerli sureli talepte teklif kabul idempotency testi
        var activeTalep = await service.TalepOlusturAsync("buyer-1", new TalepOlusturDto { UrunId = 301, Miktar = 2 });
        var offer = await service.TeklifVerAsync(activeTalep.Id, "seller-1", new TalepTeklifOlusturDto { BirimFiyat = 310m, Mesaj = "Hazir" });

        var acceptedFirst = await service.TeklifKabulEtAsync(offer.Id, "buyer-1");
        var acceptedSecond = await service.TeklifKabulEtAsync(offer.Id, "buyer-1");
        if (acceptedFirst.Durum != TalepDurumlari.Anlasildi || acceptedSecond.Durum != TalepDurumlari.Anlasildi)
        {
            throw new InvalidOperationException("Teklif kabulu idempotent calismaliydi.");
        }
    }

    private static async Task ChatService_Should_PersistProductAndDemandContext()
    {
        var chatRepo = new FakeChatMessageRepository();
        chatRepo.Users["buyer-1"] = new ApplicationUser { Id = "buyer-1", UserName = "buyer", Email = "b@y.com" };
        chatRepo.Users["seller-1"] = new ApplicationUser { Id = "seller-1", UserName = "seller", Email = "s@y.com" };

        var service = new ChatService(chatRepo, new FakeBildirimService());

        var msg = await service.SendMessageAsync("buyer-1", "seller-1", "Posof peyniri kargo durumu?", urunId: 42, talepId: 11);
        if (msg.UrunId != 42 || msg.TalepId != 11)
        {
            throw new InvalidOperationException("Chat mesajinda UrunId ve TalepId baglami saklanmaliydi.");
        }

        var filtered = await service.GetConversationMessagesAsync("buyer-1", "seller-1", 1, 20, urunId: 42, talepId: 11);
        if (filtered.Items.Count != 1)
        {
            throw new InvalidOperationException("Urun/Talep baglamina gore mesajlar filtrelenmeliydi.");
        }
    }

    private static async Task PlanliGonderimService_Should_CreateAndNotifyFollowers()
    {
        var repo = new FakePlanliGonderimRepository();
        var saticiRepo = new FakeSaticiProfiliRepository();
        var seller = new SaticiProfili { KullaniciId = "seller-1", MagazaAdi = "Ardahan Mandira", Sehir = "Ardahan", AktifMi = true };
        saticiRepo.Items.Add(seller);

        var takipRepo = new FakeSaticiTakipRepository(saticiRepo);
        takipRepo.Items.Add(new SaticiTakip { AliciId = "buyer-1", SaticiId = "seller-1", BildirimAcikMi = true });
        var bildirim = new FakeBildirimService();

        var service = new PlanliGonderimService(repo, saticiRepo, takipRepo, bildirim);
        var created = await service.OlusturAsync("seller-1", new PlanliGonderimOlusturDto
        {
            Baslik = "Cuma Bursa Yoresel Peynir Sevkiyati",
            MenseiSehir = "Ardahan",
            HedefSehir = "Bursa",
            TahminiUrunNotu = "Posof gogermis ve eski kasar",
            GonderimTarihi = DateTime.UtcNow.AddDays(3),
            SonTalepTarihi = DateTime.UtcNow.AddDays(2),
            SogukZincirVarMi = true
        });

        if (created.HedefSehir != "Bursa" || !created.SogukZincirVarMi || bildirim.Created.Count != 1 || bildirim.Created[0].Tur != BildirimTurleri.GonderimPlani)
        {
            throw new InvalidOperationException("Planli gonderim olusturulup takipciye GONDERIM_PLANI bildirimi gitmeliydi.");
        }

        var upcoming = await service.GetUpcomingAsync(hedefSehir: "Bursa");
        if (upcoming.Count != 1)
        {
            throw new InvalidOperationException("Bursa hedef sehrine planli gonderim listelenmeliydi.");
        }
    }
}
