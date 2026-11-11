using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAliciProfiliRepository : IBaseRepository<AliciProfili>
    {
        Task<AliciProfili?> GetWithUserByIdAsync(string kullaniciId);
    }
}
