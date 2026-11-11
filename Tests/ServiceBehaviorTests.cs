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
            ("KategoriService tekil isim ve urunlu kategori silme kontrolu yapmali", KategoriService_Should_EnforceUniqueNameAndProductGuard)
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
        public Task<IReadOnlyCollection<Urun>> GetRecommendedAsync(IReadOnlyCollection<int> kategoriIdleri, IReadOnlyCollection<int> haricUrunIdleri, int take)
            => Task.FromResult<IReadOnlyCollection<Urun>>(Items.Take(take).ToList());
        public Task<PagedResult<Urun>> QueryAsync(UrunQuery query) => Task.FromResult(new PagedResult<Urun>());
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

        public Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId)
            => Task.FromResult<IReadOnlyCollection<Talep>>(Talepler.Where(t => t.AliciId == aliciId).ToList());

        public Task<IEnumerable<Talep>> GetAllAsync() => Task.FromResult<IEnumerable<Talep>>(Talepler);
        public Task<Talep?> GetByIdAsync(object id) => Task.FromResult(Talepler.FirstOrDefault(t => t.Id == Convert.ToInt32(id)));
        public Task<Talep?> GetByIdWithDetailsAsync(int talepId) => Task.FromResult(Talepler.FirstOrDefault(t => t.Id == talepId));
        public Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId)
            => Task.FromResult<IReadOnlyCollection<Talep>>(Talepler.Where(t => t.Urun?.SaticiId == saticiId).ToList());
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
        public Task AddAsync(Puan entity) => Task.CompletedTask;
        public Task<IEnumerable<Puan>> FindAsync(Expression<Func<Puan, bool>> predicate) => Task.FromResult<IEnumerable<Puan>>(Array.Empty<Puan>());
        public Task<IEnumerable<Puan>> GetAllAsync() => Task.FromResult<IEnumerable<Puan>>(Array.Empty<Puan>());
        public Task<Puan?> GetByIdAsync(object id) => Task.FromResult<Puan?>(null);
        public Task<int> GetOrCreatePuanAsync(int urunId, string kullaniciId, int puanDegeri) => Task.FromResult(puanDegeri);
        public Task<double> GetOrtalamaPuanByUrunIdAsync(int urunId) => Task.FromResult(0d);
        public Task<Puan?> GetPuanByUrunIdAndKullaniciIdAsync(int urunId, string kullaniciId) => Task.FromResult<Puan?>(null);
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
        public Task<int> GetConversationMessageCountAsync(string firstUserId, string secondUserId) => Task.FromResult(0);
        public Task<IReadOnlyCollection<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId, int skip, int take)
            => Task.FromResult<IReadOnlyCollection<ChatMessage>>(Array.Empty<ChatMessage>());
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

        public Task AddAsync(Kategori entity) { Items.Add(entity); return Task.CompletedTask; }
        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
            => Task.FromResult(Items.Any(k => string.Equals(k.Adi, name.Trim(), StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || k.Id != excludeId.Value)));
        public Task<IEnumerable<Kategori>> FindAsync(Expression<Func<Kategori, bool>> predicate)
            => Task.FromResult<IEnumerable<Kategori>>(Items.Where(predicate.Compile()).ToList());
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
}
