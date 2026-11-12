using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISaticiTakipRepository : IBaseRepository<SaticiTakip>
    {
        Task<SaticiTakip?> GetAsync(string aliciId, string saticiId);
        Task<IReadOnlyCollection<SaticiTakip>> GetByAliciAsync(string aliciId);
        Task<IReadOnlyCollection<SaticiTakip>> GetFollowersWithNotificationAsync(string saticiId);
        Task<int> GetFollowerCountAsync(string saticiId);
    }
}
