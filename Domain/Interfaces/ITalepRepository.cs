using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITalepRepository : IBaseRepository<Talep>
    {
        Task<Talep?> GetByIdWithDetailsAsync(int talepId);
        Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId);
        Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId);
        Task<TalepTeklif?> GetTeklifByIdWithDetailsAsync(int teklifId);
        Task<TalepTeklif?> GetTeklifByTalepVeSaticiAsync(int talepId, string saticiId);
        Task AddTeklifAsync(TalepTeklif teklif);
    }
}
