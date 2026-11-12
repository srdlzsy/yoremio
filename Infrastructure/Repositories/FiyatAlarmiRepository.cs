using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class FiyatAlarmiRepository : BaseRepository<FiyatAlarmi>, IFiyatAlarmiRepository
    {
        public FiyatAlarmiRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<FiyatAlarmi?> GetAsync(string aliciId, int urunId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.AliciId == aliciId && x.UrunId == urunId);
        }

        public async Task<IReadOnlyCollection<FiyatAlarmi>> GetByAliciWithProductAsync(string aliciId)
        {
            return await BuildByAliciWithProductQuery(aliciId).ToListAsync();
        }

        internal IQueryable<FiyatAlarmi> BuildByAliciWithProductQuery(string aliciId)
        {
            return _dbSet
                .AsNoTracking()
                .Include(x => x.Urun)
                .ThenInclude(u => u!.Resimler)
                .Where(x => x.AliciId == aliciId)
                .OrderByDescending(x => x.OlusturmaTarihi);
        }

        public async Task<IReadOnlyCollection<FiyatAlarmi>> GetMatchingActiveAlertsAsync(int urunId, decimal yeniFiyat)
        {
            return await _dbSet
                .Where(x =>
                    x.UrunId == urunId &&
                    x.AktifMi &&
                    yeniFiyat <= x.HedefFiyat &&
                    (x.SonTetiklenenFiyat == null || yeniFiyat < x.SonTetiklenenFiyat))
                .ToListAsync();
        }
    }
}
