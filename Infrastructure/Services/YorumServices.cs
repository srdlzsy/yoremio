using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class YorumServices : IYorumServices
    {
        private readonly IYorumRepository _yorumRepository;
        private readonly IUrunRepository _urunRepository;

        public YorumServices(IYorumRepository yorumRepository, IUrunRepository urunRepository)
        {
            _yorumRepository = yorumRepository;
            _urunRepository = urunRepository;
        }

        public async Task<YorumDto> YorumEkleAsync(YorumEkleDto dto, string kullaniciId)
        {
            var urun = await _urunRepository.GetByIdAsync(dto.UrunId);
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");

            var yorum = new Yorum
            {
                YorumYapanKullaniciId = kullaniciId,
                UrunId = dto.UrunId,
                Icerik = dto.Icerik,
                Tarih = DateTime.UtcNow
            };

            await _yorumRepository.AddAsync(yorum);
            await _yorumRepository.SaveChangesAsync();

            return MapToDto(yorum);
        }

        public async Task<IEnumerable<YorumDto>> GetYorumlarByUrunIdAsync(int urunId)
        {
            var yorumlar = await _yorumRepository.GetYorumlarByUrunIdAsync(urunId);
            return yorumlar.Select(MapToDto);
        }

        public async Task<YorumDto> GuncelleYorumAsync(int yorumId, YorumEkleDto yorumDto, string kullaniciId)
        {
            var yorum = await _yorumRepository.GetByIdWithUserAsync(yorumId);
            if (yorum == null)
                throw new Exception("Yorum bulunamadı.");

            if (!string.Equals(yorum.YorumYapanKullaniciId, kullaniciId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu yorum üzerinde işlem yetkiniz yok.");

            yorum.Icerik = yorumDto.Icerik;
            yorum.Tarih = DateTime.UtcNow;
            _yorumRepository.Update(yorum);
            await _yorumRepository.SaveChangesAsync();

            return MapToDto(yorum);
        }

        public async Task<bool> SilYorumAsync(int yorumId, string kullaniciId)
        {
            var yorum = await _yorumRepository.GetByIdAsync(yorumId);
            if (yorum == null)
                return false;

            if (!string.Equals(yorum.YorumYapanKullaniciId, kullaniciId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Bu yorum üzerinde işlem yetkiniz yok.");

            _yorumRepository.Remove(yorum);
            return await _yorumRepository.SaveChangesAsync();
        }

        private static YorumDto MapToDto(Yorum yorum)
        {
            return new YorumDto
            {
                Id = yorum.Id,
                Icerik = yorum.Icerik,
                Tarih = yorum.Tarih,
                KullaniciId = yorum.YorumYapanKullaniciId,
                UrunId = yorum.UrunId,
                KullaniciAdi = yorum.YorumYapanKullanici?.UserName
            };
        }
    }
}
