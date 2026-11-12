using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces
{
    public interface IOrtakAlimService
    {
        Task<PagedResult<OrtakAlimDto>> GetAllAsync(string? hedefSehir, string? menseiSehir, string? q, string? durum, int page = 1, int pageSize = 12);
        Task<OrtakAlimDto?> GetByIdAsync(int id);
        Task<OrtakAlimDto> OlusturAsync(string userId, IReadOnlyCollection<string> roles, OrtakAlimOlusturDto dto);
        Task<OrtakAlimDto> OnaylaAsync(int id, string saticiId);
        Task<OrtakAlimDto> KatilAsync(int id, string aliciId, OrtakAlimKatilDto? dto = null);
        Task<OrtakAlimDto> KatilimiIptalEtAsync(int id, string aliciId);
        Task<OrtakAlimDto> DurumGuncelleAsync(int id, string saticiId, string yeniDurum);
    }
}
