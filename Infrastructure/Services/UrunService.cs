using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
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
        private readonly ISaticiTakipRepository? _saticiTakipRepository;
        private readonly IFiyatAlarmiRepository? _fiyatAlarmiRepository;
        private readonly IStokAlarmiRepository? _stokAlarmiRepository;
        private readonly IOrtakAlimRepository? _ortakAlimRepository;
        private readonly IPlanliGonderimRepository? _planliGonderimRepository;
        private readonly IBildirimService? _bildirimService;

        public UrunService(
            IUrunRepository urunRepository,
            IUrunFavoriRepository urunFavoriRepository,
            ISaticiProfiliRepository saticiRepository,
            IKategoriRepository kategoriRepository,
            IDosyaKaydetService dosyaKaydetService,
            IAliciProfiliRepository? aliciProfiliRepository = null,
            ISaticiTakipRepository? saticiTakipRepository = null,
            IFiyatAlarmiRepository? fiyatAlarmiRepository = null,
            IBildirimService? bildirimService = null,
            IStokAlarmiRepository? stokAlarmiRepository = null,
            IOrtakAlimRepository? ortakAlimRepository = null,
            IPlanliGonderimRepository? planliGonderimRepository = null)
        {
            _urunRepository = urunRepository;
            _urunFavoriRepository = urunFavoriRepository;
            _saticiRepository = saticiRepository;
            _kategoriRepository = kategoriRepository;
            _dosyaKaydetService = dosyaKaydetService;
            _aliciProfiliRepository = aliciProfiliRepository;
            _saticiTakipRepository = saticiTakipRepository;
            _fiyatAlarmiRepository = fiyatAlarmiRepository;
            _bildirimService = bildirimService;
            _stokAlarmiRepository = stokAlarmiRepository;
            _ortakAlimRepository = ortakAlimRepository;
            _planliGonderimRepository = planliGonderimRepository;
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

                var created = await _urunRepository.EkleUrunAsync(urun, resimUrlListesi, videoUrlListesi);
                await TakipcilereYeniUrunBildirAsync(satici, created);
                return created;
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

        public async Task<UrunKesfetSonucDto> KesfetAsync(UrunQuery query)
        {
            var sonuclar = await GetAllUrunlerAsync(query);
            if (sonuclar.TotalCount > 0)
            {
                return new UrunKesfetSonucDto
                {
                    Sonuclar = sonuclar,
                    Alternatifler = null
                };
            }

            var hedefSehir = query.TeslimatSehir ?? query.Sehir;
            var ayniYoreden = Array.Empty<UrunDto>();
            var sehreGonderim = Array.Empty<UrunDto>();
            var stoguTukenen = Array.Empty<UrunDto>();

            if (!string.IsNullOrWhiteSpace(query.UretimSehir) || query.KategoriId.HasValue)
            {
                var yoreRes = await _urunRepository.QueryAsync(new UrunQuery
                {
                    KategoriId = query.KategoriId,
                    UretimSehir = query.UretimSehir,
                    SadeceAktif = true,
                    SadeceStoktaOlanlar = true,
                    Page = 1,
                    PageSize = 4,
                    Sort = "top_rated"
                });
                ayniYoreden = yoreRes.Items.Select(MapToDto).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(hedefSehir))
            {
                var deliveryRes = await _urunRepository.QueryAsync(new UrunQuery
                {
                    TeslimatSehir = hedefSehir,
                    KategoriId = query.KategoriId,
                    SadeceAktif = true,
                    SadeceStoktaOlanlar = true,
                    Page = 1,
                    PageSize = 4,
                    Sort = "relevance"
                });
                sehreGonderim = deliveryRes.Items.Select(MapToDto).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(query.Q) || query.KategoriId.HasValue)
            {
                var outOfStockRes = await _urunRepository.QueryAsync(new UrunQuery
                {
                    Q = query.Q,
                    KategoriId = query.KategoriId,
                    SadeceAktif = true,
                    SadeceStoktaOlanlar = false,
                    Page = 1,
                    PageSize = 4,
                    Sort = "top_rated"
                });
                stoguTukenen = outOfStockRes.Items
                    .Where(u => u.StokMiktari <= 0)
                    .Select(MapToDto)
                    .ToArray();
            }

            var ortakAlimlar = _ortakAlimRepository == null
                ? Array.Empty<OrtakAlimDto>()
                : (await _ortakAlimRepository.GetMatchingOpenGroupBuysAsync(query.Q, hedefSehir, 4))
                    .Select(OrtakAlimService.MapToDto)
                    .ToArray();

            var planliGonderimler = _planliGonderimRepository == null
                ? Array.Empty<PlanliGonderimDto>()
                : (await _planliGonderimRepository.GetUpcomingAsync(hedefSehir, query.UretimSehir, take: 4))
                    .Select(PlanliGonderimService.MapToDto)
                    .ToArray();

            return new UrunKesfetSonucDto
            {
                Sonuclar = sonuclar,
                Alternatifler = new AkilliAlternatiflerDto
                {
                    AyniYoredenUrunler = ayniYoreden,
                    SehreGonderimYapanlar = sehreGonderim,
                    StoguTukenenUrunler = stoguTukenen,
                    AktifOrtakAlimlar = ortakAlimlar,
                    PlanliGonderimler = planliGonderimler,
                    BenimIcinBulOnerisi = new BenimIcinBulOneriDto
                    {
                        UrunBulunamadi = true,
                        ArananUrunAdi = query.Q,
                        TeslimatSehir = hedefSehir,
                        KategoriId = query.KategoriId,
                        AcikTalepEndpoint = "/api/Talep/pazar"
                    }
                }
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

        public async Task<UrunDto?> GetUrunByIdAsync(int urunId, bool incrementView = false)
        {
            var urun = await _urunRepository.GetByIdWithRelationsAsync(urunId);
            if (urun == null)
                return null;

            var dto = MapToDto(urun);
            if (incrementView && urun.AktifMi)
            {
                await _urunRepository.IncrementViewCountAsync(urunId);
                dto.GoruntulenmeSayisi += 1;
            }

            return dto;
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

            var eskiFiyat = urun.Fiyat;
            var eskiStok = urun.StokMiktari;

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

            if (dto.BirimMiktar is not null)
                urun.BirimMiktar = string.IsNullOrWhiteSpace(dto.BirimMiktar) ? null : dto.BirimMiktar.Trim();

            if (dto.TeslimatSecenekleri is not null)
                urun.TeslimatSecenekleri = string.IsNullOrWhiteSpace(dto.TeslimatSecenekleri) ? null : dto.TeslimatSecenekleri.Trim();

            if (dto.YeniHasatMi.HasValue)
                urun.YeniHasatMi = dto.YeniHasatMi.Value;

            if (dto.UretimSehir is not null)
                urun.UretimSehir = string.IsNullOrWhiteSpace(dto.UretimSehir) ? null : dto.UretimSehir.Trim();

            if (dto.UretimIlce is not null)
                urun.UretimIlce = string.IsNullOrWhiteSpace(dto.UretimIlce) ? null : dto.UretimIlce.Trim();

            if (dto.EldenTeslimVarMi.HasValue)
                urun.EldenTeslimVarMi = dto.EldenTeslimVarMi.Value;

            if (dto.KargoVarMi.HasValue)
                urun.KargoVarMi = dto.KargoVarMi.Value;

            if (dto.GonderimSehirleri is not null)
                urun.GonderimSehirleri = string.IsNullOrWhiteSpace(dto.GonderimSehirleri) ? null : dto.GonderimSehirleri.Trim();

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

            if (dto.Fiyat.HasValue && dto.Fiyat.Value < eskiFiyat)
            {
                await FiyatAlarmlariniTetikleAsync(urun.Id, urun.Adi, urun.Fiyat);
            }

            if (eskiStok <= 0 && urun.StokMiktari > 0 && urun.AktifMi)
            {
                await StokAlarmlariniTetikleAsync(urun.Id, urun.Adi, urun.StokMiktari);
            }

            return await GetUrunByIdAsync(urunId) ?? MapToDto(urun);
        }

        public async Task<IReadOnlyCollection<UrunDto>> TopluStokGuncelleAsync(string saticiId, TopluStokGuncelleDto dto)
        {
            var results = new List<UrunDto>();
            var fiyatDusulenler = new List<(int UrunId, string UrunAdi, decimal YeniFiyat)>();
            var stoguGelenler = new List<(int UrunId, string UrunAdi, int YeniStok)>();

            foreach (var kalem in dto.Kalemler)
            {
                var urun = await _urunRepository.GetByIdForUpdateAsync(kalem.UrunId);
                if (urun == null)
                    throw new KeyNotFoundException($"Ürün bulunamadı. UrunId: {kalem.UrunId}");

                if (!string.Equals(urun.SaticiId, saticiId, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException($"UrunId {kalem.UrunId} üzerinde işlem yetkiniz yok.");

                var eskiFiyat = urun.Fiyat;
                var eskiStok = urun.StokMiktari;
                urun.StokMiktari = kalem.StokMiktari;

                if (eskiStok <= 0 && urun.StokMiktari > 0 && urun.AktifMi)
                {
                    stoguGelenler.Add((urun.Id, urun.Adi, urun.StokMiktari));
                }

                if (kalem.Fiyat.HasValue)
                {
                    urun.Fiyat = kalem.Fiyat.Value;
                    if (kalem.Fiyat.Value < eskiFiyat)
                    {
                        fiyatDusulenler.Add((urun.Id, urun.Adi, urun.Fiyat));
                    }
                }

                if (kalem.YeniHasatMi.HasValue)
                {
                    urun.YeniHasatMi = kalem.YeniHasatMi.Value;
                }

                urun.GuncellemeTarihi = DateTime.UtcNow;
                results.Add(MapToDto(urun));
            }

            await _urunRepository.SaveChangesAsync();

            foreach (var item in fiyatDusulenler)
            {
                await FiyatAlarmlariniTetikleAsync(item.UrunId, item.UrunAdi, item.YeniFiyat);
            }

            foreach (var item in stoguGelenler)
            {
                await StokAlarmlariniTetikleAsync(item.UrunId, item.UrunAdi, item.YeniStok);
            }

            return results;
        }

        public async Task<FiyatAlarmiDto> FiyatAlarmiKaydetAsync(string aliciId, int urunId, FiyatAlarmiOlusturDto dto)
        {
            if (_fiyatAlarmiRepository == null)
                throw new InvalidOperationException("Fiyat alarm servisi yapilandirilmamis.");

            var urun = await _urunRepository.GetByIdWithRelationsAsync(urunId);
            if (urun == null || !urun.AktifMi)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (string.Equals(urun.SaticiId, aliciId, StringComparison.Ordinal))
                throw new ArgumentException("Kendi ürününüz için fiyat alarmı oluşturamazsınız.");

            var mevcut = await _fiyatAlarmiRepository.GetAsync(aliciId, urunId);
            if (mevcut != null)
            {
                mevcut.HedefFiyat = dto.HedefFiyat;
                mevcut.AktifMi = true;
                mevcut.SonTetiklenenFiyat = null;
                mevcut.SonTetiklemeTarihi = null;
                _fiyatAlarmiRepository.Update(mevcut);
                await _fiyatAlarmiRepository.SaveChangesAsync();
                return MapFiyatAlarmiDto(mevcut, urun);
            }

            var alarm = new FiyatAlarmi
            {
                AliciId = aliciId,
                UrunId = urunId,
                HedefFiyat = dto.HedefFiyat,
                AktifMi = true
            };

            await _fiyatAlarmiRepository.AddAsync(alarm);
            await _fiyatAlarmiRepository.SaveChangesAsync();
            return MapFiyatAlarmiDto(alarm, urun);
        }

        public async Task<bool> FiyatAlarmiSilAsync(string aliciId, int urunId)
        {
            if (_fiyatAlarmiRepository == null)
                throw new InvalidOperationException("Fiyat alarm servisi yapilandirilmamis.");

            var mevcut = await _fiyatAlarmiRepository.GetAsync(aliciId, urunId);
            if (mevcut == null)
                return false;

            _fiyatAlarmiRepository.Remove(mevcut);
            return await _fiyatAlarmiRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<FiyatAlarmiDto>> GetFiyatAlarmlarimAsync(string aliciId)
        {
            if (_fiyatAlarmiRepository == null)
                return Array.Empty<FiyatAlarmiDto>();

            var alarmlar = await _fiyatAlarmiRepository.GetByAliciWithProductAsync(aliciId);
            return alarmlar.Select(a => MapFiyatAlarmiDto(a, a.Urun)).ToList();
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
                BirimMiktar = urun.BirimMiktar,
                TeslimatSecenekleri = urun.TeslimatSecenekleri,
                YeniHasatMi = urun.YeniHasatMi,
                UretimSehir = urun.UretimSehir ?? urun.Satici?.Sehir,
                UretimIlce = urun.UretimIlce ?? urun.Satici?.Ilce,
                EldenTeslimVarMi = urun.EldenTeslimVarMi,
                KargoVarMi = urun.KargoVarMi,
                GonderimSehirleri = urun.GonderimSehirleri,
                GoruntulenmeSayisi = urun.GoruntulenmeSayisi,
                Slug = $"{Slugify(urun.Adi)}-{urun.Id}",
                OlusturmaTarihi = urun.OlusturmaTarihi,
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
                Adi = dto.Adi.Trim(),
                Aciklama = dto.Aciklama?.Trim() ?? string.Empty,
                Fiyat = dto.Fiyat,
                StokMiktari = dto.StokMiktari,
                BirimMiktar = string.IsNullOrWhiteSpace(dto.BirimMiktar) ? null : dto.BirimMiktar.Trim(),
                TeslimatSecenekleri = string.IsNullOrWhiteSpace(dto.TeslimatSecenekleri) ? null : dto.TeslimatSecenekleri.Trim(),
                YeniHasatMi = dto.YeniHasatMi,
                UretimSehir = string.IsNullOrWhiteSpace(dto.UretimSehir) ? null : dto.UretimSehir.Trim(),
                UretimIlce = string.IsNullOrWhiteSpace(dto.UretimIlce) ? null : dto.UretimIlce.Trim(),
                EldenTeslimVarMi = dto.EldenTeslimVarMi,
                KargoVarMi = dto.KargoVarMi,
                GonderimSehirleri = string.IsNullOrWhiteSpace(dto.GonderimSehirleri) ? null : dto.GonderimSehirleri.Trim(),
                KategoriId = dto.KategoriId,
                SaticiId = saticiId
            };

            await _urunRepository.AddAsync(urun);
            await _urunRepository.SaveChangesAsync();

            return urun;
        }

        public async Task<StokAlarmiDto> StokAlarmiKaydetAsync(string aliciId, int urunId)
        {
            if (_stokAlarmiRepository == null)
                throw new InvalidOperationException("Stok alarm servisi yapilandirilmamis.");

            var urun = await _urunRepository.GetByIdWithRelationsAsync(urunId);
            if (urun == null || !urun.AktifMi)
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (string.Equals(urun.SaticiId, aliciId, StringComparison.Ordinal))
                throw new ArgumentException("Kendi ürününüz için stok alarmı oluşturamazsınız.");

            var mevcut = await _stokAlarmiRepository.GetAsync(aliciId, urunId);
            if (mevcut != null)
            {
                mevcut.AktifMi = true;
                mevcut.SonTetiklemeTarihi = null;
                _stokAlarmiRepository.Update(mevcut);
                await _stokAlarmiRepository.SaveChangesAsync();
                return MapStokAlarmiDto(mevcut, urun);
            }

            var alarm = new StokAlarmi
            {
                AliciId = aliciId,
                UrunId = urunId,
                AktifMi = true
            };

            await _stokAlarmiRepository.AddAsync(alarm);
            await _stokAlarmiRepository.SaveChangesAsync();
            return MapStokAlarmiDto(alarm, urun);
        }

        public async Task<bool> StokAlarmiSilAsync(string aliciId, int urunId)
        {
            if (_stokAlarmiRepository == null)
                throw new InvalidOperationException("Stok alarm servisi yapilandirilmamis.");

            var mevcut = await _stokAlarmiRepository.GetAsync(aliciId, urunId);
            if (mevcut == null)
                return false;

            _stokAlarmiRepository.Remove(mevcut);
            return await _stokAlarmiRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<StokAlarmiDto>> GetStokAlarmlarimAsync(string aliciId)
        {
            if (_stokAlarmiRepository == null)
                return Array.Empty<StokAlarmiDto>();

            var alarmlar = await _stokAlarmiRepository.GetByAliciWithProductAsync(aliciId);
            return alarmlar.Select(a => MapStokAlarmiDto(a, a.Urun)).ToList();
        }

        private async Task StokAlarmlariniTetikleAsync(int urunId, string urunAdi, int yeniStok)
        {
            if (_stokAlarmiRepository == null || _bildirimService == null)
                return;

            var aktifAlarmlar = await _stokAlarmiRepository.GetActiveByProductAsync(urunId);
            if (aktifAlarmlar.Count == 0)
                return;

            foreach (var alarm in aktifAlarmlar)
            {
                alarm.AktifMi = false;
                alarm.SonTetiklemeTarihi = DateTime.UtcNow;
                _stokAlarmiRepository.Update(alarm);

                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = alarm.AliciId,
                    Tur = BildirimTurleri.StokAlarmi,
                    Baslik = "Stok yeniden geldi",
                    Mesaj = $"{urunAdi} yeniden stokta ({yeniStok} adet). Hemen teklif isteyebilirsiniz.",
                    IlgiliVarlikTuru = "Urun",
                    IlgiliVarlikId = urunId.ToString(),
                    AksiyonUrl = $"/urunler/{urunId}"
                });
            }

            await _stokAlarmiRepository.SaveChangesAsync();
        }

        private static StokAlarmiDto MapStokAlarmiDto(StokAlarmi alarm, Urun? urun)
        {
            return new StokAlarmiDto
            {
                Id = alarm.Id,
                UrunId = alarm.UrunId,
                UrunAdi = urun?.Adi ?? string.Empty,
                UrunResimUrl = urun?.Resimler?.Select(r => r.Url).FirstOrDefault(),
                UrunFiyat = urun?.Fiyat ?? 0,
                StokMiktari = urun?.StokMiktari ?? 0,
                AktifMi = alarm.AktifMi,
                SonTetiklemeTarihi = alarm.SonTetiklemeTarihi,
                OlusturmaTarihi = alarm.OlusturmaTarihi
            };
        }

        private async Task TakipcilereYeniUrunBildirAsync(SaticiProfili satici, Urun urun)
        {
            if (_saticiTakipRepository == null || _bildirimService == null || !urun.AktifMi)
                return;

            var takipciler = await _saticiTakipRepository.GetFollowersWithNotificationAsync(satici.KullaniciId);
            if (takipciler.Count == 0)
                return;

            var bildirimTuru = urun.YeniHasatMi ? BildirimTurleri.Hasat : BildirimTurleri.Urun;
            var baslik = urun.YeniHasatMi ? "Yeni hasat ürünü yayında" : "Takip ettiğiniz üreticiden yeni ürün";
            var mesaj = $"{satici.MagazaAdi}, {urun.Adi} ürününü yayınladı.";

            foreach (var takipci in takipciler)
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = takipci.AliciId,
                    Tur = bildirimTuru,
                    Baslik = baslik,
                    Mesaj = mesaj,
                    IlgiliVarlikTuru = "Urun",
                    IlgiliVarlikId = urun.Id.ToString(),
                    AksiyonUrl = $"/urunler/{urun.Id}"
                });
            }
        }

        private async Task FiyatAlarmlariniTetikleAsync(int urunId, string urunAdi, decimal yeniFiyat)
        {
            if (_fiyatAlarmiRepository == null || _bildirimService == null)
                return;

            var eslesenAlarmlar = await _fiyatAlarmiRepository.GetMatchingActiveAlertsAsync(urunId, yeniFiyat);
            if (eslesenAlarmlar.Count == 0)
                return;

            foreach (var alarm in eslesenAlarmlar)
            {
                alarm.SonTetiklenenFiyat = yeniFiyat;
                alarm.SonTetiklemeTarihi = DateTime.UtcNow;
                _fiyatAlarmiRepository.Update(alarm);

                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = alarm.AliciId,
                    Tur = BildirimTurleri.FiyatAlarmi,
                    Baslik = "Fiyat alarmı tetiklendi",
                    Mesaj = $"{urunAdi} fiyatı {yeniFiyat:0.##} TL seviyesine düştü (hedef: {alarm.HedefFiyat:0.##} TL).",
                    IlgiliVarlikTuru = "Urun",
                    IlgiliVarlikId = urunId.ToString(),
                    AksiyonUrl = $"/urunler/{urunId}"
                });
            }

            await _fiyatAlarmiRepository.SaveChangesAsync();
        }

        private static FiyatAlarmiDto MapFiyatAlarmiDto(FiyatAlarmi alarm, Urun? urun)
        {
            return new FiyatAlarmiDto
            {
                Id = alarm.Id,
                UrunId = alarm.UrunId,
                UrunAdi = urun?.Adi ?? string.Empty,
                UrunResimUrl = urun?.Resimler?.Select(r => r.Url).FirstOrDefault(),
                UrunFiyat = urun?.Fiyat ?? 0,
                HedefFiyat = alarm.HedefFiyat,
                AktifMi = alarm.AktifMi,
                TetiklendiMi = alarm.SonTetiklemeTarihi.HasValue,
                SonTetiklenenFiyat = alarm.SonTetiklenenFiyat,
                SonTetiklemeTarihi = alarm.SonTetiklemeTarihi,
                OlusturmaTarihi = alarm.OlusturmaTarihi
            };
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
