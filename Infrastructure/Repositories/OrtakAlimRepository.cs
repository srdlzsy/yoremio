using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class OrtakAlimRepository : BaseRepository<OrtakAlim>, IOrtakAlimRepository
    {
        private readonly YoremioContext _dbContext;

        public OrtakAlimRepository(YoremioContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<OrtakAlim?> GetByIdWithDetailsAsync(int id)
        {
            return await BuildDetailsQuery(asNoTracking: false)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<PagedResult<OrtakAlim>> QueryAsync(string? hedefSehir, string? menseiSehir, string? q, string? durum, int page, int pageSize)
        {
            var effectivePage = page < 1 ? 1 : page;
            var effectivePageSize = pageSize < 1 ? 12 : Math.Min(pageSize, 100);

            var query = BuildFilterQuery(hedefSehir, menseiSehir, q, durum);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync();

            return new PagedResult<OrtakAlim>
            {
                Items = items,
                Page = effectivePage,
                PageSize = effectivePageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyCollection<OrtakAlim>> GetMatchingOpenGroupBuysAsync(string? q, string? hedefSehir, int take = 5)
        {
            var effectiveTake = take <= 0 ? 5 : Math.Min(take, 20);
            return await BuildMatchingOpenGroupBuysQuery(q, hedefSehir)
                .Take(effectiveTake)
                .ToListAsync();
        }

        internal IQueryable<OrtakAlim> BuildFilterQuery(string? hedefSehir, string? menseiSehir, string? q, string? durum)
        {
            var query = BuildDetailsQuery(asNoTracking: true);

            if (!string.IsNullOrWhiteSpace(hedefSehir))
            {
                var normalizedTarget = hedefSehir.Trim().ToLower();
                query = query.Where(o => o.HedefSehir.ToLower().Contains(normalizedTarget));
            }

            if (!string.IsNullOrWhiteSpace(menseiSehir))
            {
                var normalizedOrigin = menseiSehir.Trim().ToLower();
                query = query.Where(o => o.MenseiSehir.ToLower().Contains(normalizedOrigin));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(o =>
                    o.Baslik.ToLower().Contains(term) ||
                    (o.Aciklama != null && o.Aciklama.ToLower().Contains(term)) ||
                    (o.Urun != null && o.Urun.Adi.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                var normalizedDurum = durum.Trim().ToUpperInvariant();
                query = query.Where(o => o.Durum == normalizedDurum);
            }

            return query
                .OrderByDescending(o => o.Durum == OrtakAlimDurumlari.Acik)
                .ThenBy(o => o.SonKatilimTarihi);
        }

        internal IQueryable<OrtakAlim> BuildMatchingOpenGroupBuysQuery(string? q, string? hedefSehir)
        {
            var query = BuildDetailsQuery(asNoTracking: true)
                .Where(o => (o.Durum == OrtakAlimDurumlari.Acik || o.Durum == OrtakAlimDurumlari.HedefTamamlandi)
                            && o.SonKatilimTarihi > DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(hedefSehir))
            {
                var normalizedTarget = hedefSehir.Trim().ToLower();
                query = query.Where(o => o.HedefSehir.ToLower().Contains(normalizedTarget));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(o =>
                    o.Baslik.ToLower().Contains(term) ||
                    o.MenseiSehir.ToLower().Contains(term) ||
                    (o.Urun != null && o.Urun.Adi.ToLower().Contains(term)));
            }

            return query
                .OrderByDescending(o => o.Katilimlar.Count(k => k.AktifMi))
                .ThenBy(o => o.SonKatilimTarihi);
        }

        public async Task<OrtakAlimKatilim?> GetKatilimAsync(int ortakAlimId, string aliciId)
        {
            return await _dbContext.OrtakAlimKatilimlari
                .FirstOrDefaultAsync(k => k.OrtakAlimId == ortakAlimId && k.AliciId == aliciId);
        }

        public async Task AddKatilimAsync(OrtakAlimKatilim katilim)
        {
            await _dbContext.OrtakAlimKatilimlari.AddAsync(katilim);
        }

        internal IQueryable<OrtakAlim> BuildDetailsQuery(bool asNoTracking)
        {
            var query = _dbContext.OrtakAlimlar.AsQueryable();
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query
                .Include(o => o.Satici)
                .ThenInclude(s => s!.Kullanici)
                .Include(o => o.Urun)
                .ThenInclude(u => u!.Resimler)
                .Include(o => o.Kategori)
                .Include(o => o.Katilimlar)
                .AsSplitQuery();
        }
    }
}
