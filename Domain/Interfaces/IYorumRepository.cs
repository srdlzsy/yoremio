using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IYorumRepository : IBaseRepository<Yorum>
    {
        Task<IEnumerable<Yorum>> GetYorumlarByUrunIdAsync(int urunId);
        Task<Yorum?> GetByIdWithUserAsync(int yorumId);
    }
}
