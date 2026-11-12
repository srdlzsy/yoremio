using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces
{
    public interface IOrtakAlimRepository : IBaseRepository<OrtakAlim>
    {
        Task<OrtakAlim?> GetByIdWithDetailsAsync(int id);
        Task<PagedResult<OrtakAlim>> QueryAsync(string? hedefSehir, string? menseiSehir, string? q, string? durum, int page, int pageSize);
        Task<IReadOnlyCollection<OrtakAlim>> GetMatchingOpenGroupBuysAsync(string? q, string? hedefSehir, int take = 5);
        Task<OrtakAlimKatilim?> GetKatilimAsync(int ortakAlimId, string aliciId);
        Task AddKatilimAsync(OrtakAlimKatilim katilim);
    }
}
