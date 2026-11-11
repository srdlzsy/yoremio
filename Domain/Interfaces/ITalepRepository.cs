using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces
{
    public interface ITalepRepository : IBaseRepository<Talep>
    {
        Task<Talep?> GetByIdWithDetailsAsync(int talepId);
        Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId, string? durum = null);
        Task<PagedResult<Talep>> GetAliciTalepleriPagedAsync(string aliciId, string? durum, int page, int pageSize);
        Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId, string? durum = null);
        Task<PagedResult<Talep>> GetSaticiTalepleriPagedAsync(string saticiId, string? durum, int page, int pageSize);
        Task<TalepTeklif?> GetTeklifByIdWithDetailsAsync(int teklifId);
        Task<TalepTeklif?> GetTeklifByTalepVeSaticiAsync(int talepId, string saticiId);
        Task<bool> HasAcceptedDemandForProductAsync(string aliciId, int urunId);
        Task AddTeklifAsync(TalepTeklif teklif);
    }
}
