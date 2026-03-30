using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TalepRepository : BaseRepository<Talep>, ITalepRepository
    {
        private readonly YoremioContext _dbContext;

        public TalepRepository(YoremioContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<Talep?> GetByIdWithDetailsAsync(int talepId)
        {
            return await _dbContext.Set<Talep>()
                .Include(t => t.Urun)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .FirstOrDefaultAsync(t => t.Id == talepId);
        }

        public async Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId)
        {
            return await _dbContext.Set<Talep>()
                .AsNoTracking()
                .Include(t => t.Urun)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .Where(t => t.AliciId == aliciId)
                .OrderByDescending(t => t.OlusturmaTarihi)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId)
        {
            return await _dbContext.Set<Talep>()
                .AsNoTracking()
                .Include(t => t.Urun)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .Where(t => t.Urun != null && t.Urun.SaticiId == saticiId)
                .OrderByDescending(t => t.OlusturmaTarihi)
                .ToListAsync();
        }

        public async Task<TalepTeklif?> GetTeklifByIdWithDetailsAsync(int teklifId)
        {
            return await _dbContext.Set<TalepTeklif>()
                .Include(t => t.Talep)
                .ThenInclude(talep => talep!.Urun)
                .Include(t => t.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .FirstOrDefaultAsync(t => t.Id == teklifId);
        }

        public async Task<TalepTeklif?> GetTeklifByTalepVeSaticiAsync(int talepId, string saticiId)
        {
            return await _dbContext.Set<TalepTeklif>()
                .FirstOrDefaultAsync(t => t.TalepId == talepId && t.SaticiId == saticiId);
        }

        public async Task AddTeklifAsync(TalepTeklif teklif)
        {
            await _dbContext.Set<TalepTeklif>().AddAsync(teklif);
        }
    }
}
