using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SaticiTakipRepository : BaseRepository<SaticiTakip>, ISaticiTakipRepository
    {
        public SaticiTakipRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<SaticiTakip?> GetAsync(string aliciId, string saticiId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.AliciId == aliciId && x.SaticiId == saticiId);
        }

        public async Task<IReadOnlyCollection<SaticiTakip>> GetByAliciAsync(string aliciId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Satici)
                .ThenInclude(s => s!.Kullanici)
                .Include(x => x.Satici)
                .ThenInclude(s => s!.Urunler)
                .ThenInclude(u => u.Resimler)
                .Include(x => x.Satici)
                .ThenInclude(s => s!.Urunler)
                .ThenInclude(u => u.Puanlar)
                .Include(x => x.Satici)
                .ThenInclude(s => s!.Urunler)
                .ThenInclude(u => u.Yorumlar)
                .Include(x => x.Satici)
                .ThenInclude(s => s!.Urunler)
                .ThenInclude(u => u.Favoriler)
                .AsSplitQuery()
                .Where(x => x.AliciId == aliciId)
                .OrderByDescending(x => x.OlusturmaTarihi)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<SaticiTakip>> GetFollowersWithNotificationAsync(string saticiId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.SaticiId == saticiId && x.BildirimAcikMi)
                .ToListAsync();
        }

        public async Task<int> GetFollowerCountAsync(string saticiId)
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(x => x.SaticiId == saticiId);
        }
    }
}
