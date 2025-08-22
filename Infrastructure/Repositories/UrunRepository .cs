using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UrunRepository : BaseRepository<Urun>, IUrunRepository
    {
        public UrunRepository(YoremioContext context) : base(context) { }

        public async Task<Urun> EkleUrunAsync(Urun urun, List<string> resimUrlListesi, List<string> videoUrlListesi)
        {
            // Null check
            urun.Resimler ??= new List<UrunResim>();
            urun.Videolar ??= new List<UrunVideo>();

            // Resim kayıtlarını ilişkilendir
            foreach (var resimUrl in resimUrlListesi)
            {
                urun.Resimler.Add(new UrunResim
                {
                    Url = resimUrl,
                    UrunId = urun.Id
                });
            }

            // Video kayıtlarını ilişkilendir
            foreach (var videoUrl in videoUrlListesi)
            {
                urun.Videolar.Add(new UrunVideo
                {
                    Url = videoUrl,
                    UrunId = urun.Id
                });
            }

            // urun zaten veritabanında olduğundan AddAsync kaldırıldı
            await _context.SaveChangesAsync();

            return urun;
        }
        public async Task<Urun?> GetByIdAsync(int id)
        {
            return await _dbSet.Include(u => u.Videolar)
                               .Include(u => u.Resimler)
                               .FirstOrDefaultAsync(u => u.Id == id);
        }
        public override async Task<IEnumerable<Urun>> GetAllAsync()
        {
            return await _dbSet.Include(u => u.Resimler)
                               .Include(u => u.Videolar)
                               .ToListAsync();
        }

        public override async Task<IEnumerable<Urun>> FindAsync(Expression<Func<Urun, bool>> predicate)
        {
            return await _dbSet.Include(u => u.Resimler)
                               .Include(u => u.Videolar)
                               .Where(predicate)
                                .AsSplitQuery()  // <-- burada sorgu split edilir
                               .ToListAsync();
        }

        
    }
}
