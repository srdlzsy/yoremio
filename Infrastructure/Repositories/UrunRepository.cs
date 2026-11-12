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

        public async Task IncrementViewCountAsync(int id)
        {
            await _dbSet
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.GoruntulenmeSayisi, u => u.GoruntulenmeSayisi + 1));
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
                .Include(u => u.Kategori)
                .Include(u => u.Puanlar)
                .Include(u => u.Favoriler)
                .Include(u => u.Satici)
                .ThenInclude(s => s.Kullanici)
                .Include(u => u.Yorumlar.Where(y => y.AktifMi))
                .AsSplitQuery()
                .AsQueryable();

            if (query.SadeceAktif)
            {
                urunler = urunler.Where(u => u.AktifMi);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var rawTokens = query.Q.Trim().ToLower()
                    .Split(new[] { ' ', ',', ';', '.', '-', '/', '+' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length >= 2)
                    .Distinct()
                    .ToArray();

                if (rawTokens.Length == 0)
                {
                    var term = query.Q.Trim().ToLower();
                    var normTerm = SearchNormalizer.NormalizeTurkish(term);
                    urunler = urunler.Where(u =>
                        u.Adi.ToLower().Contains(term) || u.Adi.ToLower().Contains(normTerm) ||
                        u.Aciklama.ToLower().Contains(term) || u.Aciklama.ToLower().Contains(normTerm));
                }
                else
                {
                    foreach (var rawTok in rawTokens)
                    {
                        var normTok = SearchNormalizer.NormalizeTurkish(rawTok);
                        var stemTok = normTok.Length >= 5 && (normTok.EndsWith("i") || normTok.EndsWith("u") || normTok.EndsWith("a") || normTok.EndsWith("e"))
                            ? normTok[..^1]
                            : normTok;
                        var rawStemTok = rawTok.Length >= 5 && (rawTok.EndsWith("i") || rawTok.EndsWith("ı") || rawTok.EndsWith("u") || rawTok.EndsWith("ü") || rawTok.EndsWith("a") || rawTok.EndsWith("e"))
                            ? rawTok[..^1]
                            : rawTok;

                        urunler = urunler.Where(u =>
                            u.Adi.ToLower().Contains(rawTok) || u.Adi.ToLower().Contains(normTok) || u.Adi.ToLower().Contains(stemTok) || u.Adi.ToLower().Contains(rawStemTok) ||
                            u.Aciklama.ToLower().Contains(rawTok) || u.Aciklama.ToLower().Contains(normTok) || u.Aciklama.ToLower().Contains(stemTok) ||
                            (u.UretimSehir != null && (u.UretimSehir.ToLower().Contains(rawTok) || u.UretimSehir.ToLower().Contains(normTok))) ||
                            (u.UretimIlce != null && (u.UretimIlce.ToLower().Contains(rawTok) || u.UretimIlce.ToLower().Contains(normTok))) ||
                            (u.Satici.MagazaAdi != null && (u.Satici.MagazaAdi.ToLower().Contains(rawTok) || u.Satici.MagazaAdi.ToLower().Contains(normTok))));
                    }
                }
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

            if (!string.IsNullOrWhiteSpace(query.UretimSehir))
            {
                var uretimSehir = query.UretimSehir.Trim().ToLower();
                urunler = urunler.Where(u =>
                    (u.UretimSehir != null && u.UretimSehir.ToLower().Contains(uretimSehir)) ||
                    (u.UretimSehir == null && u.Satici.Sehir != null && u.Satici.Sehir.ToLower().Contains(uretimSehir)));
            }

            string? normalizedDeliveryCity = string.IsNullOrWhiteSpace(query.TeslimatSehir)
                ? null
                : query.TeslimatSehir.Trim().ToLower();

            if (normalizedDeliveryCity != null)
            {
                urunler = urunler.Where(u =>
                    (u.Satici.Sehir != null && u.Satici.Sehir.ToLower().Contains(normalizedDeliveryCity)) ||
                    (u.KargoVarMi &&
                     (string.IsNullOrWhiteSpace(u.GonderimSehirleri) ||
                      u.GonderimSehirleri.ToLower().Contains("tüm türkiye") ||
                      u.GonderimSehirleri.ToLower().Contains("tum turkiye") ||
                      u.GonderimSehirleri.ToLower().Contains(normalizedDeliveryCity))));
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

            if (query.SadeceYeniHasat)
            {
                urunler = urunler.Where(u => u.YeniHasatMi);
            }

            if (query.SadeceEldenTeslim)
            {
                urunler = urunler.Where(u => u.EldenTeslimVarMi);
            }

            if (query.SadeceKargolu)
            {
                urunler = urunler.Where(u => u.KargoVarMi);
            }

            if (query.SadeceDogrulanmisSatici)
            {
                urunler = urunler.Where(u =>
                    u.Satici.AdminDogrulandiMi == true ||
                    (u.Satici.AdminDogrulandiMi == null &&
                     u.Satici.Kullanici != null &&
                     u.Satici.Kullanici.EmailConfirmed &&
                     !string.IsNullOrWhiteSpace(u.Satici.VergiNo) &&
                     u.Satici.IsletmeDogrulandiMi &&
                     u.Satici.UreticiDogrulandiMi));
            }

            if (query.MinOrtalamaPuan.HasValue)
            {
                var minPuan = query.MinOrtalamaPuan.Value;
                urunler = urunler.Where(u => u.Puanlar.Any() && u.Puanlar.Average(p => p.PuanDegeri) >= minPuan);
            }

            urunler = (query.Sort ?? string.Empty).ToLowerInvariant() switch
            {
                "relevance" when normalizedDeliveryCity != null => urunler
                    .OrderByDescending(u => u.Satici.Sehir != null && u.Satici.Sehir.ToLower().Contains(normalizedDeliveryCity) ? 1 : 0)
                    .ThenByDescending(u => u.EldenTeslimVarMi && u.Satici.Sehir != null && u.Satici.Sehir.ToLower().Contains(normalizedDeliveryCity) ? 1 : 0)
                    .ThenByDescending(u => u.GonderimSehirleri != null && u.GonderimSehirleri.ToLower().Contains(normalizedDeliveryCity) ? 1 : 0)
                    .ThenByDescending(u => u.StokMiktari > 0 ? 1 : 0)
                    .ThenByDescending(u => u.Puanlar.Any() ? u.Puanlar.Average(p => p.PuanDegeri) : 0)
                    .ThenByDescending(u => u.OlusturmaTarihi),
                "relevance" => urunler
                    .OrderByDescending(u => u.StokMiktari > 0 ? 1 : 0)
                    .ThenByDescending(u => u.YeniHasatMi ? 1 : 0)
                    .ThenByDescending(u => u.Puanlar.Any() ? u.Puanlar.Average(p => p.PuanDegeri) : 0)
                    .ThenByDescending(u => u.OlusturmaTarihi),
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
                "most_viewed" => urunler.OrderByDescending(u => u.GoruntulenmeSayisi)
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
