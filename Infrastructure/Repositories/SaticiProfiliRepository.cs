using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class SaticiProfiliRepository : BaseRepository<SaticiProfili>, ISaticiProfiliRepository
    {
        public SaticiProfiliRepository(YoremioContext context) : base(context)
        {
        }


        public async Task<SaticiProfili?> GetSaticiWithUserByIdAsync(string kullaniciId)
        {
            return await _dbSet.Include(p => p.Kullanici)
                               .FirstOrDefaultAsync(p => p.KullaniciId == kullaniciId);
        }

        public async Task<SaticiProfili?> GetSaticiPublicDetailsByIdAsync(string kullaniciId)
        {
            return await BuildPublicDetailsQuery()
                .FirstOrDefaultAsync(p => p.KullaniciId == kullaniciId && p.AktifMi);
        }

        public async Task<IReadOnlyCollection<SaticiProfili>> GetOneCikanSaticilarAsync(int take)
        {
            var effectiveTake = take <= 0 ? 6 : Math.Min(take, 24);
            var candidateLimit = effectiveTake * 4;

            return await BuildFeaturedSellersQuery(candidateLimit).ToListAsync();
        }

        internal IQueryable<SaticiProfili> BuildPublicDetailsQuery()
        {
            return ApplySellerStorefrontIncludes(_dbSet.AsNoTracking());
        }

        internal IQueryable<SaticiProfili> BuildFeaturedSellersQuery(int candidateLimit)
        {
            // Tam skorlama öncesi SQL tarafında aday havuzunu sınırlandırarak tüm tablo yüklemesini önlüyoruz.
            return ApplySellerStorefrontIncludes(
                _dbSet
                    .AsNoTracking()
                    .Where(p => p.AktifMi && p.Urunler.Any(u => u.AktifMi))
                    .OrderByDescending(p =>
                        p.AdminDogrulandiMi == true ||
                        (p.AdminDogrulandiMi == null && p.Kullanici != null && p.Kullanici.EmailConfirmed && !string.IsNullOrWhiteSpace(p.VergiNo)))
                    .ThenByDescending(p => p.Urunler.Where(u => u.AktifMi).Sum(u => u.Favoriler.Count + u.Yorumlar.Count(y => y.AktifMi) + u.Puanlar.Count))
                    .ThenByDescending(p => p.Urunler.Count(u => u.AktifMi))
                    .Take(candidateLimit));
        }

        private static IQueryable<SaticiProfili> ApplySellerStorefrontIncludes(IQueryable<SaticiProfili> query)
        {
            return query
                .Include(p => p.Kullanici)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Puanlar)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Yorumlar)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Favoriler)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Resimler)
                .AsSplitQuery();
        }

        // Ekstra metodlar burada yazılabilir
    }
}
