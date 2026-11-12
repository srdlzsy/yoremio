using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPlanliGonderimRepository : IBaseRepository<PlanliGonderim>
    {
        Task<IReadOnlyCollection<PlanliGonderim>> GetUpcomingAsync(string? hedefSehir = null, string? menseiSehir = null, string? saticiId = null, int take = 20);
        Task<PlanliGonderim?> GetByIdWithSellerAsync(int id);
    }
}
