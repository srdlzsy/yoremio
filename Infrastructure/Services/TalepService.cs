using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class TalepService : ITalepService
    {
        private readonly ITalepRepository _talepRepository;
        private readonly IUrunRepository _urunRepository;
        private readonly IBildirimService _bildirimService;

        public TalepService(
            ITalepRepository talepRepository,
            IUrunRepository urunRepository,
            IBildirimService bildirimService)
        {
            _talepRepository = talepRepository;
            _urunRepository = urunRepository;
            _bildirimService = bildirimService;
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

        public async Task<TalepDto> GetTalepByIdAsync(int talepId, string userId)
        {
            var talep = await _talepRepository.GetByIdWithDetailsAsync(talepId);
            if (talep == null)
            {
                throw new KeyNotFoundException("Talep bulunamadi.");
            }

            var isBuyer = string.Equals(talep.AliciId, userId, StringComparison.Ordinal);
            var isSeller = talep.Urun != null && string.Equals(talep.Urun.SaticiId, userId, StringComparison.Ordinal);
            if (!isBuyer && !isSeller)
            {
                throw new UnauthorizedAccessException("Bu talebi goruntuleme yetkiniz yok.");
            }

            return MapTalepDto(talep);
        }

        public async Task<IReadOnlyCollection<TalepDto>> GetAliciTalepleriAsync(string aliciId)
        {
            var talepler = await _talepRepository.GetAliciTalepleriAsync(aliciId);
            return talepler.Select(MapTalepDto).ToList();
        }

        public async Task<IReadOnlyCollection<TalepDto>> GetSaticiTalepleriAsync(string saticiId)
        {
            var talepler = await _talepRepository.GetSaticiTalepleriAsync(saticiId);
            return talepler.Select(MapTalepDto).ToList();
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

            if (talep.Urun == null || !string.Equals(talep.Urun.SaticiId, saticiId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu talep icin teklif verme yetkiniz yok.");
            }

            var mevcutTeklif = await _talepRepository.GetTeklifByTalepVeSaticiAsync(talepId, saticiId);
            if (mevcutTeklif != null)
            {
                mevcutTeklif.Mesaj = dto.Mesaj.Trim();
                mevcutTeklif.BirimFiyat = dto.BirimFiyat;
                mevcutTeklif.Durum = TalepTeklifDurumlari.Beklemede;
                _talepRepository.Update(talep);
                await _talepRepository.SaveChangesAsync();

                var updated = await _talepRepository.GetTeklifByIdWithDetailsAsync(mevcutTeklif.Id) ?? mevcutTeklif;
                await TeklifBildirimiGonderAsync(talep, mevcutTeklif);
                return MapTalepTeklifDto(updated);
            }

            var teklif = new TalepTeklif
            {
                TalepId = talepId,
                SaticiId = saticiId,
                BirimFiyat = dto.BirimFiyat,
                Mesaj = dto.Mesaj.Trim(),
                Durum = TalepTeklifDurumlari.Beklemede
            };

            await _talepRepository.AddTeklifAsync(teklif);
            await _talepRepository.SaveChangesAsync();

            var created = await _talepRepository.GetTeklifByIdWithDetailsAsync(teklif.Id) ?? teklif;
            await TeklifBildirimiGonderAsync(talep, teklif);
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
            }

            _talepRepository.Update(teklif.Talep);
            await _talepRepository.SaveChangesAsync();

            var updatedTalep = await _talepRepository.GetByIdWithDetailsAsync(teklif.TalepId) ?? teklif.Talep;
            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = teklif.SaticiId,
                Tur = BildirimTurleri.Teklif,
                Baslik = "Teklif kabul edildi",
                Mesaj = $"{teklif.Talep.Urun?.Adi ?? "Urun"} icin verdiginiz teklif kabul edildi.",
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
            teklif.Talep.GuncellemeTarihi = DateTime.UtcNow;

            _talepRepository.Update(teklif.Talep);
            await _talepRepository.SaveChangesAsync();

            var updatedTalep = await _talepRepository.GetByIdWithDetailsAsync(teklif.TalepId) ?? teklif.Talep;
            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = teklif.SaticiId,
                Tur = BildirimTurleri.Teklif,
                Baslik = "Teklif reddedildi",
                Mesaj = $"{teklif.Talep.Urun?.Adi ?? "Urun"} icin verdiginiz teklif reddedildi.",
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

        private async Task TeklifBildirimiGonderAsync(Talep talep, TalepTeklif teklif)
        {
            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = talep.AliciId,
                Tur = BildirimTurleri.Teklif,
                Baslik = "Yeni teklif",
                Mesaj = $"{talep.Urun?.Adi ?? "Urun"} talebiniz icin yeni teklif var.",
                IlgiliVarlikTuru = "TalepTeklif",
                IlgiliVarlikId = teklif.Id.ToString(),
                AksiyonUrl = $"/talepler/{talep.Id}"
            });
        }

        private static TalepDto MapTalepDto(Talep talep)
        {
            return new TalepDto
            {
                Id = talep.Id,
                AliciId = talep.AliciId,
                UrunId = talep.UrunId,
                UrunAdi = talep.Urun?.Adi,
                UrunResimUrl = talep.Urun?.Resimler?.FirstOrDefault()?.Url,
                UrunFiyat = talep.Urun?.Fiyat,
                SaticiId = talep.Urun?.SaticiId,
                SaticiMagazaAdi = talep.Urun?.Satici?.MagazaAdi,
                SaticiSehir = talep.Urun?.Satici?.Sehir,
                SaticiIlce = talep.Urun?.Satici?.Ilce,
                Miktar = talep.Miktar,
                Not = talep.Not,
                Durum = talep.Durum,
                OlusturmaTarihi = talep.OlusturmaTarihi,
                Teklifler = talep.Teklifler
                    .OrderByDescending(t => t.OlusturmaTarihi)
                    .Select(MapTalepTeklifDto)
                    .ToList()
            };
        }

        private static TalepTeklifDto MapTalepTeklifDto(TalepTeklif teklif)
        {
            return new TalepTeklifDto
            {
                Id = teklif.Id,
                TalepId = teklif.TalepId,
                SaticiId = teklif.SaticiId,
                SaticiMagazaAdi = teklif.Satici?.SaticiProfili?.MagazaAdi,
                BirimFiyat = teklif.BirimFiyat,
                Mesaj = teklif.Mesaj,
                Durum = teklif.Durum,
                OlusturmaTarihi = teklif.OlusturmaTarihi
            };
        }
    }
}
