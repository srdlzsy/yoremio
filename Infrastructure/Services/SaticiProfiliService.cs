using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class SaticiProfiliService : BaseService<SaticiProfili>, ISaticiProfiliService
    {
        private readonly ISaticiProfiliRepository _saticiRepo;
        private readonly IUrunRepository _urunRepository;

        public SaticiProfiliService(ISaticiProfiliRepository saticiRepo, IUrunRepository urunRepository)
            : base(saticiRepo)
        {
            _saticiRepo = saticiRepo;
            _urunRepository = urunRepository;
        }

        public async Task<SaticiProfili?> GetByVergiNoAsync(string vergiNo)
        {
            var result = await _saticiRepo.FindAsync(x => x.VergiNo == vergiNo);
            return result.FirstOrDefault();
        }

        public async Task<SaticiProfili?> GetSaticiWithUserByIdAsync(string kullaniciId)
        {
            return await _saticiRepo.GetSaticiWithUserByIdAsync(kullaniciId);
        }

        public async Task<SaticiProfilDto?> GetSaticiProfilDtoAsync(string kullaniciId)
        {
            var satici = await _saticiRepo.GetSaticiWithUserByIdAsync(kullaniciId);
            return satici is null ? null : MapToDto(satici);
        }

        public async Task<SaticiProfilDto?> UpdateProfilAsync(string kullaniciId, SaticiProfilGuncelleDto dto)
        {
            var satici = await _saticiRepo.GetSaticiWithUserByIdAsync(kullaniciId);
            if (satici is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.MagazaAdi))
            {
                satici.MagazaAdi = dto.MagazaAdi.Trim();
            }

            if (dto.Adres is not null)
            {
                satici.Adres = string.IsNullOrWhiteSpace(dto.Adres) ? null : dto.Adres.Trim();
            }

            if (dto.Sehir is not null)
            {
                satici.Sehir = string.IsNullOrWhiteSpace(dto.Sehir) ? null : dto.Sehir.Trim();
            }

            if (dto.Ilce is not null)
            {
                satici.Ilce = string.IsNullOrWhiteSpace(dto.Ilce) ? null : dto.Ilce.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && satici.Kullanici is not null)
            {
                satici.Kullanici.PhoneNumber = dto.PhoneNumber.Trim();
            }

            _saticiRepo.Update(satici);
            await _saticiRepo.SaveChangesAsync();

            return MapToDto(satici);
        }

        public async Task<SaticiGuvenSkoruDto?> GetGuvenSkoruAsync(string saticiId)
        {
            var satici = await _saticiRepo.GetSaticiWithUserByIdAsync(saticiId);
            if (satici is null)
            {
                return null;
            }

            var urunler = (await _urunRepository.FindAsync(u => u.SaticiId == saticiId && u.AktifMi)).ToList();

            var toplamPuan = urunler.Sum(u => u.Puanlar.Count);
            var toplamYorum = urunler.Sum(u => u.Yorumlar.Count);
            var toplamFavori = urunler.Sum(u => u.Favoriler.Count);
            var urunSayisi = urunler.Count;

            var ortalamaPuan = toplamPuan == 0
                ? 0
                : Math.Round(urunler.SelectMany(u => u.Puanlar).Average(p => p.PuanDegeri), 1);

            var dogrulanmisSatici = IsSellerVerified(satici);

            var kayitGun = (DateTime.UtcNow - satici.KayitTarihi).TotalDays;
            var puanSkoru = (ortalamaPuan / 5d) * 45d;
            var yorumSkoru = (Math.Min(toplamYorum, 50) / 50d) * 20d;
            var favoriSkoru = (Math.Min(toplamFavori, 100) / 100d) * 15d;
            var deneyimSkoru = (Math.Min(kayitGun, 365) / 365d) * 10d;
            var dogrulamaSkoru = dogrulanmisSatici ? 10d : 0d;
            var guvenSkoru = Math.Round(Math.Min(100, puanSkoru + yorumSkoru + favoriSkoru + deneyimSkoru + dogrulamaSkoru), 1);

            return new SaticiGuvenSkoruDto
            {
                KullaniciId = satici.KullaniciId,
                MagazaAdi = satici.MagazaAdi,
                DogrulanmisSatici = dogrulanmisSatici,
                UrunSayisi = urunSayisi,
                OrtalamaPuan = ortalamaPuan,
                ToplamPuan = toplamPuan,
                ToplamYorum = toplamYorum,
                ToplamFavori = toplamFavori,
                GuvenSkoru = guvenSkoru
            };
        }

        public async Task<IReadOnlyCollection<SaticiOzetDto>> GetOneCikanSaticilarAsync(int take)
        {
            var effectiveTake = take <= 0 ? 6 : Math.Min(take, 24);
            var saticilar = await _saticiRepo.GetOneCikanSaticilarAsync(effectiveTake);

            return saticilar
                .Select(MapToOzetDto)
                .OrderByDescending(s => s.GuvenSkoru)
                .ThenByDescending(s => s.OrtalamaPuan)
                .ThenByDescending(s => s.ToplamFavori)
                .ThenBy(s => s.MagazaAdi)
                .Take(effectiveTake)
                .ToList();
        }

        private static SaticiProfilDto MapToDto(SaticiProfili satici)
        {
            return new SaticiProfilDto
            {
                KullaniciId = satici.KullaniciId,
                MagazaAdi = satici.MagazaAdi,
                VergiNo = satici.VergiNo,
                Adres = satici.Adres,
                Sehir = satici.Sehir,
                Ilce = satici.Ilce,
                KayitTarihi = satici.KayitTarihi,
                AktifMi = satici.AktifMi,
                DogrulanmisSatici = IsSellerVerified(satici),
                Email = satici.Kullanici?.Email,
                UserName = satici.Kullanici?.UserName,
                PhoneNumber = satici.Kullanici?.PhoneNumber
            };
        }

        private static SaticiOzetDto MapToOzetDto(SaticiProfili satici)
        {
            var aktifUrunler = satici.Urunler.Where(u => u.AktifMi).ToList();
            var toplamPuan = aktifUrunler.Sum(u => u.Puanlar.Count);
            var toplamYorum = aktifUrunler.Sum(u => u.Yorumlar.Count);
            var toplamFavori = aktifUrunler.Sum(u => u.Favoriler.Count);
            var ortalamaPuan = toplamPuan == 0
                ? 0
                : Math.Round(aktifUrunler.SelectMany(u => u.Puanlar).Average(p => p.PuanDegeri), 1);
            var guvenSkoru = CalculateTrustScore(satici, aktifUrunler, ortalamaPuan, toplamYorum, toplamFavori);
            var vitrinUrunler = aktifUrunler
                .OrderByDescending(u => u.Favoriler.Count)
                .ThenByDescending(u => u.Puanlar.Count == 0 ? 0 : u.Puanlar.Average(p => p.PuanDegeri))
                .ThenByDescending(u => u.Yorumlar.Count)
                .ThenByDescending(u => u.OlusturmaTarihi)
                .ToList();
            var vitrinUrun = vitrinUrunler.FirstOrDefault();

            return new SaticiOzetDto
            {
                KullaniciId = satici.KullaniciId,
                MagazaAdi = satici.MagazaAdi,
                Sehir = satici.Sehir,
                Ilce = satici.Ilce,
                DogrulanmisSatici = IsSellerVerified(satici),
                UrunSayisi = aktifUrunler.Count,
                OrtalamaPuan = ortalamaPuan,
                ToplamYorum = toplamYorum,
                ToplamFavori = toplamFavori,
                GuvenSkoru = guvenSkoru,
                VitrinUrunId = vitrinUrun?.Id,
                KapakResimUrl = vitrinUrunler
                    .SelectMany(u => u.Resimler)
                    .Select(r => r.Url)
                    .FirstOrDefault()
            };
        }

        private static double CalculateTrustScore(
            SaticiProfili satici,
            IReadOnlyCollection<Urun> urunler,
            double ortalamaPuan,
            int toplamYorum,
            int toplamFavori)
        {
            var kayitGun = (DateTime.UtcNow - satici.KayitTarihi).TotalDays;
            var puanSkoru = (ortalamaPuan / 5d) * 45d;
            var yorumSkoru = (Math.Min(toplamYorum, 50) / 50d) * 20d;
            var favoriSkoru = (Math.Min(toplamFavori, 100) / 100d) * 15d;
            var deneyimSkoru = (Math.Min(kayitGun, 365) / 365d) * 10d;
            var dogrulamaSkoru = IsSellerVerified(satici) ? 10d : 0d;
            var urunSkoru = Math.Min(urunler.Count, 20) / 20d * 5d;

            return Math.Round(Math.Min(100, puanSkoru + yorumSkoru + favoriSkoru + deneyimSkoru + dogrulamaSkoru + urunSkoru), 1);
        }

        private static bool IsSellerVerified(SaticiProfili satici)
        {
            return !string.IsNullOrWhiteSpace(satici.VergiNo)
                && satici.Kullanici is not null
                && satici.Kullanici.EmailConfirmed;
        }
    }
}
