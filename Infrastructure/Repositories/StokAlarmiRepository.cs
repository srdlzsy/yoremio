using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class StokAlarmiRepository : BaseRepository<StokAlarmi>, IStokAlarmiRepository
    {
        public StokAlarmiRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<StokAlarmi?> GetAsync(string aliciId, int urunId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.AliciId == aliciId && x.UrunId == urunId);
        }

        public async Task<IReadOnlyCollection<StokAlarmi>> GetByAliciWithProductAsync(string aliciId)
        {
            return await BuildByAliciWithProductQuery(aliciId).ToListAsync();
        }

        internal IQueryable<StokAlarmi> BuildByAliciWithProductQuery(string aliciId)
        {
            return _dbSet
                .AsNoTracking()
                .Include(x => x.Urun)
                .ThenInclude(u => u!.Resimler)
                .Where(x => x.AliciId == aliciId)
                .OrderByDescending(x => x.OlusturmaTarihi);
        }

        public async Task<IReadOnlyCollection<StokAlarmi>> GetActiveByProductAsync(int urunId)
        {
            return await _dbSet
                .Where(x => x.UrunId == urunId && x.AktifMi)
                .ToListAsync();
        }
    }
}
