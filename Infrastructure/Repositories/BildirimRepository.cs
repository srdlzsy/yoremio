using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BildirimRepository : BaseRepository<Bildirim>, IBildirimRepository
    {
        private readonly YoremioContext _dbContext;

        public BildirimRepository(YoremioContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<PagedResult<Bildirim>> GetKullaniciBildirimleriAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize)
        {
            var effectivePage = page < 1 ? 1 : page;
            var effectivePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = _dbContext.Bildirimler
                .AsNoTracking()
                .Where(bildirim => bildirim.KullaniciId == kullaniciId);

            if (sadeceOkunmamis)
            {
                query = query.Where(bildirim => bildirim.OkunmaTarihi == null);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(bildirim => bildirim.OlusturmaTarihi)
                .ThenByDescending(bildirim => bildirim.Id)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync();

            return new PagedResult<Bildirim>
            {
                Items = items,
                Page = effectivePage,
                PageSize = effectivePageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> GetOkunmamisSayisiAsync(string kullaniciId)
        {
            return await _dbContext.Bildirimler
                .AsNoTracking()
                .CountAsync(bildirim => bildirim.KullaniciId == kullaniciId && bildirim.OkunmaTarihi == null);
        }

        public async Task<Bildirim?> GetKullaniciBildirimiAsync(long bildirimId, string kullaniciId)
        {
            return await _dbContext.Bildirimler
                .FirstOrDefaultAsync(bildirim => bildirim.Id == bildirimId && bildirim.KullaniciId == kullaniciId);
        }

        public async Task<int> TumunuOkunduIsaretleAsync(string kullaniciId, DateTime okunmaTarihi)
        {
            return await _dbContext.Bildirimler
                .Where(bildirim => bildirim.KullaniciId == kullaniciId && bildirim.OkunmaTarihi == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(bildirim => bildirim.OkunmaTarihi, okunmaTarihi));
        }
    }
}
