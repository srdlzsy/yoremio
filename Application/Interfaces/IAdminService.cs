using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<AdminKullaniciDto>> GetKullanicilarAsync(AdminKullaniciQuery query);
        Task<AdminKullaniciDto> SetSaticiDogrulamaAsync(string saticiId, AdminSaticiDogrulamaDto dto);
        Task<AdminKullaniciDto> SetSaticiDurumAsync(string saticiId, AdminDurumGuncelleDto dto);
        Task<PagedResult<UrunDto>> GetUrunlerAsync(AdminUrunQuery query);
        Task<UrunDto> SetUrunDurumAsync(int urunId, AdminDurumGuncelleDto dto);
        Task<PagedResult<AdminYorumDto>> GetYorumlarAsync(AdminYorumQuery query);
        Task<AdminYorumDto> SetYorumDurumAsync(int yorumId, AdminDurumGuncelleDto dto);
        Task<IReadOnlyCollection<TalepBosluguDto>> GetTalepBosluklariAsync(int top = 20);
    }
}
