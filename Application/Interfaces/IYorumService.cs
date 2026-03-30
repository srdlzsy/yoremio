using Application.DTOs;

namespace Application.Interfaces
{
    public interface IYorumServices
    {
        Task<IEnumerable<YorumDto>> GetYorumlarByUrunIdAsync(int urunId);
        Task<YorumDto> YorumEkleAsync(YorumEkleDto dto, string kullaniciId);
        Task<YorumDto> GuncelleYorumAsync(int yorumId, YorumEkleDto yorumDto, string kullaniciId);
        Task<bool> SilYorumAsync(int yorumId, string kullaniciId);
    }
}
