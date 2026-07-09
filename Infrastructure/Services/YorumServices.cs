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
        private readonly ITalepRepository _talepRepository;

        public YorumServices(IYorumRepository yorumRepository, IUrunRepository urunRepository, ITalepRepository talepRepository)
        {
            _yorumRepository = yorumRepository;
            _urunRepository = urunRepository;
            _talepRepository = talepRepository;
        }

        public async Task<YorumDto> YorumEkleAsync(YorumEkleDto dto, string kullaniciId)
        {
            var yetki = await GetYorumYetkisiAsync(dto.UrunId, kullaniciId);
            if (!yetki.YorumYapabilir)
            {
                if (string.Equals(yetki.Sebep, "Urun bulunamadi.", StringComparison.Ordinal))
                {
                    throw new KeyNotFoundException(yetki.Sebep);
                }

                throw new UnauthorizedAccessException(yetki.Sebep ?? "Yorum yapma yetkiniz yok.");
            }

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

        public async Task<YorumYetkisiDto> GetYorumYetkisiAsync(int urunId, string kullaniciId)
        {
            if (string.IsNullOrWhiteSpace(kullaniciId))
            {
                return new YorumYetkisiDto
                {
                    YorumYapabilir = false,
                    Sebep = "Kullanici dogrulanamadi."
                };
            }

            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null)
            {
                return new YorumYetkisiDto
                {
                    YorumYapabilir = false,
                    Sebep = "Urun bulunamadi."
                };
            }

            if (!urun.AktifMi)
            {
                return new YorumYetkisiDto
                {
                    YorumYapabilir = false,
                    Sebep = "Pasif urune yorum yapilamaz."
                };
            }

            if (string.Equals(urun.SaticiId, kullaniciId, StringComparison.Ordinal))
            {
                return new YorumYetkisiDto
                {
                    YorumYapabilir = false,
                    Sebep = "Kendi urununuz icin yorum yapamazsiniz."
                };
            }

            if (!await _talepRepository.HasAcceptedDemandForProductAsync(kullaniciId, urunId))
            {
                return new YorumYetkisiDto
                {
                    YorumYapabilir = false,
                    Sebep = "Yorum yapabilmek icin bu urunle ilgili kabul edilmis bir talebiniz olmalidir."
                };
            }

            return new YorumYetkisiDto
            {
                YorumYapabilir = true
            };
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
