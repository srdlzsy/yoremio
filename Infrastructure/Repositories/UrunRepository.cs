using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class UrunRepository : BaseRepository<Urun>, IUrunRepository
    {
        public UrunRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<Urun> EkleUrunAsync(Urun urun, List<string> resimUrlListesi, List<string> videoUrlListesi)
        {
            urun.Resimler ??= new List<UrunResim>();
            urun.Videolar ??= new List<UrunVideo>();

            foreach (var resimUrl in resimUrlListesi)
            {
                urun.Resimler.Add(new UrunResim
                {
                    Url = resimUrl,
                    UrunId = urun.Id
                });
            }

            foreach (var videoUrl in videoUrlListesi)
            {
                urun.Videolar.Add(new UrunVideo
                {
                    Url = videoUrl,
                    UrunId = urun.Id
                });
            }

            await _context.SaveChangesAsync();

            return urun;
        }

        public async Task<Urun?> GetByIdForUpdateAsync(int id)
        {
            return await _dbSet.Include(u => u.Resimler)
                               .Include(u => u.Videolar)
                               .Include(u => u.Kategori)
                               .Include(u => u.Puanlar)
                               .Include(u => u.Favoriler)
                               .Include(u => u.Satici)
                               .ThenInclude(s => s.Kullanici)
                               .Include(u => u.Yorumlar)
                               .ThenInclude(y => y.YorumYapanKullanici)
                               .AsSplitQuery()
                               .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Urun?> GetByIdAsync(int id)
        {
            return await GetByIdForUpdateAsync(id);
        }

        public override async Task<IEnumerable<Urun>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking()
                               .Include(u => u.Resimler)
                               .Include(u => u.Videolar)
                               .Include(u => u.Kategori)
                               .Include(u => u.Puanlar)
                               .Include(u => u.Favoriler)
                               .Include(u => u.Satici)
                               .ThenInclude(s => s.Kullanici)
                               .Include(u => u.Yorumlar)
                               .ThenInclude(y => y.YorumYapanKullanici)
                               .AsSplitQuery()
                               .ToListAsync();
        }

        public override async Task<IEnumerable<Urun>> FindAsync(Expression<Func<Urun, bool>> predicate)
        {
            return await _dbSet.AsNoTracking()
                               .Include(u => u.Resimler)
                               .Include(u => u.Videolar)
                               .Include(u => u.Kategori)
                               .Include(u => u.Puanlar)
                               .ThenInclude(p => p.Kullanici)
                               .Include(u => u.Favoriler)
                               .Include(u => u.Satici)
                               .ThenInclude(s => s.Kullanici)
                               .Include(u => u.Yorumlar)
                               .ThenInclude(y => y.YorumYapanKullanici)
                               .Where(predicate)
                               .AsSplitQuery()
                               .ToListAsync();
        }

        public async Task<Urun?> GetByIdWithRelationsAsync(int id)
        {
            return await _dbSet.AsNoTracking()
                               .Include(u => u.Videolar)
                               .Include(u => u.Resimler)
                               .Include(u => u.Kategori)
                               .Include(u => u.Puanlar)
                               .ThenInclude(p => p.Kullanici)
                               .Include(u => u.Favoriler)
                               .Include(u => u.Satici)
                               .ThenInclude(s => s.Kullanici)
                               .Include(u => u.Yorumlar)
                               .ThenInclude(y => y.YorumYapanKullanici)
                               .AsSplitQuery()
                               .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<PagedResult<Urun>> QueryAsync(UrunQuery query)
        {
            var minFiyat = query.MinFiyat;
            var maxFiyat = query.MaxFiyat;
            if (minFiyat.HasValue && maxFiyat.HasValue && minFiyat.Value > maxFiyat.Value)
            {
                (minFiyat, maxFiyat) = (maxFiyat, minFiyat);
            }

            var urunler = _dbSet
                .AsNoTracking()
                .Include(u => u.Resimler)
                .Include(u => u.Videolar)
                .Include(u => u.Kategori)
                .Include(u => u.Puanlar)
                .ThenInclude(p => p.Kullanici)
                .Include(u => u.Favoriler)
                .Include(u => u.Satici)
                .ThenInclude(s => s.Kullanici)
                .Include(u => u.Yorumlar)
                .ThenInclude(y => y.YorumYapanKullanici)
                .AsSplitQuery()
                .AsQueryable();

            if (query.SadeceAktif)
            {
                urunler = urunler.Where(u => u.AktifMi);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim().ToLower();
                urunler = urunler.Where(u =>
                    u.Adi.ToLower().Contains(term) ||
                    u.Aciklama.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.SaticiId))
            {
                urunler = urunler.Where(u => u.SaticiId == query.SaticiId);
            }

            if (!string.IsNullOrWhiteSpace(query.Sehir))
            {
                var sehir = query.Sehir.Trim().ToLower();
                urunler = urunler.Where(u => u.Satici.Sehir != null && u.Satici.Sehir.ToLower().Contains(sehir));
            }

            if (!string.IsNullOrWhiteSpace(query.Ilce))
            {
                var ilce = query.Ilce.Trim().ToLower();
                urunler = urunler.Where(u => u.Satici.Ilce != null && u.Satici.Ilce.ToLower().Contains(ilce));
            }

            if (query.KategoriId.HasValue)
            {
                urunler = urunler.Where(u => u.KategoriId == query.KategoriId.Value);
            }

            if (minFiyat.HasValue)
            {
                urunler = urunler.Where(u => u.Fiyat >= minFiyat.Value);
            }

            if (maxFiyat.HasValue)
            {
                urunler = urunler.Where(u => u.Fiyat <= maxFiyat.Value);
            }

            if (query.SadeceStoktaOlanlar)
            {
                urunler = urunler.Where(u => u.StokMiktari > 0);
            }

            if (query.SadeceDogrulanmisSatici)
            {
                urunler = urunler.Where(u =>
                    u.Satici.AdminDogrulandiMi == true ||
                    (u.Satici.AdminDogrulandiMi == null &&
                     u.Satici.Kullanici != null &&
                     u.Satici.Kullanici.EmailConfirmed &&
                     !string.IsNullOrWhiteSpace(u.Satici.VergiNo)));
            }

            if (query.MinOrtalamaPuan.HasValue)
            {
                var minPuan = query.MinOrtalamaPuan.Value;
                urunler = urunler.Where(u => u.Puanlar.Any() && u.Puanlar.Average(p => p.PuanDegeri) >= minPuan);
            }

            urunler = (query.Sort ?? string.Empty).ToLowerInvariant() switch
            {
                "price_asc" => urunler.OrderBy(u => u.Fiyat),
                "price_desc" => urunler.OrderByDescending(u => u.Fiyat),
                "name_asc" => urunler.OrderBy(u => u.Adi),
                "name_desc" => urunler.OrderByDescending(u => u.Adi),
                "top_rated" => urunler.OrderByDescending(u => u.Puanlar.Any() ? u.Puanlar.Average(p => p.PuanDegeri) : 0)
                                      .ThenByDescending(u => u.OlusturmaTarihi),
                "most_reviewed" => urunler.OrderByDescending(u => u.Yorumlar.Count)
                                         .ThenByDescending(u => u.OlusturmaTarihi),
                "most_favorited" => urunler.OrderByDescending(u => u.Favoriler.Count)
                                          .ThenByDescending(u => u.OlusturmaTarihi),
                "newest" => urunler.OrderByDescending(u => u.OlusturmaTarihi),
                "oldest" => urunler.OrderBy(u => u.OlusturmaTarihi),
                _ => urunler.OrderByDescending(u => u.OlusturmaTarihi)
            };

            var totalCount = await urunler.CountAsync();
            var items = await urunler.Skip((query.Page - 1) * query.PageSize)
                                     .Take(query.PageSize)
                                     .ToListAsync();

            return new PagedResult<Urun>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyCollection<Urun>> GetRecommendedAsync(
            IReadOnlyCollection<int> kategoriIdleri,
            IReadOnlyCollection<int> haricUrunIdleri,
            int take,
            string? tercihEdilenSehir = null,
            string? tercihEdilenIlce = null)
        {
            var effectiveTake = take <= 0 ? 12 : take;
            var normalizedSehir = string.IsNullOrWhiteSpace(tercihEdilenSehir) ? null : tercihEdilenSehir.Trim().ToLower();
            var normalizedIlce = string.IsNullOrWhiteSpace(tercihEdilenIlce) ? null : tercihEdilenIlce.Trim().ToLower();

            var query = _dbSet
                .AsNoTracking()
                .Include(u => u.Resimler)
                .Include(u => u.Videolar)
                .Include(u => u.Kategori)
                .Include(u => u.Puanlar)
                .ThenInclude(p => p.Kullanici)
                .Include(u => u.Favoriler)
                .Include(u => u.Satici)
                .ThenInclude(s => s.Kullanici)
                .Include(u => u.Yorumlar)
                .ThenInclude(y => y.YorumYapanKullanici)
                .Where(u => u.AktifMi)
                .AsSplitQuery()
                .AsQueryable();

            if (kategoriIdleri.Count > 0)
            {
                query = query.Where(u => kategoriIdleri.Contains(u.KategoriId));
            }

            if (haricUrunIdleri.Count > 0)
            {
                query = query.Where(u => !haricUrunIdleri.Contains(u.Id));
            }

            var result = await OrderRecommended(query, normalizedSehir, normalizedIlce)
                .Take(effectiveTake)
                .ToListAsync();

            if (result.Count >= effectiveTake)
            {
                return result;
            }

            var eksik = effectiveTake - result.Count;
            var alinanIdler = result.Select(x => x.Id).ToHashSet();
            foreach (var id in haricUrunIdleri)
            {
                alinanIdler.Add(id);
            }

            var fallbackQuery = _dbSet
                .AsNoTracking()
                .Include(u => u.Resimler)
                .Include(u => u.Videolar)
                .Include(u => u.Kategori)
                .Include(u => u.Puanlar)
                .ThenInclude(p => p.Kullanici)
                .Include(u => u.Favoriler)
                .Include(u => u.Satici)
                .ThenInclude(s => s.Kullanici)
                .Include(u => u.Yorumlar)
                .ThenInclude(y => y.YorumYapanKullanici)
                .Where(u => u.AktifMi && !alinanIdler.Contains(u.Id))
                .AsSplitQuery();

            var fallback = await OrderRecommended(fallbackQuery, normalizedSehir, normalizedIlce)
                .Take(eksik)
                .ToListAsync();

            result.AddRange(fallback);
            return result;
        }

        public async Task<IReadOnlyCollection<(string Sehir, string? Ilce)>> GetActiveSellerLocationsAsync()
        {
            var rows = await _dbSet
                .AsNoTracking()
                .Where(u => u.AktifMi && u.Satici != null && u.Satici.Sehir != null && u.Satici.Sehir != string.Empty)
                .Select(u => new { Sehir = u.Satici.Sehir!, u.Satici.Ilce })
                .Distinct()
                .ToListAsync();

            return rows
                .Select(x => (x.Sehir.Trim(), string.IsNullOrWhiteSpace(x.Ilce) ? null : x.Ilce.Trim()))
                .ToList();
        }

        private static IOrderedQueryable<Urun> OrderRecommended(IQueryable<Urun> query, string? normalizedSehir, string? normalizedIlce)
        {
            if (normalizedSehir == null)
            {
                return query
                    .OrderByDescending(u => u.Puanlar.Any() ? u.Puanlar.Average(p => p.PuanDegeri) : 0)
                    .ThenByDescending(u => u.Yorumlar.Count(y => y.AktifMi))
                    .ThenByDescending(u => u.Favoriler.Count)
                    .ThenByDescending(u => u.OlusturmaTarihi);
            }

            return query
                .OrderByDescending(u => normalizedIlce != null && u.Satici.Ilce != null && u.Satici.Ilce.ToLower() == normalizedIlce ? 1 : 0)
                .ThenByDescending(u => u.Satici.Sehir != null && u.Satici.Sehir.ToLower() == normalizedSehir ? 1 : 0)
                .ThenByDescending(u => u.Puanlar.Any() ? u.Puanlar.Average(p => p.PuanDegeri) : 0)
                .ThenByDescending(u => u.Yorumlar.Count(y => y.AktifMi))
                .ThenByDescending(u => u.Favoriler.Count)
                .ThenByDescending(u => u.OlusturmaTarihi);
        }
    }
}
