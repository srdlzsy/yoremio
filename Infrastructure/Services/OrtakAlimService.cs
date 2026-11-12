using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;

namespace Infrastructure.Services
{
    public class OrtakAlimService : IOrtakAlimService
    {
        private readonly IOrtakAlimRepository _ortakAlimRepository;
        private readonly ISaticiProfiliRepository _saticiRepository;
        private readonly IUrunRepository _urunRepository;
        private readonly IBildirimService _bildirimService;

        public OrtakAlimService(
            IOrtakAlimRepository ortakAlimRepository,
            ISaticiProfiliRepository saticiRepository,
            IUrunRepository urunRepository,
            IBildirimService bildirimService)
        {
            _ortakAlimRepository = ortakAlimRepository;
            _saticiRepository = saticiRepository;
            _urunRepository = urunRepository;
            _bildirimService = bildirimService;
        }

        public async Task<PagedResult<OrtakAlimDto>> GetAllAsync(string? hedefSehir, string? menseiSehir, string? q, string? durum, int page = 1, int pageSize = 12)
        {
            var paged = await _ortakAlimRepository.QueryAsync(hedefSehir, menseiSehir, q, durum, page, pageSize);
            return new PagedResult<OrtakAlimDto>
            {
                Items = paged.Items.Select(MapToDto).ToArray(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };
        }

        public async Task<OrtakAlimDto?> GetByIdAsync(int id)
        {
            var entity = await _ortakAlimRepository.GetByIdWithDetailsAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<OrtakAlimDto> OlusturAsync(string userId, IReadOnlyCollection<string> roles, OrtakAlimOlusturDto dto)
        {
            var isSeller = roles.Any(r => string.Equals(r, ApplicationRoles.Satici, StringComparison.OrdinalIgnoreCase));
            var hedefSaticiId = isSeller && string.IsNullOrWhiteSpace(dto.SaticiId) ? userId : dto.SaticiId?.Trim();

            Urun? urun = null;
            if (dto.UrunId.HasValue)
            {
                urun = await _urunRepository.GetByIdWithRelationsAsync(dto.UrunId.Value);
                if (urun == null)
                {
                    throw new KeyNotFoundException("Secilen urun bulunamadi.");
                }

                hedefSaticiId ??= urun.SaticiId;
            }

            if (string.IsNullOrWhiteSpace(hedefSaticiId))
            {
                throw new ArgumentException("Ortak alim icin satici veya urun secilmelidir.");
            }

            var satici = await _saticiRepository.GetByIdAsync(hedefSaticiId);
            if (satici == null || !satici.AktifMi)
            {
                throw new KeyNotFoundException("Satici bulunamadi.");
            }

            if (dto.SonKatilimTarihi <= DateTime.UtcNow)
            {
                throw new ArgumentException("Son katilim tarihi gelecekte olmalidir.");
            }

            var isOwnerSeller = isSeller && string.Equals(hedefSaticiId, userId, StringComparison.Ordinal);

            var entity = new OrtakAlim
            {
                SaticiId = hedefSaticiId,
                OlusturanKullaniciId = userId,
                UrunId = dto.UrunId,
                KategoriId = dto.KategoriId ?? urun?.KategoriId,
                Baslik = dto.Baslik.Trim(),
                Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
                MenseiSehir = dto.MenseiSehir.Trim(),
                HedefSehir = dto.HedefSehir.Trim(),
                BirimFiyat = dto.BirimFiyat,
                Birim = string.IsNullOrWhiteSpace(dto.Birim) ? "kg" : dto.Birim.Trim(),
                MinimumKatilimci = dto.MinimumKatilimci,
                SogukZincirGerekliMi = dto.SogukZincirGerekliMi,
                TeslimatKosullari = string.IsNullOrWhiteSpace(dto.TeslimatKosullari) ? null : dto.TeslimatKosullari.Trim(),
                IptalKosullari = string.IsNullOrWhiteSpace(dto.IptalKosullari) ? null : dto.IptalKosullari.Trim(),
                Durum = isOwnerSeller ? OrtakAlimDurumlari.Acik : OrtakAlimDurumlari.OnayBekliyor,
                SonKatilimTarihi = dto.SonKatilimTarihi.ToUniversalTime()
            };

            await _ortakAlimRepository.AddAsync(entity);
            await _ortakAlimRepository.SaveChangesAsync();

            if (!isOwnerSeller)
            {
                entity.Katilimlar.Add(new OrtakAlimKatilim
                {
                    OrtakAlimId = entity.Id,
                    AliciId = userId,
                    KatilimMiktari = 1,
                    AktifMi = true
                });
                await _ortakAlimRepository.SaveChangesAsync();

                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = hedefSaticiId,
                    Tur = BildirimTurleri.OrtakAlim,
                    Baslik = "Yeni ortak alim talebi",
                    Mesaj = $"{entity.HedefSehir} icin \"{entity.Baslik}\" ortak alim onerisi onayinizi bekliyor.",
                    IlgiliVarlikTuru = "OrtakAlim",
                    IlgiliVarlikId = entity.Id.ToString(),
                    AksiyonUrl = $"/ortak-alim/{entity.Id}"
                });
            }

            var created = await _ortakAlimRepository.GetByIdWithDetailsAsync(entity.Id) ?? entity;
            return MapToDto(created);
        }

        public async Task<OrtakAlimDto> OnaylaAsync(int id, string saticiId)
        {
            var entity = await _ortakAlimRepository.GetByIdWithDetailsAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Ortak alim bulunamadi.");
            }

            if (!string.Equals(entity.SaticiId, saticiId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu ortak alimi onaylama yetkiniz yok.");
            }

            if (entity.Durum != OrtakAlimDurumlari.OnayBekliyor)
            {
                throw new ArgumentException("Sadece onay bekleyen ortak alimlar acilabilir.");
            }

            entity.Durum = OrtakAlimDurumlari.Acik;
            entity.GuncellemeTarihi = DateTime.UtcNow;
            _ortakAlimRepository.Update(entity);
            await _ortakAlimRepository.SaveChangesAsync();

            foreach (var katilim in entity.Katilimlar.Where(k => k.AktifMi))
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = katilim.AliciId,
                    Tur = BildirimTurleri.OrtakAlim,
                    Baslik = "Ortak alim onaylandi",
                    Mesaj = $"{entity.Baslik} uretici tarafindan onaylandi ve katilima acildi.",
                    IlgiliVarlikTuru = "OrtakAlim",
                    IlgiliVarlikId = entity.Id.ToString(),
                    AksiyonUrl = $"/ortak-alim/{entity.Id}"
                });
            }

            return MapToDto(entity);
        }

        public async Task<OrtakAlimDto> KatilAsync(int id, string aliciId, OrtakAlimKatilDto? dto = null)
        {
            var entity = await _ortakAlimRepository.GetByIdWithDetailsAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Ortak alim bulunamadi.");
            }

            if (string.Equals(entity.SaticiId, aliciId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Kendi baslattiginiz ortak alima alici olarak katilamazsiniz.");
            }

            if (entity.Durum != OrtakAlimDurumlari.Acik && entity.Durum != OrtakAlimDurumlari.HedefTamamlandi)
            {
                throw new ArgumentException("Bu ortak alim su an katilima acik degil.");
            }

            if (entity.SonKatilimTarihi <= DateTime.UtcNow)
            {
                throw new ArgumentException("Ortak alimin son katilim tarihi gecmis.");
            }

            var miktar = dto?.KatilimMiktari ?? 1;
            var not = string.IsNullOrWhiteSpace(dto?.Not) ? null : dto!.Not.Trim();

            var mevcut = entity.Katilimlar.FirstOrDefault(k => string.Equals(k.AliciId, aliciId, StringComparison.Ordinal));
            if (mevcut != null)
            {
                mevcut.KatilimMiktari = miktar;
                mevcut.Not = not;
                mevcut.AktifMi = true;
            }
            else
            {
                var yeniKatilim = new OrtakAlimKatilim
                {
                    OrtakAlimId = entity.Id,
                    AliciId = aliciId,
                    KatilimMiktari = miktar,
                    Not = not,
                    AktifMi = true
                };
                entity.Katilimlar.Add(yeniKatilim);
                await _ortakAlimRepository.AddKatilimAsync(yeniKatilim);
            }

            var aktifKatilimci = entity.Katilimlar.Count(k => k.AktifMi);
            var hedefYeniTamamlandi = entity.Durum == OrtakAlimDurumlari.Acik && aktifKatilimci >= entity.MinimumKatilimci;
            if (hedefYeniTamamlandi)
            {
                entity.Durum = OrtakAlimDurumlari.HedefTamamlandi;
            }

            entity.GuncellemeTarihi = DateTime.UtcNow;
            _ortakAlimRepository.Update(entity);
            await _ortakAlimRepository.SaveChangesAsync();

            if (hedefYeniTamamlandi)
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = entity.SaticiId,
                    Tur = BildirimTurleri.OrtakAlim,
                    Baslik = "Ortak alim hedefine ulasti",
                    Mesaj = $"{entity.Baslik} icin {aktifKatilimci}/{entity.MinimumKatilimci} katilimci hedefine ulasildi.",
                    IlgiliVarlikTuru = "OrtakAlim",
                    IlgiliVarlikId = entity.Id.ToString(),
                    AksiyonUrl = $"/ortak-alim/{entity.Id}"
                });
            }

            return MapToDto(entity);
        }

        public async Task<OrtakAlimDto> KatilimiIptalEtAsync(int id, string aliciId)
        {
            var entity = await _ortakAlimRepository.GetByIdWithDetailsAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Ortak alim bulunamadi.");
            }

            if (entity.Durum == OrtakAlimDurumlari.TeslimEdildi || entity.Durum == OrtakAlimDurumlari.Iptal)
            {
                throw new ArgumentException("Sonuclanmis ortak alimdan cikilamaz.");
            }

            var katilim = entity.Katilimlar.FirstOrDefault(k => string.Equals(k.AliciId, aliciId, StringComparison.Ordinal) && k.AktifMi);
            if (katilim == null)
            {
                throw new KeyNotFoundException("Aktif katilim kaydiniz bulunamadi.");
            }

            katilim.AktifMi = false;
            var aktifKatilimci = entity.Katilimlar.Count(k => k.AktifMi);
            if (entity.Durum == OrtakAlimDurumlari.HedefTamamlandi && aktifKatilimci < entity.MinimumKatilimci)
            {
                entity.Durum = OrtakAlimDurumlari.Acik;
            }

            entity.GuncellemeTarihi = DateTime.UtcNow;
            _ortakAlimRepository.Update(entity);
            await _ortakAlimRepository.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<OrtakAlimDto> DurumGuncelleAsync(int id, string saticiId, string yeniDurum)
        {
            var entity = await _ortakAlimRepository.GetByIdWithDetailsAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Ortak alim bulunamadi.");
            }

            if (!string.Equals(entity.SaticiId, saticiId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu ortak alim uzerinde islem yetkiniz yok.");
            }

            var normalized = yeniDurum.Trim().ToUpperInvariant();
            if (normalized != OrtakAlimDurumlari.Acik &&
                normalized != OrtakAlimDurumlari.TeslimEdildi &&
                normalized != OrtakAlimDurumlari.Iptal)
            {
                throw new ArgumentException("Gecersiz ortak alim durumu. ACIK, TESLIM_EDILDI veya IPTAL kullanin.");
            }

            entity.Durum = normalized;
            entity.GuncellemeTarihi = DateTime.UtcNow;
            _ortakAlimRepository.Update(entity);
            await _ortakAlimRepository.SaveChangesAsync();

            foreach (var katilim in entity.Katilimlar.Where(k => k.AktifMi))
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = katilim.AliciId,
                    Tur = BildirimTurleri.OrtakAlim,
                    Baslik = "Ortak alim durumu guncellendi",
                    Mesaj = $"{entity.Baslik} durumu \"{normalized}\" olarak guncellendi.",
                    IlgiliVarlikTuru = "OrtakAlim",
                    IlgiliVarlikId = entity.Id.ToString(),
                    AksiyonUrl = $"/ortak-alim/{entity.Id}"
                });
            }

            return MapToDto(entity);
        }

        internal static OrtakAlimDto MapToDto(OrtakAlim o)
        {
            var aktifKatilimlar = o.Katilimlar.Where(k => k.AktifMi).ToList();
            return new OrtakAlimDto
            {
                Id = o.Id,
                SaticiId = o.SaticiId,
                SaticiMagazaAdi = o.Satici?.MagazaAdi,
                SaticiDogrulanmis = SaticiProfili.IsVerified(o.Satici),
                OlusturanKullaniciId = o.OlusturanKullaniciId,
                UrunId = o.UrunId,
                UrunAdi = o.Urun?.Adi,
                UrunResimUrl = o.Urun?.Resimler?.Select(r => r.Url).FirstOrDefault(),
                KategoriId = o.KategoriId,
                KategoriAdi = o.Kategori?.Adi,
                Baslik = o.Baslik,
                Aciklama = o.Aciklama,
                MenseiSehir = o.MenseiSehir,
                HedefSehir = o.HedefSehir,
                BirimFiyat = o.BirimFiyat,
                Birim = o.Birim,
                MinimumKatilimci = o.MinimumKatilimci,
                MevcutKatilimci = aktifKatilimlar.Count,
                ToplamTalepMiktari = aktifKatilimlar.Sum(k => k.KatilimMiktari),
                SogukZincirGerekliMi = o.SogukZincirGerekliMi,
                TeslimatKosullari = o.TeslimatKosullari,
                IptalKosullari = o.IptalKosullari,
                Durum = o.Durum,
                SonKatilimTarihi = o.SonKatilimTarihi,
                OlusturmaTarihi = o.OlusturmaTarihi
            };
        }
    }
}
