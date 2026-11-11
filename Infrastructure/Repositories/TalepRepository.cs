using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
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
            return await BuildDetailsQuery(asNoTracking: false)
                .FirstOrDefaultAsync(t => t.Id == talepId);
        }

        public async Task<IReadOnlyCollection<Talep>> GetAliciTalepleriAsync(string aliciId, string? durum = null)
        {
            return await FilterByDurum(
                    BuildDetailsQuery(asNoTracking: true).Where(t => t.AliciId == aliciId),
                    durum)
                .OrderByDescending(t => t.OlusturmaTarihi)
                .ToListAsync();
        }

        public async Task<PagedResult<Talep>> GetAliciTalepleriPagedAsync(string aliciId, string? durum, int page, int pageSize)
        {
            var effectivePage = page < 1 ? 1 : page;
            var effectivePageSize = pageSize < 1 ? 12 : Math.Min(pageSize, 100);

            var query = FilterByDurum(
                    BuildDetailsQuery(asNoTracking: true).Where(t => t.AliciId == aliciId),
                    durum)
                .OrderByDescending(t => t.OlusturmaTarihi);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync();

            return new PagedResult<Talep>
            {
                Items = items,
                Page = effectivePage,
                PageSize = effectivePageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyCollection<Talep>> GetSaticiTalepleriAsync(string saticiId, string? durum = null)
        {
            return await FilterByDurum(
                    BuildDetailsQuery(asNoTracking: true).Where(t => t.Urun != null && t.Urun.SaticiId == saticiId),
                    durum)
                .OrderByDescending(t => t.OlusturmaTarihi)
                .ToListAsync();
        }

        public async Task<PagedResult<Talep>> GetSaticiTalepleriPagedAsync(string saticiId, string? durum, int page, int pageSize)
        {
            var effectivePage = page < 1 ? 1 : page;
            var effectivePageSize = pageSize < 1 ? 12 : Math.Min(pageSize, 100);

            var query = FilterByDurum(
                    BuildDetailsQuery(asNoTracking: true).Where(t => t.Urun != null && t.Urun.SaticiId == saticiId),
                    durum)
                .OrderByDescending(t => t.OlusturmaTarihi);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync();

            return new PagedResult<Talep>
            {
                Items = items,
                Page = effectivePage,
                PageSize = effectivePageSize,
                TotalCount = totalCount
            };
        }

        public async Task<TalepTeklif?> GetTeklifByIdWithDetailsAsync(int teklifId)
        {
            return await _dbContext.Set<TalepTeklif>()
                .Include(t => t.Talep)
                .ThenInclude(talep => talep!.Urun)
                .Include(t => t.Talep)
                .ThenInclude(talep => talep!.Alici)
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

        private IQueryable<Talep> BuildDetailsQuery(bool asNoTracking)
        {
            var query = _dbContext.Set<Talep>().AsQueryable();
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query
                .Include(t => t.Alici)
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Resimler)
                .Include(t => t.Urun)
                .ThenInclude(urun => urun!.Satici)
                .Include(t => t.Teklifler)
                .ThenInclude(teklif => teklif.Satici)
                .ThenInclude(user => user!.SaticiProfili)
                .AsSplitQuery();
        }

        private static IQueryable<Talep> FilterByDurum(IQueryable<Talep> query, string? durum)
        {
            if (string.IsNullOrWhiteSpace(durum))
            {
                return query;
            }

            var normalized = durum.Trim().ToUpperInvariant();
            return query.Where(t => t.Durum == normalized);
        }
    }
}
