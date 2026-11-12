using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PlanliGonderimRepository : BaseRepository<PlanliGonderim>, IPlanliGonderimRepository
    {
        public PlanliGonderimRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<PlanliGonderim?> GetByIdWithSellerAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Satici)
                    .ThenInclude(s => s!.Kullanici)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IReadOnlyCollection<PlanliGonderim>> GetUpcomingAsync(string? hedefSehir = null, string? menseiSehir = null, string? saticiId = null, int take = 20)
        {
            var effectiveTake = take <= 0 ? 20 : Math.Min(take, 100);
            var now = DateTime.UtcNow;

            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.Satici)
                    .ThenInclude(s => s!.Kullanici)
                .Where(p => p.AktifMi && p.GonderimTarihi >= now.AddDays(-1))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(hedefSehir))
            {
                var term = hedefSehir.Trim().ToLower();
                query = query.Where(p => p.HedefSehir.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(menseiSehir))
            {
                var term = menseiSehir.Trim().ToLower();
                query = query.Where(p => p.MenseiSehir.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(saticiId))
            {
                query = query.Where(p => p.SaticiId == saticiId.Trim());
            }

            return await query
                .OrderBy(p => p.GonderimTarihi)
                .Take(effectiveTake)
                .ToListAsync();
        }
    }
}
