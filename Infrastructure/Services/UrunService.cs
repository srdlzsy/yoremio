using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    public class UrunService : IUrunService
    {
        private readonly IUrunRepository _urunRepository;
        private readonly IUrunFavoriRepository _urunFavoriRepository;
        private readonly ISaticiProfiliRepository _saticiRepository;
        private readonly IKategoriRepository _kategoriRepository;
        private readonly IDosyaKaydetService _dosyaKaydetService;
        private readonly IAliciProfiliRepository? _aliciProfiliRepository;

        public UrunService(
            IUrunRepository urunRepository,
            IUrunFavoriRepository urunFavoriRepository,
            ISaticiProfiliRepository saticiRepository,
            IKategoriRepository kategoriRepository,
            IDosyaKaydetService dosyaKaydetService,
            IAliciProfiliRepository? aliciProfiliRepository = null)
        {
            _urunRepository = urunRepository;
            _urunFavoriRepository = urunFavoriRepository;
            _saticiRepository = saticiRepository;
            _kategoriRepository = kategoriRepository;
            _dosyaKaydetService = dosyaKaydetService;
            _aliciProfiliRepository = aliciProfiliRepository;
        }

        public async Task<Urun> UrunEkleAsync(string saticiId, UrunEkleDto dto)
        {
            var satici = await _saticiRepository.GetByIdAsync(saticiId);
            if (satici == null)
                throw new KeyNotFoundException("Satıcı bulunamadı.");

            var saticiAdi = Slugify(satici.MagazaAdi ?? "bilinmeyen-satici");
            var urun = await UrunKaydetAsync(saticiId, dto);
            var uploadedUrls = new List<string>();

            try
            {
                var resimUrlListesi = await DosyalariKaydetAsync(dto.Resimler, saticiAdi, urun.Id, "resimler", uploadedUrls);
                var videoUrlListesi = await DosyalariKaydetAsync(dto.Videolar, saticiAdi, urun.Id, "videolar", uploadedUrls);

                return await _urunRepository.EkleUrunAsync(urun, resimUrlListesi, videoUrlListesi);
            }
            catch
            {
                foreach (var url in uploadedUrls)
                {
                    try
                    {
                        await _dosyaKaydetService.SilDosyaAsync(url);
                    }
                    catch
                    {
                        // Cleanup best-effort; asıl hata korunur.
                    }
                }

                _urunRepository.Remove(urun);
                await _urunRepository.SaveChangesAsync();
                throw;
            }
        }

        public async Task<PagedResult<UrunDto>> GetAllUrunlerAsync(UrunQuery query)
        {
            var urunler = await _urunRepository.QueryAsync(query);
            return new PagedResult<UrunDto>
            {
                Items = urunler.Items.Select(MapToDto).ToArray(),
                Page = urunler.Page,
                PageSize = urunler.PageSize,
                TotalCount = urunler.TotalCount
            };
        }

        public async Task<IReadOnlyCollection<AppLocationDto>> GetActiveLocationsAsync()
        {
            var rawLocations = await _urunRepository.GetActiveSellerLocationsAsync();

            return rawLocations
                .GroupBy(x => x.Sehir, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => new AppLocationDto
                {
                    Sehir = g.First().Sehir,
                    Ilceler = g
                        .Select(x => x.Ilce)
                        .Where(ilce => !string.IsNullOrWhiteSpace(ilce))
                        .Select(ilce => ilce!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(ilce => ilce, StringComparer.CurrentCultureIgnoreCase)
                        .ToArray()
                })
                .ToArray();
        }

        public async Task<IEnumerable<UrunDto>> GetOnerilenUrunlerAsync(string kullaniciId, int take)
        {
            var effectiveTake = take <= 0 ? 12 : Math.Min(take, 50);
            var favoriUrunIdleri = await _urunFavoriRepository.GetFavoriUrunIdleriAsync(kullaniciId);
            var kategoriIdleri = (await _urunFavoriRepository.GetKullaniciFavoriKategoriIdleriAsync(kullaniciId, 3)).ToList();

            string? sehir = null;
            string? ilce = null;
            if (_aliciProfiliRepository != null)
            {
                var aliciProfili = await _aliciProfiliRepository.GetWithUserByIdAsync(kullaniciId);
                if (aliciProfili != null)
                {
                    sehir = aliciProfili.Sehir;
                    ilce = aliciProfili.Ilce;

                    if (kategoriIdleri.Count == 0 && !string.IsNullOrWhiteSpace(aliciProfili.FavoriKategori))
                    {
                        var favoriKategoriAdi = aliciProfili.FavoriKategori.Trim();
                        var kategoriler = await _kategoriRepository.GetAllAsync();
                        var matched = kategoriler.FirstOrDefault(k =>
                            string.Equals(k.Adi, favoriKategoriAdi, StringComparison.OrdinalIgnoreCase));
                        if (matched != null)
                        {
                            kategoriIdleri.Add(matched.Id);
                        }
                    }
                }
            }

            var urunler = await _urunRepository.GetRecommendedAsync(kategoriIdleri, favoriUrunIdleri, effectiveTake, sehir, ilce);

            return urunler.Select(MapToDto);
        }

        public async Task<IEnumerable<UrunDto>> GetUrunlerBySaticiIdAsync(string saticiId)
        {
            var urunler = await _urunRepository.FindAsync(u => u.SaticiId == saticiId);
            return urunler.Select(MapToDto);
        }

        public async Task<IEnumerable<UrunDto>> GetFavoriUrunlerAsync(string kullaniciId)
        {
            var urunler = await _urunFavoriRepository.GetFavoriUrunlerAsync(kullaniciId);
            return urunler.Select(MapToDto);
        }

        public async Task<bool> FavoriyeEkleAsync(string kullaniciId, int urunId)
        {
            var urun = await _urunRepository.GetByIdWithRelationsAsync(urunId);
            if (urun == null || !urun.AktifMi)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            var mevcutFavori = await _urunFavoriRepository.GetByKullaniciVeUrunAsync(kullaniciId, urunId);
            if (mevcutFavori != null)
                return false;

            await _urunFavoriRepository.AddAsync(new UrunFavori
            {
                KullaniciId = kullaniciId,
                UrunId = urunId
            });

            return await _urunFavoriRepository.SaveChangesAsync();
        }

        public async Task<bool> FavoridenCikarAsync(string kullaniciId, int urunId)
        {
            var mevcutFavori = await _urunFavoriRepository.GetByKullaniciVeUrunAsync(kullaniciId, urunId);
            if (mevcutFavori == null)
                return false;

            _urunFavoriRepository.Remove(mevcutFavori);
            return await _urunFavoriRepository.SaveChangesAsync();
        }

        public async Task<UrunDto?> GetUrunByIdAsync(int urunId)
        {
            var urun = await _urunRepository.GetByIdWithRelationsAsync(urunId);
            return urun == null ? null : MapToDto(urun);
        }

        public async Task<UrunDto> UrunGuncelleAsync(int urunId, string saticiId, UrunGuncelleDto dto)
        {
            var satici = await _saticiRepository.GetByIdAsync(saticiId);
            if (satici == null)
                throw new KeyNotFoundException("Satıcı bulunamadı.");

            var saticiAdi = Slugify(satici.MagazaAdi ?? "bilinmeyen-satici");
            var urun = await _urunRepository.GetByIdForUpdateAsync(urunId);
            if (urun == null)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (!string.Equals(urun.SaticiId, saticiId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu ürün üzerinde işlem yetkiniz yok.");

            if (dto.KategoriId.HasValue)
            {
                var kategori = await _kategoriRepository.GetByIdAsync(dto.KategoriId.Value);
                if (kategori == null)
                    throw new KeyNotFoundException($"Kategori bulunamadı. KategoriId: {dto.KategoriId.Value}");

                urun.KategoriId = dto.KategoriId.Value;
                urun.Kategori = kategori;
            }

            if (!string.IsNullOrWhiteSpace(dto.Adi))
                urun.Adi = dto.Adi.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Aciklama))
                urun.Aciklama = dto.Aciklama.Trim();

            if (dto.Fiyat.HasValue)
                urun.Fiyat = dto.Fiyat.Value;

            if (dto.StokMiktari.HasValue)
                urun.StokMiktari = dto.StokMiktari.Value;

            if (dto.Resimler is { Count: > 0 })
            {
                var resimUrlListesi = await DosyalariKaydetAsync(dto.Resimler, saticiAdi, urun.Id, "resimler");
                foreach (var url in resimUrlListesi)
                    urun.Resimler.Add(new UrunResim { Url = url });
            }

            if (dto.Videolar is { Count: > 0 })
            {
                var videoUrlListesi = await DosyalariKaydetAsync(dto.Videolar, saticiAdi, urun.Id, "videolar");
                foreach (var url in videoUrlListesi)
                    urun.Videolar.Add(new UrunVideo { Url = url });
            }

            urun.GuncellemeTarihi = DateTime.UtcNow;
            await _urunRepository.SaveChangesAsync();
            return await GetUrunByIdAsync(urunId) ?? MapToDto(urun);
        }

        public async Task<bool> UrunSilAsync(int urunId, string saticiId)
        {
            var urun = await _urunRepository.GetByIdForUpdateAsync(urunId);
            if (urun == null)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (!string.Equals(urun.SaticiId, saticiId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu ürün üzerinde işlem yetkiniz yok.");

            var medyaUrlleri = urun.Resimler.Select(r => r.Url)
                .Concat(urun.Videolar.Select(v => v.Url))
                .ToList();

            _urunRepository.Remove(urun);
            var deleted = await _urunRepository.SaveChangesAsync();
            if (deleted)
            {
                foreach (var url in medyaUrlleri)
                {
                    await _dosyaKaydetService.SilDosyaAsync(url);
                }
            }

            return deleted;
        }

        public async Task<UrunDto> UrunDurumuGuncelleAsync(int urunId, string saticiId, bool aktifMi)
        {
            var urun = await _urunRepository.GetByIdForUpdateAsync(urunId);
            if (urun == null)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (!string.Equals(urun.SaticiId, saticiId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu ürün üzerinde işlem yetkiniz yok.");

            urun.AktifMi = aktifMi;
            urun.GuncellemeTarihi = DateTime.UtcNow;

            await _urunRepository.SaveChangesAsync();
            return MapToDto(urun);
        }

        public async Task<bool> UrunResimSilAsync(int urunId, int resimId, string saticiId)
        {
            var urun = await _urunRepository.GetByIdForUpdateAsync(urunId);
            if (urun == null)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (!string.Equals(urun.SaticiId, saticiId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu ürün üzerinde işlem yetkiniz yok.");

            var resim = urun.Resimler.FirstOrDefault(r => r.Id == resimId);
            if (resim == null)
                throw new KeyNotFoundException("Resim bulunamadı.");

            await _dosyaKaydetService.SilDosyaAsync(resim.Url);
            urun.Resimler.Remove(resim);
            return await _urunRepository.SaveChangesAsync();
        }

        public async Task<bool> UrunVideoSilAsync(int urunId, int videoId, string saticiId)
        {
            var urun = await _urunRepository.GetByIdForUpdateAsync(urunId);
            if (urun == null)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (!string.Equals(urun.SaticiId, saticiId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu ürün üzerinde işlem yetkiniz yok.");

            var video = urun.Videolar.FirstOrDefault(v => v.Id == videoId);
            if (video == null)
                throw new KeyNotFoundException("Video bulunamadı.");

            await _dosyaKaydetService.SilDosyaAsync(video.Url);
            urun.Videolar.Remove(video);
            return await _urunRepository.SaveChangesAsync();
        }

        public async Task<Urun?> EkleMedyaToUrunAsync(int urunId, List<string> resimUrlListesi, List<string> videoUrlListesi)
        {
            var urun = await _urunRepository.GetByIdForUpdateAsync(urunId);
            if (urun == null)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            urun.Resimler ??= new List<UrunResim>();
            urun.Videolar ??= new List<UrunVideo>();

            foreach (var url in resimUrlListesi)
            {
                urun.Resimler.Add(new UrunResim { Url = url, UrunId = urunId });
            }

            foreach (var url in videoUrlListesi)
            {
                urun.Videolar.Add(new UrunVideo { Url = url, UrunId = urunId });
            }

            await _urunRepository.SaveChangesAsync();
            return urun;
        }

        private static UrunDto MapToDto(Urun urun)
        {
            return new UrunDto
            {
                Id = urun.Id,
                Adi = urun.Adi,
                Aciklama = urun.Aciklama,
                Fiyat = urun.Fiyat,
                StokMiktari = urun.StokMiktari,
                AktifMi = urun.AktifMi,
                KategoriId = urun.KategoriId,
                KategoriAdi = urun.Kategori?.Adi,
                SaticiId = urun.SaticiId,
                SaticiMagazaAdi = urun.Satici?.MagazaAdi,
                SaticiSehir = urun.Satici?.Sehir,
                SaticiIlce = urun.Satici?.Ilce,
                SaticiDogrulanmis = SaticiProfili.IsVerified(urun.Satici),
                OrtalamaPuan = urun.Puanlar.Count == 0 ? 0 : Math.Round(urun.Puanlar.Average(p => p.PuanDegeri), 1),
                ToplamPuan = urun.Puanlar.Count,
                ToplamYorum = urun.Yorumlar.Count(y => y.AktifMi),
                ToplamFavori = urun.Favoriler.Count,
                Puanlar = urun.Puanlar.Select(p => new PuanDto
                {
                    Id = p.Id,
                    UrunId = p.UrunId,
                    KullaniciId = p.KullaniciId,
                    KullaniciAdi = p.Kullanici?.UserName,
                    PuanDegeri = p.PuanDegeri,
                    PuanTarihi = p.PuanTarihi
                }).ToList(),
                Yorumlar = urun.Yorumlar
                    .Where(y => y.AktifMi)
                    .OrderByDescending(y => y.Tarih)
                    .Select(y => new YorumDto
                    {
                        Id = y.Id,
                        UrunId = y.UrunId,
                        Icerik = y.Icerik,
                        Tarih = y.Tarih,
                        KullaniciId = y.YorumYapanKullaniciId,
                        KullaniciAdi = y.YorumYapanKullanici?.UserName
                    }).ToList(),
                Resimler = urun.Resimler?.Select(r => new UrunResimDto { Id = r.Id, Url = r.Url }).ToList(),
                Videolar = urun.Videolar?.Select(v => new UrunVideoDto { Id = v.Id, Url = v.Url }).ToList()
            };
        }

        private async Task<Urun> UrunKaydetAsync(string saticiId, UrunEkleDto dto)
        {
            var kategori = await _kategoriRepository.GetByIdAsync(dto.KategoriId);
            if (kategori == null)
                throw new KeyNotFoundException($"Kategori bulunamadı. KategoriId: {dto.KategoriId}");

            var urun = new Urun
            {
                Adi = dto.Adi,
                Aciklama = dto.Aciklama ?? string.Empty,
                Fiyat = dto.Fiyat,
                StokMiktari = dto.StokMiktari,
                KategoriId = dto.KategoriId,
                SaticiId = saticiId
            };

            await _urunRepository.AddAsync(urun);
            await _urunRepository.SaveChangesAsync();

            return urun;
        }

        private async Task<List<string>> DosyalariKaydetAsync(
            IEnumerable<IFormFile> dosyalar,
            string saticiAdi,
            int urunId,
            string dosyaTuru,
            List<string>? trackedUrls = null)
        {
            var urlListesi = new List<string>();
            if (dosyalar == null || !dosyalar.Any())
                return urlListesi;

            foreach (var dosya in dosyalar)
            {
                var klasorYolu = Path.Combine(saticiAdi, urunId.ToString(), dosyaTuru);
                var url = await _dosyaKaydetService.KaydetDosyaAsync(dosya, klasorYolu);
                urlListesi.Add(url);
                trackedUrls?.Add(url);
            }

            return urlListesi;
        }

        private static string Slugify(string text)
        {
            text = text.ToLowerInvariant();
            text = text.Replace("ı", "i").Replace("ö", "o").Replace("ü", "u").Replace("ş", "s").Replace("ğ", "g").Replace("ç", "c");
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            text = Regex.Replace(text, @"-+", "-").Trim('-');
            return text;
        }
    }
}
