using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces
{
    public interface IUrunRepository : IBaseRepository<Urun>
    {
        Task<Urun> EkleUrunAsync(Urun urun, List<string> resimUrlListesi, List<string> videoUrlListesi);
        Task<Urun?> GetByIdWithRelationsAsync(int id);
        Task<PagedResult<Urun>> QueryAsync(UrunQuery query);
        Task<IReadOnlyCollection<Urun>> GetRecommendedAsync(IReadOnlyCollection<int> kategoriIdleri, IReadOnlyCollection<int> haricUrunIdleri, int take);
    }
}
