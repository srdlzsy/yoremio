using Application.DTOs;
using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces
{
    public interface IUrunService
    {
        Task<Urun> UrunEkleAsync(string saticiId, UrunEkleDto dto);
        Task<PagedResult<UrunDto>> GetAllUrunlerAsync(UrunQuery query);
        Task<IEnumerable<UrunDto>> GetOnerilenUrunlerAsync(string kullaniciId, int take);
        Task<IEnumerable<UrunDto>> GetUrunlerBySaticiIdAsync(string saticiId);
        Task<IEnumerable<UrunDto>> GetFavoriUrunlerAsync(string kullaniciId);
        Task<bool> FavoriyeEkleAsync(string kullaniciId, int urunId);
        Task<bool> FavoridenCikarAsync(string kullaniciId, int urunId);
        Task<UrunDto?> GetUrunByIdAsync(int urunId);
        Task<UrunDto> UrunGuncelleAsync(int urunId, string saticiId, UrunGuncelleDto dto);
        Task<bool> UrunSilAsync(int urunId, string saticiId);
        Task<bool> UrunResimSilAsync(int urunId, int resimId, string saticiId);
        Task<bool> UrunVideoSilAsync(int urunId, int videoId, string saticiId);
        Task<Urun?> EkleMedyaToUrunAsync(int urunId, List<string> resimUrlListesi, List<string> videoUrlListesi);
    }
}
