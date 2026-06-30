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
                .ThenInclude(urun => urun!.Resimler)
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Satici)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == talepId);
        }

        public async Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId)
        {
            return await _dbContext.Set<Talep>()
                .AsNoTracking()
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Resimler)
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Satici)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .Where(t => t.AliciId == aliciId)
                .OrderByDescending(t => t.OlusturmaTarihi)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId)
        {
            return await _dbContext.Set<Talep>()
                .AsNoTracking()
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Resimler)
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Satici)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .Where(t => t.Urun != null && t.Urun.SaticiId == saticiId)
                .OrderByDescending(t => t.OlusturmaTarihi)
                .AsSplitQuery()
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

        public async Task<bool> HasAcceptedDemandForProductAsync(string aliciId, int urunId)
        {
            return await _dbContext.Set<Talep>()
                .AsNoTracking()
                .AnyAsync(t =>
                    t.AliciId == aliciId &&
                    t.UrunId == urunId &&
                    t.Durum == Domain.Constants.TalepDurumlari.Anlasildi &&
                    t.Teklifler.Any(teklif => teklif.Durum == Domain.Constants.TalepTeklifDurumlari.Kabul));
        }

        public async Task AddTeklifAsync(TalepTeklif teklif)
        {
            await _dbContext.Set<TalepTeklif>().AddAsync(teklif);
        }
    }
}
