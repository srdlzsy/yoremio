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
        private readonly IDosyaKaydetService? _dosyaKaydetService;
        private readonly ISaticiTakipRepository? _saticiTakipRepository;

        public SaticiProfiliService(
            ISaticiProfiliRepository saticiRepo,
            IUrunRepository urunRepository,
            IDosyaKaydetService? dosyaKaydetService = null,
            ISaticiTakipRepository? saticiTakipRepository = null)
            : base(saticiRepo)
        {
            _saticiRepo = saticiRepo;
            _urunRepository = urunRepository;
            _dosyaKaydetService = dosyaKaydetService;
            _saticiTakipRepository = saticiTakipRepository;
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

            if (dto.Aciklama is not null)
            {
                satici.Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && satici.Kullanici is not null)
            {
                satici.Kullanici.PhoneNumber = dto.PhoneNumber.Trim();
            }

            if (dto.KapakResim is not null && dto.KapakResim.Length > 0)
            {
                if (_dosyaKaydetService is null)
                {
                    throw new InvalidOperationException("Dosya kaydetme servisi yapilandirilmamis.");
                }

                var eskiKapakUrl = satici.KapakResimUrl;
                var yeniKapakUrl = await _dosyaKaydetService.KaydetDosyaAsync(dto.KapakResim, Path.Combine("saticilar", kullaniciId));
                satici.KapakResimUrl = yeniKapakUrl;

                if (!string.IsNullOrWhiteSpace(eskiKapakUrl))
                {
                    try
                    {
                        await _dosyaKaydetService.SilDosyaAsync(eskiKapakUrl);
                    }
                    catch
                    {
                        // Eski kapak resmi temizligi best-effort uygulanir.
                    }
                }
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
            var toplamYorum = urunler.Sum(u => u.Yorumlar.Count(y => y.AktifMi));
            var toplamFavori = urunler.Sum(u => u.Favoriler.Count);
            var urunSayisi = urunler.Count;

            var ortalamaPuan = toplamPuan == 0
                ? 0
                : Math.Round(urunler.SelectMany(u => u.Puanlar).Average(p => p.PuanDegeri), 1);

            var dogrulanmisSatici = IsSellerVerified(satici);
            var guvenSkoru = CalculateTrustScore(satici, urunler, ortalamaPuan, toplamYorum, toplamFavori);

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

        public async Task<SaticiOzetDto?> GetPublicSaticiDetayAsync(string saticiId)
        {
            var satici = await _saticiRepo.GetSaticiPublicDetailsByIdAsync(saticiId);
            if (satici is null)
            {
                return null;
            }

            var takipciSayisi = _saticiTakipRepository is null ? 0 : await _saticiTakipRepository.GetFollowerCountAsync(saticiId);
            return MapToOzetDto(satici, takipciSayisi);
        }

        public async Task<IReadOnlyCollection<SaticiOzetDto>> GetOneCikanSaticilarAsync(int take)
        {
            var effectiveTake = take <= 0 ? 6 : Math.Min(take, 24);
            var saticilar = await _saticiRepo.GetOneCikanSaticilarAsync(effectiveTake);

            return saticilar
                .Select(s => MapToOzetDto(s))
                .OrderByDescending(s => s.GuvenSkoru)
                .ThenByDescending(s => s.OrtalamaPuan)
                .ThenByDescending(s => s.ToplamFavori)
                .ThenBy(s => s.MagazaAdi)
                .Take(effectiveTake)
                .ToList();
        }

        public async Task<bool> SaticiTakipEtAsync(string aliciId, string saticiId, bool bildirimAcikMi = true)
        {
            if (string.Equals(aliciId, saticiId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Kendi magazanizi takip edemezsiniz.");
            }

            if (_saticiTakipRepository is null)
            {
                throw new InvalidOperationException("Takip servisi yapilandirilmamis.");
            }

            var satici = await _saticiRepo.GetByIdAsync(saticiId);
            if (satici is null || !satici.AktifMi)
            {
                throw new KeyNotFoundException("Satici bulunamadi.");
            }

            var mevcut = await _saticiTakipRepository.GetAsync(aliciId, saticiId);
            if (mevcut is not null)
            {
                if (mevcut.BildirimAcikMi != bildirimAcikMi)
                {
                    mevcut.BildirimAcikMi = bildirimAcikMi;
                    _saticiTakipRepository.Update(mevcut);
                    await _saticiTakipRepository.SaveChangesAsync();
                }

                return false;
            }

            await _saticiTakipRepository.AddAsync(new SaticiTakip
            {
                AliciId = aliciId,
                SaticiId = saticiId,
                BildirimAcikMi = bildirimAcikMi
            });

            return await _saticiTakipRepository.SaveChangesAsync();
        }

        public async Task<bool> SaticiTakibiBirakAsync(string aliciId, string saticiId)
        {
            if (_saticiTakipRepository is null)
            {
                throw new InvalidOperationException("Takip servisi yapilandirilmamis.");
            }

            var mevcut = await _saticiTakipRepository.GetAsync(aliciId, saticiId);
            if (mevcut is null)
            {
                return false;
            }

            _saticiTakipRepository.Remove(mevcut);
            return await _saticiTakipRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<SaticiOzetDto>> GetTakipEdilenSaticilarAsync(string aliciId)
        {
            if (_saticiTakipRepository is null)
            {
                return Array.Empty<SaticiOzetDto>();
            }

            var takipler = await _saticiTakipRepository.GetByAliciAsync(aliciId);
            return takipler
                .Where(t => t.Satici is not null)
                .Select(t => MapToOzetDto(t.Satici!))
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
                Aciklama = satici.Aciklama,
                KapakResimUrl = satici.KapakResimUrl,
                KayitTarihi = satici.KayitTarihi,
                AktifMi = satici.AktifMi,
                PaketTuru = satici.PaketTuru,
                DogrulanmisSatici = IsSellerVerified(satici),
                AdminDogrulamaDurumu = satici.AdminDogrulandiMi,
                Email = satici.Kullanici?.Email,
                UserName = satici.Kullanici?.UserName,
                PhoneNumber = satici.Kullanici?.PhoneNumber
            };
        }

        private static SaticiOzetDto MapToOzetDto(SaticiProfili satici, int takipciSayisi = 0)
        {
            var aktifUrunler = satici.Urunler.Where(u => u.AktifMi).ToList();
            var toplamPuan = aktifUrunler.Sum(u => u.Puanlar.Count);
            var toplamYorum = aktifUrunler.Sum(u => u.Yorumlar.Count(y => y.AktifMi));
            var toplamFavori = aktifUrunler.Sum(u => u.Favoriler.Count);
            var ortalamaPuan = toplamPuan == 0
                ? 0
                : Math.Round(aktifUrunler.SelectMany(u => u.Puanlar).Average(p => p.PuanDegeri), 1);
            var guvenSkoru = CalculateTrustScore(satici, aktifUrunler, ortalamaPuan, toplamYorum, toplamFavori);
            var vitrinUrunler = aktifUrunler
                .OrderByDescending(u => u.Favoriler.Count)
                .ThenByDescending(u => u.Puanlar.Count == 0 ? 0 : u.Puanlar.Average(p => p.PuanDegeri))
                .ThenByDescending(u => u.Yorumlar.Count(y => y.AktifMi))
                .ThenByDescending(u => u.OlusturmaTarihi)
                .ToList();
            var vitrinUrun = vitrinUrunler.FirstOrDefault();

            return new SaticiOzetDto
            {
                KullaniciId = satici.KullaniciId,
                MagazaAdi = satici.MagazaAdi,
                Sehir = satici.Sehir,
                Ilce = satici.Ilce,
                Aciklama = satici.Aciklama,
                DogrulanmisSatici = IsSellerVerified(satici),
                UrunSayisi = aktifUrunler.Count,
                OrtalamaPuan = ortalamaPuan,
                ToplamYorum = toplamYorum,
                ToplamFavori = toplamFavori,
                GuvenSkoru = guvenSkoru,
                TakipciSayisi = takipciSayisi,
                VitrinUrunId = vitrinUrun?.Id,
                KapakResimUrl = !string.IsNullOrWhiteSpace(satici.KapakResimUrl)
                    ? satici.KapakResimUrl
                    : vitrinUrunler
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
            return SaticiProfili.IsVerified(satici);
        }
    }
}
