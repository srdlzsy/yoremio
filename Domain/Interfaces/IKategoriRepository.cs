using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IKategoriRepository : IBaseRepository<Kategori>
    {
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasProductsAsync(int kategoriId);
        Task<IReadOnlyDictionary<int, int>> GetActiveProductCountsAsync();
    }
}
