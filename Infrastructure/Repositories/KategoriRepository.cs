using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class KategoriRepository : BaseRepository<Kategori>, IKategoriRepository
    {
        public KategoriRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var normalized = name.Trim().ToLower();
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(k => k.Adi.ToLower() == normalized && (!excludeId.HasValue || k.Id != excludeId.Value));
        }

        public async Task<bool> HasProductsAsync(int kategoriId)
        {
            return await _context.Set<Urun>()
                .AsNoTracking()
                .AnyAsync(u => u.KategoriId == kategoriId);
        }

        public async Task<IReadOnlyDictionary<int, int>> GetActiveProductCountsAsync()
        {
            return await _context.Set<Urun>()
                .AsNoTracking()
                .Where(u => u.AktifMi)
                .GroupBy(u => u.KategoriId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }
    }
}
