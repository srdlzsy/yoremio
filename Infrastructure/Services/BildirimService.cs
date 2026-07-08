using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class BildirimService : IBildirimService
    {
        private const int MaxTitleLength = 120;
        private const int MaxMessageLength = 500;

        private readonly IBildirimRepository _bildirimRepository;

        public BildirimService(IBildirimRepository bildirimRepository)
        {
            _bildirimRepository = bildirimRepository;
        }

        public async Task<BildirimDto> BildirimOlusturAsync(BildirimOlusturDto dto)
        {
            var kullaniciId = dto.KullaniciId?.Trim() ?? string.Empty;
            var tur = dto.Tur?.Trim() ?? string.Empty;
            var baslik = NormalizeLength(dto.Baslik, MaxTitleLength);
            var mesaj = NormalizeLength(dto.Mesaj, MaxMessageLength);

            if (string.IsNullOrWhiteSpace(kullaniciId))
            {
                throw new ArgumentException("Bildirim kullanicisi bos olamaz.");
            }

            if (string.IsNullOrWhiteSpace(tur))
            {
                throw new ArgumentException("Bildirim turu bos olamaz.");
            }

            if (string.IsNullOrWhiteSpace(baslik))
            {
                throw new ArgumentException("Bildirim basligi bos olamaz.");
            }

            if (string.IsNullOrWhiteSpace(mesaj))
            {
                throw new ArgumentException("Bildirim mesaji bos olamaz.");
            }

            var bildirim = new Bildirim
            {
                KullaniciId = kullaniciId,
                Tur = tur,
                Baslik = baslik,
                Mesaj = mesaj,
                IlgiliVarlikTuru = NormalizeOptional(dto.IlgiliVarlikTuru, 50),
                IlgiliVarlikId = NormalizeOptional(dto.IlgiliVarlikId, 100),
                AksiyonUrl = NormalizeOptional(dto.AksiyonUrl, 300),
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _bildirimRepository.AddAsync(bildirim);
            await _bildirimRepository.SaveChangesAsync();
            return MapToDto(bildirim);
        }

        public async Task<BildirimPagedResultDto> GetBildirimlerAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize)
        {
            kullaniciId = ValidateUserId(kullaniciId);
            var result = await _bildirimRepository.GetKullaniciBildirimleriAsync(kullaniciId, sadeceOkunmamis, page, pageSize);
            var okunmamisSayisi = await _bildirimRepository.GetOkunmamisSayisiAsync(kullaniciId);

            return new BildirimPagedResultDto
            {
                Items = result.Items.Select(MapToDto).ToList(),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                OkunmamisSayisi = okunmamisSayisi
            };
        }

        public async Task<int> GetOkunmamisSayisiAsync(string kullaniciId)
        {
            kullaniciId = ValidateUserId(kullaniciId);
            return await _bildirimRepository.GetOkunmamisSayisiAsync(kullaniciId);
        }

        public async Task<BildirimDto> OkunduIsaretleAsync(long bildirimId, string kullaniciId)
        {
            kullaniciId = ValidateUserId(kullaniciId);
            var bildirim = await _bildirimRepository.GetKullaniciBildirimiAsync(bildirimId, kullaniciId);
            if (bildirim == null)
            {
                throw new KeyNotFoundException("Bildirim bulunamadi.");
            }

            bildirim.OkunmaTarihi ??= DateTime.UtcNow;
            await _bildirimRepository.SaveChangesAsync();
            return MapToDto(bildirim);
        }

        public async Task<int> TumunuOkunduIsaretleAsync(string kullaniciId)
        {
            kullaniciId = ValidateUserId(kullaniciId);
            return await _bildirimRepository.TumunuOkunduIsaretleAsync(kullaniciId, DateTime.UtcNow);
        }

        private static BildirimDto MapToDto(Bildirim bildirim)
        {
            return new BildirimDto
            {
                Id = bildirim.Id,
                Tur = bildirim.Tur,
                Baslik = bildirim.Baslik,
                Mesaj = bildirim.Mesaj,
                IlgiliVarlikTuru = bildirim.IlgiliVarlikTuru,
                IlgiliVarlikId = bildirim.IlgiliVarlikId,
                AksiyonUrl = bildirim.AksiyonUrl,
                OlusturmaTarihi = bildirim.OlusturmaTarihi,
                OkunmaTarihi = bildirim.OkunmaTarihi
            };
        }

        private static string ValidateUserId(string kullaniciId)
        {
            kullaniciId = kullaniciId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(kullaniciId))
            {
                throw new UnauthorizedAccessException("Kullanici dogrulanamadi.");
            }

            return kullaniciId;
        }

        private static string NormalizeLength(string? value, int maxLength)
        {
            value = value?.Trim() ?? string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            value = value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
