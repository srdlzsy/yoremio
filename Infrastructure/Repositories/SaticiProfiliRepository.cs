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

        public async Task<IReadOnlyCollection<SaticiProfili>> GetOneCikanSaticilarAsync(int take)
        {
            var effectiveTake = take <= 0 ? 6 : Math.Min(take, 24);

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
                .Where(p => p.AktifMi && p.Urunler.Any(u => u.AktifMi))
                .AsSplitQuery()
                .ToListAsync();
        }

        // Ekstra metodlar burada yazılabilir
    }
}
