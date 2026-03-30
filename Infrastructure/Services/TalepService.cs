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

        public TalepService(ITalepRepository talepRepository, IUrunRepository urunRepository)
        {
            _talepRepository = talepRepository;
            _urunRepository = urunRepository;
        }

        public async Task<TalepDto> TalepOlusturAsync(string aliciId, TalepOlusturDto dto)
        {
            var urun = await _urunRepository.GetByIdWithRelationsAsync(dto.UrunId);
            if (urun == null || !urun.AktifMi)
            {
                throw new KeyNotFoundException("Urun bulunamadi.");
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
            return MapTalepDto(created);
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

            teklif.Talep.Durum = TalepDurumlari.Anlasildi;
            teklif.Talep.GuncellemeTarihi = DateTime.UtcNow;

            foreach (var item in teklif.Talep.Teklifler)
            {
                item.Durum = item.Id == teklif.Id ? TalepTeklifDurumlari.Kabul : TalepTeklifDurumlari.Red;
            }

            _talepRepository.Update(teklif.Talep);
            await _talepRepository.SaveChangesAsync();

            var updatedTalep = await _talepRepository.GetByIdWithDetailsAsync(teklif.TalepId) ?? teklif.Talep;
            return MapTalepDto(updatedTalep);
        }

        private static TalepDto MapTalepDto(Talep talep)
        {
            return new TalepDto
            {
                Id = talep.Id,
                AliciId = talep.AliciId,
                UrunId = talep.UrunId,
                UrunAdi = talep.Urun?.Adi,
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
