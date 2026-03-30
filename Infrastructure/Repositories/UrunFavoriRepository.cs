using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UrunFavoriRepository : BaseRepository<UrunFavori>, IUrunFavoriRepository
    {
        public UrunFavoriRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<UrunFavori?> GetByKullaniciVeUrunAsync(string kullaniciId, int urunId)
        {
            return await _context.Set<UrunFavori>()
                .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId && x.UrunId == urunId);
        }

        public async Task<IReadOnlyCollection<Urun>> GetFavoriUrunlerAsync(string kullaniciId)
        {
            var urunIdleri = await _context.Set<UrunFavori>()
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId && x.Urun != null && x.Urun.AktifMi)
                .OrderByDescending(x => x.EklenmeTarihi)
                .Select(x => x.UrunId)
                .ToListAsync();

            return await _context.Set<Urun>()
                .AsNoTracking()
                .Where(u => urunIdleri.Contains(u.Id))
                .Include(u => u.Resimler)
                .Include(u => u.Videolar)
                .Include(u => u.Puanlar)
                .Include(u => u.Yorumlar)
                .Include(u => u.Favoriler)
                .ToListAsync();
        }
        public async Task<IReadOnlyCollection<int>> GetFavoriUrunIdleriAsync(string kullaniciId)
        {
            return await _context.Set<UrunFavori>()
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId)
                .Select(x => x.UrunId)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<int>> GetKullaniciFavoriKategoriIdleriAsync(string kullaniciId, int take)
        {
            return await _context.Set<UrunFavori>()
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId && x.Urun != null && x.Urun.AktifMi)
                .Include(x => x.Urun)
                .GroupBy(x => x.Urun!.KategoriId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(take)
                .ToListAsync();
        }
    }
}
