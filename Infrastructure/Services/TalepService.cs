using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;

namespace Infrastructure.Services
{
    public class TalepService : ITalepService
    {
        private readonly ITalepRepository _talepRepository;
        private readonly IUrunRepository _urunRepository;
        private readonly IBildirimService _bildirimService;
        private readonly ISaticiProfiliRepository? _saticiProfiliRepository;
        private readonly IKategoriRepository? _kategoriRepository;
        private readonly IFiyatAlarmiRepository? _fiyatAlarmiRepository;

        public TalepService(
            ITalepRepository talepRepository,
            IUrunRepository urunRepository,
            IBildirimService bildirimService,
            ISaticiProfiliRepository? saticiProfiliRepository = null,
            IKategoriRepository? kategoriRepository = null,
            IFiyatAlarmiRepository? fiyatAlarmiRepository = null)
        {
            _talepRepository = talepRepository;
            _urunRepository = urunRepository;
            _bildirimService = bildirimService;
            _saticiProfiliRepository = saticiProfiliRepository;
            _kategoriRepository = kategoriRepository;
            _fiyatAlarmiRepository = fiyatAlarmiRepository;
        }

        public async Task<TalepDto> TalepOlusturAsync(string aliciId, TalepOlusturDto dto)
        {
            var urun = await _urunRepository.GetByIdWithRelationsAsync(dto.UrunId);
            if (urun == null || !urun.AktifMi)
            {
                throw new KeyNotFoundException("Urun bulunamadi.");
            }

            if (string.Equals(urun.SaticiId, aliciId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Kendi urununuz icin talep olusturamazsiniz.");
            }

            var talep = new Talep
            {
                AliciId = aliciId,
                UrunId = dto.UrunId,
                KategoriId = urun.KategoriId,
                TalepTuru = TalepTurleri.Urun,
                TalepTipi = TalepTipleri.Bireysel,
                Birim = urun.BirimMiktar,
                Miktar = dto.Miktar,
                Not = string.IsNullOrWhiteSpace(dto.Not) ? null : dto.Not.Trim(),
                Durum = TalepDurumlari.Acik
            };

            await _talepRepository.AddAsync(talep);
            await _talepRepository.SaveChangesAsync();

            var created = await _talepRepository.GetByIdWithDetailsAsync(talep.Id) ?? talep;
            if (!string.IsNullOrWhiteSpace(urun.SaticiId))
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = urun.SaticiId,
                    Tur = BildirimTurleri.Talep,
                    Baslik = "Yeni talep",
                    Mesaj = $"{urun.Adi} urunu icin yeni talep olusturuldu.",
                    IlgiliVarlikTuru = "Talep",
                    IlgiliVarlikId = talep.Id.ToString(),
                    AksiyonUrl = $"/talepler/{talep.Id}"
                });
            }

            return MapTalepDto(created);
        }

        public async Task<TalepDto> AcikTalepOlusturAsync(string aliciId, AcikTalepOlusturDto dto)
        {
            if (dto.KategoriId.HasValue && _kategoriRepository != null)
            {
                var kategori = await _kategoriRepository.GetByIdAsync(dto.KategoriId.Value);
                if (kategori == null)
                {
                    throw new KeyNotFoundException($"Kategori bulunamadi. KategoriId: {dto.KategoriId.Value}");
                }
            }

            var talepTipi = string.Equals(dto.TalepTipi?.Trim(), TalepTipleri.Kurumsal, StringComparison.OrdinalIgnoreCase)
                ? TalepTipleri.Kurumsal
                : TalepTipleri.Bireysel;

            var talep = new Talep
            {
                AliciId = aliciId,
                UrunId = null,
                KategoriId = dto.KategoriId,
                TalepTuru = TalepTurleri.AcikPazar,
                TalepTipi = talepTipi,
                ArananUrunAdi = dto.ArananUrunAdi.Trim(),
                Miktar = dto.Miktar,
                Birim = string.IsNullOrWhiteSpace(dto.Birim) ? "kg" : dto.Birim.Trim(),
                TeslimatSehir = dto.TeslimatSehir.Trim(),
                TercihEdilenSehirler = string.IsNullOrWhiteSpace(dto.TercihEdilenSehirler) ? null : dto.TercihEdilenSehirler.Trim(),
                HedefBirimFiyat = dto.HedefBirimFiyat,
                SonTeklifTarihi = dto.SonTeklifTarihi,
                Not = string.IsNullOrWhiteSpace(dto.Not) ? null : dto.Not.Trim(),
                Durum = TalepDurumlari.Acik
            };

            await _talepRepository.AddAsync(talep);
            await _talepRepository.SaveChangesAsync();

            await EslesmeleriOlusturVeBildirAsync(talep);

            var created = await _talepRepository.GetByIdWithDetailsAsync(talep.Id) ?? talep;
            return MapTalepDto(created);
        }

        public async Task<TalepDto> TalepTekrarlaAsync(int talepId, string aliciId, TalepTekrarlaDto? dto = null)
        {
            var mevcut = await _talepRepository.GetByIdWithDetailsAsync(talepId);
            if (mevcut == null)
            {
                throw new KeyNotFoundException("Tekrarlanacak talep bulunamadi.");
            }

            if (!string.Equals(mevcut.AliciId, aliciId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Sadece kendi taleplerinizi tekrarlayabilirsiniz.");
            }

            var yeniMiktar = dto?.Miktar ?? mevcut.Miktar;
            var yeniNot = dto?.Not is not null ? dto.Not : mevcut.Not;

            if (mevcut.UrunId.HasValue && mevcut.TalepTuru != TalepTurleri.AcikPazar)
            {
                return await TalepOlusturAsync(aliciId, new TalepOlusturDto
                {
                    UrunId = mevcut.UrunId.Value,
                    Miktar = yeniMiktar,
                    Not = yeniNot
                });
            }

            return await AcikTalepOlusturAsync(aliciId, new AcikTalepOlusturDto
            {
                ArananUrunAdi = mevcut.ArananUrunAdi ?? mevcut.Urun?.Adi ?? "Yoresel Urun",
                KategoriId = mevcut.KategoriId,
                Miktar = yeniMiktar,
                Birim = mevcut.Birim ?? "kg",
                TeslimatSehir = mevcut.TeslimatSehir ?? "Turkiye",
                TercihEdilenSehirler = mevcut.TercihEdilenSehirler,
                HedefBirimFiyat = mevcut.HedefBirimFiyat,
                TalepTipi = mevcut.TalepTipi,
                Not = yeniNot
            });
        }

        public async Task<TalepDto> GetTalepByIdAsync(int talepId, string userId)
        {
            var talep = await _talepRepository.GetByIdWithDetailsAsync(talepId);
            if (talep == null)
            {
                throw new KeyNotFoundException("Talep bulunamadi.");
            }

            var isBuyer = string.Equals(talep.AliciId, userId, StringComparison.Ordinal);
            var isProductSeller = talep.Urun != null && string.Equals(talep.Urun.SaticiId, userId, StringComparison.Ordinal);
            var isMatchedOrOfferedSeller = talep.TalepTuru == TalepTurleri.AcikPazar &&
                (talep.Eslesmeler.Any(e => string.Equals(e.SaticiId, userId, StringComparison.Ordinal)) ||
                 talep.Teklifler.Any(t => string.Equals(t.SaticiId, userId, StringComparison.Ordinal)));

            if (!isBuyer && !isProductSeller && !isMatchedOrOfferedSeller)
            {
                throw new UnauthorizedAccessException("Bu talebi goruntuleme yetkiniz yok.");
            }

            return MapTalepDto(talep, isBuyer ? null : userId);
        }

        public async Task<IReadOnlyCollection<TalepDto>> GetAliciTalepleriAsync(string aliciId, string? durum = null)
        {
            var talepler = await _talepRepository.GetAliciTalepleriAsync(aliciId, durum);
            return talepler.Select(t => MapTalepDto(t)).ToList();
        }

        public async Task<PagedResult<TalepDto>> GetAliciTalepleriPagedAsync(string aliciId, string? durum, int page, int pageSize)
        {
            var paged = await _talepRepository.GetAliciTalepleriPagedAsync(aliciId, durum, page, pageSize);
            return new PagedResult<TalepDto>
            {
                Items = paged.Items.Select(t => MapTalepDto(t)).ToArray(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };
        }

        public async Task<IReadOnlyCollection<TalepDto>> GetSaticiTalepleriAsync(string saticiId, string? durum = null)
        {
            var talepler = await _talepRepository.GetSaticiTalepleriAsync(saticiId, durum);
            return talepler.Select(t => MapTalepDto(t, saticiId)).ToList();
        }

        public async Task<PagedResult<TalepDto>> GetSaticiTalepleriPagedAsync(string saticiId, string? durum, int page, int pageSize)
        {
            var paged = await _talepRepository.GetSaticiTalepleriPagedAsync(saticiId, durum, page, pageSize);
            return new PagedResult<TalepDto>
            {
                Items = paged.Items.Select(t => MapTalepDto(t, saticiId)).ToArray(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };
        }

        public async Task<PagedResult<TalepDto>> GetAcikPazarTalepleriAsync(string saticiId, string? durum, int page, int pageSize)
        {
            var paged = await _talepRepository.GetAcikPazarTalepleriForSellerAsync(saticiId, durum, page, pageSize);
            return new PagedResult<TalepDto>
            {
                Items = paged.Items.Select(t => MapTalepDto(t, saticiId)).ToArray(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };
        }

        public async Task<TalepTeklifDto> TeklifVerAsync(int talepId, string saticiId, TalepTeklifOlusturDto dto)
        {
            var talep = await _talepRepository.GetByIdWithDetailsAsync(talepId);
            if (talep == null)
            {
                throw new KeyNotFoundException("Talep bulunamadi.");
            }

            if (talep.Durum != TalepDurumlari.Acik)
            {
                throw new ArgumentException("Sadece acik taleplere teklif verilebilir.");
            }

            if (talep.TalepTuru == TalepTurleri.AcikPazar)
            {
                if (string.Equals(talep.AliciId, saticiId, StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException("Kendi actiginiz pazaryeri talebine teklif veremezsiniz.");
                }

                if (_saticiProfiliRepository != null)
                {
                    var saticiProfili = await _saticiProfiliRepository.GetByIdAsync(saticiId);
                    if (saticiProfili == null || !saticiProfili.AktifMi)
                    {
                        throw new UnauthorizedAccessException("Acik pazar taleplerine teklif vermek icin aktif bir satici profiliniz olmalidir.");
                    }
                }
            }
            else if (talep.Urun == null || !string.Equals(talep.Urun.SaticiId, saticiId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu talep icin teklif verme yetkiniz yok.");
            }

            var gonderimSehir = string.IsNullOrWhiteSpace(dto.GonderimSehir) ? null : dto.GonderimSehir.Trim();
            if (gonderimSehir == null && _saticiProfiliRepository != null)
            {
                var saticiProfil = await _saticiProfiliRepository.GetByIdAsync(saticiId);
                gonderimSehir = saticiProfil?.Sehir;
            }

            var mevcutTeklif = await _talepRepository.GetTeklifByTalepVeSaticiAsync(talepId, saticiId);
            if (mevcutTeklif != null)
            {
                mevcutTeklif.Mesaj = dto.Mesaj.Trim();
                mevcutTeklif.BirimFiyat = dto.BirimFiyat;
                mevcutTeklif.TeslimatSuresiGun = dto.TeslimatSuresiGun;
                mevcutTeklif.KargoDahilMi = dto.KargoDahilMi;
                mevcutTeklif.GonderimSehir = gonderimSehir;
                mevcutTeklif.Durum = TalepTeklifDurumlari.Beklemede;
                mevcutTeklif.GuncellemeTarihi = DateTime.UtcNow;
                talep.GuncellemeTarihi = DateTime.UtcNow;
                _talepRepository.Update(talep);
                await _talepRepository.SaveChangesAsync();

                var updated = await _talepRepository.GetTeklifByIdWithDetailsAsync(mevcutTeklif.Id) ?? mevcutTeklif;
                await TeklifBildirimiGonderAsync(talep, mevcutTeklif);
                await TeklifFiyatAlarmiKontrolEtAsync(talep, dto.BirimFiyat);
                return MapTalepTeklifDto(updated);
            }

            var teklif = new TalepTeklif
            {
                TalepId = talepId,
                SaticiId = saticiId,
                BirimFiyat = dto.BirimFiyat,
                Mesaj = dto.Mesaj.Trim(),
                TeslimatSuresiGun = dto.TeslimatSuresiGun,
                KargoDahilMi = dto.KargoDahilMi,
                GonderimSehir = gonderimSehir,
                Durum = TalepTeklifDurumlari.Beklemede
            };

            talep.GuncellemeTarihi = DateTime.UtcNow;
            await _talepRepository.AddTeklifAsync(teklif);
            _talepRepository.Update(talep);
            await _talepRepository.SaveChangesAsync();

            var created = await _talepRepository.GetTeklifByIdWithDetailsAsync(teklif.Id) ?? teklif;
            await TeklifBildirimiGonderAsync(talep, teklif);
            await TeklifFiyatAlarmiKontrolEtAsync(talep, dto.BirimFiyat);
            return MapTalepTeklifDto(created);
        }

        public async Task<TalepDto> TeklifKabulEtAsync(int teklifId, string aliciId)
        {
            var teklif = await _talepRepository.GetTeklifByIdWithDetailsAsync(teklifId);
            if (teklif == null || teklif.Talep == null)
            {
                throw new KeyNotFoundException("Teklif bulunamadi.");
            }

            if (!string.Equals(teklif.Talep.AliciId, aliciId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu teklif uzerinde islem yetkiniz yok.");
            }

            if (teklif.Talep.Durum != TalepDurumlari.Acik)
            {
                throw new ArgumentException("Bu talep zaten sonuclanmis.");
            }

            if (teklif.Durum != TalepTeklifDurumlari.Beklemede)
            {
                throw new ArgumentException("Sadece beklemedeki teklifler kabul edilebilir.");
            }

            teklif.Talep.Durum = TalepDurumlari.Anlasildi;
            teklif.Talep.GuncellemeTarihi = DateTime.UtcNow;

            foreach (var item in teklif.Talep.Teklifler)
            {
                item.Durum = item.Id == teklif.Id ? TalepTeklifDurumlari.Kabul : TalepTeklifDurumlari.Red;
                item.GuncellemeTarihi = DateTime.UtcNow;
            }

            _talepRepository.Update(teklif.Talep);
            await _talepRepository.SaveChangesAsync();

            var updatedTalep = await _talepRepository.GetByIdWithDetailsAsync(teklif.TalepId) ?? teklif.Talep;
            var urunBasligi = ResolveTalepBasligi(teklif.Talep);
            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = teklif.SaticiId,
                Tur = BildirimTurleri.Teklif,
                Baslik = "Teklif kabul edildi",
                Mesaj = $"{urunBasligi} icin verdiginiz teklif kabul edildi.",
                IlgiliVarlikTuru = "TalepTeklif",
                IlgiliVarlikId = teklif.Id.ToString(),
                AksiyonUrl = $"/talepler/{teklif.TalepId}"
            });

            return MapTalepDto(updatedTalep);
        }

        public async Task<TalepDto> TeklifReddetAsync(int teklifId, string aliciId)
        {
            var teklif = await _talepRepository.GetTeklifByIdWithDetailsAsync(teklifId);
            if (teklif == null || teklif.Talep == null)
            {
                throw new KeyNotFoundException("Teklif bulunamadi.");
            }

            if (!string.Equals(teklif.Talep.AliciId, aliciId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu teklif uzerinde islem yetkiniz yok.");
            }

            if (teklif.Talep.Durum != TalepDurumlari.Acik)
            {
                throw new ArgumentException("Bu talep zaten sonuclanmis.");
            }

            if (teklif.Durum != TalepTeklifDurumlari.Beklemede)
            {
                throw new ArgumentException("Sadece beklemedeki teklifler reddedilebilir.");
            }

            teklif.Durum = TalepTeklifDurumlari.Red;
            teklif.GuncellemeTarihi = DateTime.UtcNow;
            teklif.Talep.GuncellemeTarihi = DateTime.UtcNow;

            _talepRepository.Update(teklif.Talep);
            await _talepRepository.SaveChangesAsync();

            var updatedTalep = await _talepRepository.GetByIdWithDetailsAsync(teklif.TalepId) ?? teklif.Talep;
            var urunBasligi = ResolveTalepBasligi(teklif.Talep);
            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = teklif.SaticiId,
                Tur = BildirimTurleri.Teklif,
                Baslik = "Teklif reddedildi",
                Mesaj = $"{urunBasligi} icin verdiginiz teklif reddedildi.",
                IlgiliVarlikTuru = "TalepTeklif",
                IlgiliVarlikId = teklif.Id.ToString(),
                AksiyonUrl = $"/talepler/{teklif.TalepId}"
            });

            return MapTalepDto(updatedTalep);
        }

        public async Task<TalepDto> TalepIptalEtAsync(int talepId, string aliciId)
        {
            var talep = await _talepRepository.GetByIdWithDetailsAsync(talepId);
            if (talep == null)
            {
                throw new KeyNotFoundException("Talep bulunamadi.");
            }

            if (!string.Equals(talep.AliciId, aliciId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu talep uzerinde islem yetkiniz yok.");
            }

            if (talep.Durum != TalepDurumlari.Acik)
            {
                throw new ArgumentException("Sadece acik talepler iptal edilebilir.");
            }

            talep.Durum = TalepDurumlari.Iptal;
            talep.GuncellemeTarihi = DateTime.UtcNow;

            foreach (var teklif in talep.Teklifler.Where(t => t.Durum == TalepTeklifDurumlari.Beklemede))
            {
                teklif.Durum = TalepTeklifDurumlari.Red;
                teklif.GuncellemeTarihi = DateTime.UtcNow;
            }

            _talepRepository.Update(talep);
            await _talepRepository.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(talep.Urun?.SaticiId))
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = talep.Urun.SaticiId,
                    Tur = BildirimTurleri.Talep,
                    Baslik = "Talep iptal edildi",
                    Mesaj = $"{talep.Urun.Adi} urunu icin olusturulan talep alici tarafindan iptal edildi.",
                    IlgiliVarlikTuru = "Talep",
                    IlgiliVarlikId = talep.Id.ToString(),
                    AksiyonUrl = $"/talepler/{talep.Id}"
                });
            }

            return MapTalepDto(talep);
        }

        public async Task<TalepDto> AnlasmaOnayiVerAsync(int talepId, string userId, AnlasmaOnayiDto dto)
        {
            var talep = await _talepRepository.GetByIdWithDetailsAsync(talepId);
            if (talep == null)
            {
                throw new KeyNotFoundException("Talep bulunamadi.");
            }

            if (talep.Durum != TalepDurumlari.Anlasildi)
            {
                throw new ArgumentException("Sadece anlasilmis talepler icin gerceklesme onayi verilebilir.");
            }

            var acceptedOfferSellerId = talep.Teklifler
                .FirstOrDefault(t => t.Durum == TalepTeklifDurumlari.Kabul)?.SaticiId
                ?? talep.Urun?.SaticiId;

            if (string.Equals(talep.AliciId, userId, StringComparison.Ordinal))
            {
                talep.AliciGerceklestiOnayi = dto.GerceklestiMi;
            }
            else if (!string.IsNullOrWhiteSpace(acceptedOfferSellerId) &&
                     string.Equals(acceptedOfferSellerId, userId, StringComparison.Ordinal))
            {
                talep.SaticiGerceklestiOnayi = dto.GerceklestiMi;
            }
            else
            {
                throw new UnauthorizedAccessException("Bu talep icin anlasma onayi verme yetkiniz yok.");
            }

            talep.GuncellemeTarihi = DateTime.UtcNow;
            _talepRepository.Update(talep);
            await _talepRepository.SaveChangesAsync();

            return MapTalepDto(talep, string.Equals(talep.AliciId, userId, StringComparison.Ordinal) ? null : userId);
        }

        private async Task EslesmeleriOlusturVeBildirAsync(Talep talep)
        {
            var aktifUrunler = (await _urunRepository.FindAsync(u => u.AktifMi)).ToList();
            if (aktifUrunler.Count == 0 && _saticiProfiliRepository == null)
            {
                return;
            }

            var tercihSehirler = (talep.TercihEdilenSehirler ?? string.Empty)
                .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var keywords = (talep.ArananUrunAdi ?? string.Empty)
                .Split(new[] { ' ', ',', '.', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => k.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var saticiGruplari = aktifUrunler
                .Where(u => !string.Equals(u.SaticiId, talep.AliciId, StringComparison.Ordinal) &&
                            u.Satici != null &&
                            u.Satici.AktifMi)
                .GroupBy(u => u.SaticiId)
                .ToList();

            var eslesmeAdaylari = new List<(string SaticiId, int Skor, List<string> Sebepler, bool KatalogEslesmesi)>();

            foreach (var grup in saticiGruplari)
            {
                var urunler = grup.ToList();
                var satici = urunler[0].Satici;
                var skor = 0;
                var sebepler = new List<string>();
                var kategoriEslesmesi = false;
                var kelimeEslesmesi = false;

                if (talep.KategoriId.HasValue && urunler.Any(u => u.KategoriId == talep.KategoriId.Value))
                {
                    skor += 35;
                    kategoriEslesmesi = true;
                    sebepler.Add("Kategori eslesmesi");
                }

                var kelimeUrunleri = keywords.Count == 0
                    ? new List<Urun>()
                    : urunler.Where(u => keywords.Any(k =>
                        u.Adi.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(u.Aciklama) && u.Aciklama.Contains(k, StringComparison.OrdinalIgnoreCase))))
                        .ToList();

                if (kelimeUrunleri.Count > 0)
                {
                    skor += 35;
                    kelimeEslesmesi = true;
                    sebepler.Add("Urun anahtar kelime eslesmesi");
                }

                if (!string.IsNullOrWhiteSpace(satici.Sehir))
                {
                    if (tercihSehirler.Contains(satici.Sehir.Trim()))
                    {
                        skor += 30;
                        sebepler.Add($"Oncelikli uretim ili ({satici.Sehir.Trim()})");
                    }
                    else if (!string.IsNullOrWhiteSpace(talep.TeslimatSehir) &&
                             string.Equals(satici.Sehir.Trim(), talep.TeslimatSehir.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        skor += 15;
                        sebepler.Add($"Teslimat sehri ({satici.Sehir.Trim()})");
                    }
                }

                var stokKontrolUrunleri = kelimeUrunleri.Count > 0
                    ? kelimeUrunleri
                    : (talep.KategoriId.HasValue ? urunler.Where(u => u.KategoriId == talep.KategoriId.Value).ToList() : urunler);

                if (stokKontrolUrunleri.Any(u => u.StokMiktari > 0))
                {
                    skor += 15;
                    sebepler.Add("Stok hazir");
                }

                if (SaticiProfili.IsVerified(satici))
                {
                    skor += 15;
                    sebepler.Add("Dogrulanmis uretici");
                }

                var puanlar = urunler.SelectMany(u => u.Puanlar).ToList();
                if (puanlar.Count > 0 && puanlar.Average(p => p.PuanDegeri) >= 4.0)
                {
                    skor += 5;
                }

                eslesmeAdaylari.Add((grup.Key, skor, sebepler, kategoriEslesmesi || kelimeEslesmesi));
            }

            var katalogUygunlari = eslesmeAdaylari
                .Where(x => x.KatalogEslesmesi && x.Skor >= 20)
                .OrderByDescending(x => x.Skor)
                .Take(15)
                .ToList();

            var secilenler = katalogUygunlari.Count > 0
                ? katalogUygunlari
                : eslesmeAdaylari
                    .Where(x => x.Skor >= 15)
                    .OrderByDescending(x => x.Skor)
                    .Take(5)
                    .ToList();

            if (secilenler.Count == 0)
            {
                return;
            }

            var eslesmeKayitlari = secilenler
                .Select(x => new TalepEslesme
                {
                    TalepId = talep.Id,
                    SaticiId = x.SaticiId,
                    EslesmeSkoru = x.Skor,
                    Sebep = string.Join(", ", x.Sebepler.Distinct())
                })
                .ToList();

            await _talepRepository.AddEslesmelerAsync(eslesmeKayitlari);
            await _talepRepository.SaveChangesAsync();

            var baslik = talep.TalepTipi == TalepTipleri.Kurumsal
                ? "Yeni kurumsal alim talebi"
                : "Eslesen acik pazar talebi";
            var urunOzeti = $"{talep.Miktar} {talep.Birim ?? "adet"} {talep.ArananUrunAdi}".Trim();

            foreach (var eslesme in eslesmeKayitlari)
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = eslesme.SaticiId,
                    Tur = BildirimTurleri.Talep,
                    Baslik = baslik,
                    Mesaj = $"{urunOzeti} ({talep.TeslimatSehir ?? "Teslimat"}) icin uygun uretici olarak eslestiniz.",
                    IlgiliVarlikTuru = "Talep",
                    IlgiliVarlikId = talep.Id.ToString(),
                    AksiyonUrl = $"/talepler/{talep.Id}"
                });
            }
        }

        private async Task TeklifBildirimiGonderAsync(Talep talep, TalepTeklif teklif)
        {
            var urunBasligi = ResolveTalepBasligi(talep);
            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = talep.AliciId,
                Tur = BildirimTurleri.Teklif,
                Baslik = "Yeni teklif",
                Mesaj = $"{urunBasligi} talebiniz icin yeni teklif var.",
                IlgiliVarlikTuru = "TalepTeklif",
                IlgiliVarlikId = teklif.Id.ToString(),
                AksiyonUrl = $"/talepler/{talep.Id}"
            });
        }

        private async Task TeklifFiyatAlarmiKontrolEtAsync(Talep talep, decimal? birimFiyat)
        {
            if (_fiyatAlarmiRepository == null || !talep.UrunId.HasValue || !birimFiyat.HasValue)
            {
                return;
            }

            var alarm = await _fiyatAlarmiRepository.GetAsync(talep.AliciId, talep.UrunId.Value);
            if (alarm == null || !alarm.AktifMi || birimFiyat.Value > alarm.HedefFiyat)
            {
                return;
            }

            alarm.SonTetiklenenFiyat = birimFiyat.Value;
            alarm.SonTetiklemeTarihi = DateTime.UtcNow;
            _fiyatAlarmiRepository.Update(alarm);
            await _fiyatAlarmiRepository.SaveChangesAsync();

            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = talep.AliciId,
                Tur = BildirimTurleri.FiyatAlarmi,
                Baslik = "Hedef fiyat teklifi geldi",
                Mesaj = $"{ResolveTalepBasligi(talep)} icin {birimFiyat.Value:0.##} TL tutarinda teklif hedef fiyatinizi karsiliyor.",
                IlgiliVarlikTuru = "Talep",
                IlgiliVarlikId = talep.Id.ToString(),
                AksiyonUrl = $"/talepler/{talep.Id}"
            });
        }

        private static string ResolveTalepBasligi(Talep talep)
        {
            return talep.Urun?.Adi ?? talep.ArananUrunAdi ?? "Urun";
        }

        private static TalepDto MapTalepDto(Talep talep, string? currentSaticiId = null)
        {
            var kabulTeklif = talep.Teklifler.FirstOrDefault(t => t.Durum == TalepTeklifDurumlari.Kabul);
            var saticiEslesme = string.IsNullOrWhiteSpace(currentSaticiId)
                ? null
                : talep.Eslesmeler.FirstOrDefault(e => string.Equals(e.SaticiId, currentSaticiId, StringComparison.Ordinal));

            return new TalepDto
            {
                Id = talep.Id,
                AliciId = talep.AliciId,
                AliciAdi = talep.Alici?.UserName ?? talep.Alici?.Email,
                TalepTuru = talep.TalepTuru,
                TalepTipi = talep.TalepTipi,
                UrunId = talep.UrunId,
                UrunAdi = talep.Urun?.Adi,
                UrunResimUrl = talep.Urun?.Resimler?.FirstOrDefault()?.Url,
                UrunFiyat = talep.Urun?.Fiyat,
                KategoriId = talep.KategoriId ?? talep.Urun?.KategoriId,
                KategoriAdi = talep.Kategori?.Adi ?? talep.Urun?.Kategori?.Adi,
                ArananUrunAdi = talep.ArananUrunAdi ?? talep.Urun?.Adi,
                Birim = talep.Birim ?? talep.Urun?.BirimMiktar,
                TeslimatSehir = talep.TeslimatSehir,
                TercihEdilenSehirler = talep.TercihEdilenSehirler,
                HedefBirimFiyat = talep.HedefBirimFiyat,
                SonTeklifTarihi = talep.SonTeklifTarihi,
                SaticiId = talep.Urun?.SaticiId ?? kabulTeklif?.SaticiId,
                SaticiMagazaAdi = talep.Urun?.Satici?.MagazaAdi ?? kabulTeklif?.Satici?.SaticiProfili?.MagazaAdi,
                SaticiSehir = talep.Urun?.Satici?.Sehir ?? kabulTeklif?.Satici?.SaticiProfili?.Sehir,
                SaticiIlce = talep.Urun?.Satici?.Ilce ?? kabulTeklif?.Satici?.SaticiProfili?.Ilce,
                Miktar = talep.Miktar,
                Not = talep.Not,
                Durum = talep.Durum,
                AliciGerceklestiOnayi = talep.AliciGerceklestiOnayi,
                SaticiGerceklestiOnayi = talep.SaticiGerceklestiOnayi,
                EslesmeSkoru = saticiEslesme?.EslesmeSkoru,
                EslesmeSebebi = saticiEslesme?.Sebep,
                EslesenSaticiSayisi = talep.Eslesmeler.Count,
                OlusturmaTarihi = talep.OlusturmaTarihi,
                GuncellemeTarihi = talep.GuncellemeTarihi,
                Teklifler = talep.Teklifler
                    .OrderByDescending(t => t.OlusturmaTarihi)
                    .Select(MapTalepTeklifDto)
                    .ToList()
            };
        }

        private static TalepTeklifDto MapTalepTeklifDto(TalepTeklif teklif)
        {
            var saticiProfili = teklif.Satici?.SaticiProfili;
            return new TalepTeklifDto
            {
                Id = teklif.Id,
                TalepId = teklif.TalepId,
                SaticiId = teklif.SaticiId,
                SaticiMagazaAdi = saticiProfili?.MagazaAdi,
                SaticiSehir = saticiProfili?.Sehir,
                SaticiDogrulanmis = SaticiProfili.IsVerified(saticiProfili),
                BirimFiyat = teklif.BirimFiyat,
                Mesaj = teklif.Mesaj,
                TeslimatSuresiGun = teklif.TeslimatSuresiGun,
                KargoDahilMi = teklif.KargoDahilMi,
                GonderimSehir = teklif.GonderimSehir ?? saticiProfili?.Sehir,
                Durum = teklif.Durum,
                OlusturmaTarihi = teklif.OlusturmaTarihi,
                GuncellemeTarihi = teklif.GuncellemeTarihi
            };
        }
    }
}
