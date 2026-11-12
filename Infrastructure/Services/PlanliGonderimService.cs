using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class PlanliGonderimService : IPlanliGonderimService
    {
        private readonly IPlanliGonderimRepository _planliGonderimRepository;
        private readonly ISaticiProfiliRepository _saticiProfiliRepository;
        private readonly ISaticiTakipRepository? _saticiTakipRepository;
        private readonly IBildirimService? _bildirimService;

        public PlanliGonderimService(
            IPlanliGonderimRepository planliGonderimRepository,
            ISaticiProfiliRepository saticiProfiliRepository,
            ISaticiTakipRepository? saticiTakipRepository = null,
            IBildirimService? bildirimService = null)
        {
            _planliGonderimRepository = planliGonderimRepository;
            _saticiProfiliRepository = saticiProfiliRepository;
            _saticiTakipRepository = saticiTakipRepository;
            _bildirimService = bildirimService;
        }

        public async Task<IReadOnlyCollection<PlanliGonderimDto>> GetUpcomingAsync(
            string? hedefSehir = null,
            string? menseiSehir = null,
            string? saticiId = null,
            int take = 20)
        {
            var items = await _planliGonderimRepository.GetUpcomingAsync(hedefSehir, menseiSehir, saticiId, take);
            return items.Select(MapToDto).ToArray();
        }

        public async Task<PlanliGonderimDto> OlusturAsync(string saticiId, PlanliGonderimOlusturDto dto)
        {
            var satici = await _saticiProfiliRepository.GetSaticiWithUserByIdAsync(saticiId);
            if (satici == null || !satici.AktifMi)
            {
                throw new UnauthorizedAccessException("Planli gonderim olusturmak icin aktif bir satici profiliniz olmalidir.");
            }

            if (dto.GonderimTarihi <= DateTime.UtcNow)
            {
                throw new ArgumentException("Gonderim tarihi gelecekte olmalidir.");
            }

            if (dto.SonTalepTarihi > dto.GonderimTarihi)
            {
                throw new ArgumentException("Son talep tarihi gonderim tarihinden sonra olamaz.");
            }

            var entity = new PlanliGonderim
            {
                SaticiId = saticiId,
                Baslik = dto.Baslik.Trim(),
                Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
                MenseiSehir = dto.MenseiSehir.Trim(),
                HedefSehir = dto.HedefSehir.Trim(),
                TahminiUrunNotu = string.IsNullOrWhiteSpace(dto.TahminiUrunNotu) ? null : dto.TahminiUrunNotu.Trim(),
                GonderimTarihi = dto.GonderimTarihi.ToUniversalTime(),
                SonTalepTarihi = dto.SonTalepTarihi.ToUniversalTime(),
                SogukZincirVarMi = dto.SogukZincirVarMi,
                AktifMi = true
            };

            await _planliGonderimRepository.AddAsync(entity);
            await _planliGonderimRepository.SaveChangesAsync();

            if (_saticiTakipRepository != null && _bildirimService != null)
            {
                var takipciler = await _saticiTakipRepository.GetFollowersWithNotificationAsync(saticiId);
                foreach (var takip in takipciler)
                {
                    await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                    {
                        KullaniciId = takip.AliciId,
                        Tur = BildirimTurleri.GonderimPlani,
                        Baslik = $"{entity.HedefSehir} icin planli gonderim",
                        Mesaj = $"{satici.MagazaAdi}, {entity.MenseiSehir} -> {entity.HedefSehir} rotasinda {entity.GonderimTarihi:dd.MM.yyyy} tarihinde gonderim planladi.",
                        IlgiliVarlikTuru = "PlanliGonderim",
                        IlgiliVarlikId = entity.Id.ToString(),
                        AksiyonUrl = $"/kesfet?teslimatSehir={Uri.EscapeDataString(entity.HedefSehir)}"
                    });
                }
            }

            var created = await _planliGonderimRepository.GetByIdWithSellerAsync(entity.Id) ?? entity;
            if (created.Satici == null) created.Satici = satici;
            return MapToDto(created);
        }

        public async Task<bool> IptalEtAsync(int id, string saticiId)
        {
            var entity = await _planliGonderimRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Planli gonderim bulunamadi.");
            }

            if (!string.Equals(entity.SaticiId, saticiId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Bu planli gonderimi iptal etme yetkiniz yok.");
            }

            entity.AktifMi = false;
            _planliGonderimRepository.Update(entity);
            return await _planliGonderimRepository.SaveChangesAsync();
        }

        internal static PlanliGonderimDto MapToDto(PlanliGonderim p)
        {
            return new PlanliGonderimDto
            {
                Id = p.Id,
                SaticiId = p.SaticiId,
                SaticiMagazaAdi = p.Satici?.MagazaAdi,
                SaticiDogrulanmis = SaticiProfili.IsVerified(p.Satici),
                Baslik = p.Baslik,
                Aciklama = p.Aciklama,
                MenseiSehir = p.MenseiSehir,
                HedefSehir = p.HedefSehir,
                TahminiUrunNotu = p.TahminiUrunNotu,
                GonderimTarihi = p.GonderimTarihi,
                SonTalepTarihi = p.SonTalepTarihi,
                SogukZincirVarMi = p.SogukZincirVarMi,
                AktifMi = p.AktifMi,
                OlusturmaTarihi = p.OlusturmaTarihi
            };
        }
    }
}
