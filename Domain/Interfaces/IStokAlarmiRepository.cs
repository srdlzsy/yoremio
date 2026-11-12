using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IStokAlarmiRepository : IBaseRepository<StokAlarmi>
    {
        Task<StokAlarmi?> GetAsync(string aliciId, int urunId);
        Task<IReadOnlyCollection<StokAlarmi>> GetByAliciWithProductAsync(string aliciId);
        Task<IReadOnlyCollection<StokAlarmi>> GetActiveByProductAsync(int urunId);
    }
}
