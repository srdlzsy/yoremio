using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IFiyatAlarmiRepository : IBaseRepository<FiyatAlarmi>
    {
        Task<FiyatAlarmi?> GetAsync(string aliciId, int urunId);
        Task<IReadOnlyCollection<FiyatAlarmi>> GetByAliciWithProductAsync(string aliciId);
        Task<IReadOnlyCollection<FiyatAlarmi>> GetMatchingActiveAlertsAsync(int urunId, decimal yeniFiyat);
    }
}
