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
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Kullanici)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Puanlar)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Yorumlar)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Favoriler)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Resimler)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.KullaniciId == kullaniciId && p.AktifMi);
        }

        public async Task<IReadOnlyCollection<SaticiProfili>> GetOneCikanSaticilarAsync(int take)
        {
            var effectiveTake = take <= 0 ? 6 : Math.Min(take, 24);
            var candidateLimit = effectiveTake * 4;

            // Tam skorlama öncesi SQL tarafında aday havuzunu sınırlandırarak tüm tablo yüklemesini önlüyoruz.
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.AktifMi && p.Urunler.Any(u => u.AktifMi))
                .OrderByDescending(p => p.Kullanici != null && p.Kullanici.EmailConfirmed)
                .ThenByDescending(p => p.Urunler.Where(u => u.AktifMi).Sum(u => u.Favoriler.Count + u.Yorumlar.Count + u.Puanlar.Count))
                .ThenByDescending(p => p.Urunler.Count(u => u.AktifMi))
                .Take(candidateLimit)
                .Include(p => p.Kullanici)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Puanlar)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Yorumlar)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Favoriler)
                .Include(p => p.Urunler.Where(u => u.AktifMi))
                    .ThenInclude(u => u.Resimler)
                .AsSplitQuery()
                .ToListAsync();
        }

        // Ekstra metodlar burada yazılabilir
    }
}
