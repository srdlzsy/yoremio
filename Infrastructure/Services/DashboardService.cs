using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly YoremioContext _context;
        private readonly ISaticiProfiliService _saticiProfiliService;

        public DashboardService(YoremioContext context, ISaticiProfiliService saticiProfiliService)
        {
            _context = context;
            _saticiProfiliService = saticiProfiliService;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(string userId, IReadOnlyCollection<string> roles)
        {
            var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var summary = new DashboardSummaryDto
            {
                Roles = roles.OrderBy(role => role).ToArray(),
                UnreadMessages = await _context.ChatMessages
                    .AsNoTracking()
                    .CountAsync(message => message.ReceiverId == userId && message.ReadAt == null)
            };

            if (roleSet.Contains(ApplicationRoles.Alici))
            {
                summary.FavoriteProducts = await _context.UrunFavoriler
                    .AsNoTracking()
                    .CountAsync(favorite => favorite.KullaniciId == userId);

                summary.BuyerOpenDemands = await _context.Talepler
                    .AsNoTracking()
                    .CountAsync(demand => demand.AliciId == userId && demand.Durum == TalepDurumlari.Acik);

                summary.BuyerPendingOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer =>
                        offer.Durum == TalepTeklifDurumlari.Beklemede &&
                        offer.Talep != null &&
                        offer.Talep.AliciId == userId);
            }

            if (roleSet.Contains(ApplicationRoles.Satici))
            {
                summary.MyProducts = await _context.Urunler
                    .AsNoTracking()
                    .CountAsync(product => product.SaticiId == userId);

                summary.SellerOpenDemands = await _context.Talepler
                    .AsNoTracking()
                    .CountAsync(demand =>
                        demand.Durum == TalepDurumlari.Acik &&
                        ((demand.Urun != null && demand.Urun.SaticiId == userId) ||
                         demand.Eslesmeler.Any(e => e.SaticiId == userId)));

                summary.SellerPendingOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == userId && offer.Durum == TalepTeklifDurumlari.Beklemede);
            }

            summary.OpenDemands = summary.BuyerOpenDemands + summary.SellerOpenDemands;
            summary.PendingOffers = summary.BuyerPendingOffers + summary.SellerPendingOffers;

            return summary;
        }

        public async Task<SaticiDashboardDto> GetSaticiDashboardAsync(string saticiId)
        {
            var products = _context.Urunler
                .AsNoTracking()
                .Where(product => product.SaticiId == saticiId);

            var ratingValues = _context.Puanlar
                .AsNoTracking()
                .Where(rating => rating.Urun.SaticiId == saticiId)
                .Select(rating => rating.PuanDegeri);

            var trustScore = await _saticiProfiliService.GetGuvenSkoruAsync(saticiId);

            var productStats = await products
                .Select(p => new
                {
                    p.Id,
                    p.Adi,
                    p.AktifMi,
                    p.StokMiktari,
                    p.GoruntulenmeSayisi
                })
                .ToListAsync();

            var activeProducts = productStats.Count(p => p.AktifMi);
            var outOfStockProducts = productStats.Count(p => p.AktifMi && p.StokMiktari <= 0);
            var lowStockProducts = productStats.Count(p => p.AktifMi && p.StokMiktari > 0 && p.StokMiktari <= 5);

            var sellerDemands = await _context.Talepler
                .AsNoTracking()
                .Where(demand =>
                    (demand.Urun != null && demand.Urun.SaticiId == saticiId) ||
                    demand.Eslesmeler.Any(e => e.SaticiId == saticiId) ||
                    demand.Teklifler.Any(t => t.SaticiId == saticiId))
                .Select(d => new
                {
                    d.Id,
                    d.UrunId,
                    d.Durum,
                    d.TalepTuru,
                    d.OlusturmaTarihi,
                    d.AliciGerceklestiOnayi,
                    d.SaticiGerceklestiOnayi,
                    HasSellerOffer = d.Teklifler.Any(t => t.SaticiId == saticiId),
                    IsAgreedWithSeller = d.Durum == TalepDurumlari.Anlasildi &&
                        ((d.Urun != null && d.Urun.SaticiId == saticiId) ||
                         d.Teklifler.Any(t => t.SaticiId == saticiId && t.Durum == TalepTeklifDurumlari.Kabul)),
                    SellerOfferCreatedAt = d.Teklifler
                        .Where(t => t.SaticiId == saticiId)
                        .Select(t => (DateTime?)t.OlusturmaTarihi)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var openDemands = sellerDemands.Count(d => d.Durum == TalepDurumlari.Acik);
            var unansweredDemands = sellerDemands.Count(d => d.Durum == TalepDurumlari.Acik && !d.HasSellerOffer);
            var matchedOpenMarketDemands = sellerDemands.Count(d =>
                d.Durum == TalepDurumlari.Acik &&
                d.TalepTuru == TalepTurleri.AcikPazar);

            var agreedDemands = sellerDemands.Count(d => d.IsAgreedWithSeller);
            var verifiedCompletedDeals = sellerDemands.Count(d =>
                d.IsAgreedWithSeller &&
                (d.AliciGerceklestiOnayi == true || d.SaticiGerceklestiOnayi == true));

            var responseDurations = sellerDemands
                .Where(d => d.SellerOfferCreatedAt.HasValue && d.SellerOfferCreatedAt.Value >= d.OlusturmaTarihi)
                .Select(d => (d.SellerOfferCreatedAt!.Value - d.OlusturmaTarihi).TotalMinutes)
                .ToList();

            var avgResponseMinutes = responseDurations.Count == 0
                ? 0
                : Math.Round(responseDurations.Average(), 1);

            var demandCountsByProduct = sellerDemands
                .Where(d => d.UrunId.HasValue)
                .GroupBy(d => d.UrunId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var topRequestedProducts = productStats
                .Where(p => p.AktifMi)
                .Select(p => new UrunTalepOzetDto
                {
                    UrunId = p.Id,
                    UrunAdi = p.Adi,
                    TalepSayisi = demandCountsByProduct.GetValueOrDefault(p.Id, 0),
                    GoruntulenmeSayisi = p.GoruntulenmeSayisi,
                    StokMiktari = p.StokMiktari
                })
                .OrderByDescending(x => x.TalepSayisi)
                .ThenByDescending(x => x.GoruntulenmeSayisi)
                .Take(3)
                .ToList();

            var actionSuggestions = BuildSellerSuggestions(
                unansweredDemands,
                lowStockProducts,
                outOfStockProducts,
                matchedOpenMarketDemands,
                topRequestedProducts);

            return new SaticiDashboardDto
            {
                TotalProducts = productStats.Count,
                ActiveProducts = activeProducts,
                InactiveProducts = productStats.Count - activeProducts,
                OutOfStockProducts = outOfStockProducts,
                LowStockProducts = lowStockProducts,
                TotalProductViews = productStats.Sum(p => p.GoruntulenmeSayisi),
                TotalFavorites = await _context.UrunFavoriler
                    .AsNoTracking()
                    .CountAsync(favorite => favorite.Urun != null && favorite.Urun.SaticiId == saticiId),
                FollowerCount = await _context.SaticiTakipleri
                    .AsNoTracking()
                    .CountAsync(f => f.SaticiId == saticiId),
                TotalReviews = await _context.Yorumlar
                    .AsNoTracking()
                    .CountAsync(review => review.AktifMi && review.Urun != null && review.Urun.SaticiId == saticiId),
                TotalRatings = await ratingValues.CountAsync(),
                AverageRating = await ratingValues.AnyAsync()
                    ? Math.Round(await ratingValues.AverageAsync(), 1)
                    : 0,
                TrustScore = trustScore?.GuvenSkoru ?? 0,
                OpenDemands = openDemands,
                UnansweredDemands = unansweredDemands,
                MatchedOpenMarketDemands = matchedOpenMarketDemands,
                AgreedDemands = agreedDemands,
                VerifiedCompletedDeals = verifiedCompletedDeals,
                PendingOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == saticiId && offer.Durum == TalepTeklifDurumlari.Beklemede),
                AcceptedOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == saticiId && offer.Durum == TalepTeklifDurumlari.Kabul),
                RejectedOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == saticiId && offer.Durum == TalepTeklifDurumlari.Red),
                AverageResponseMinutes = avgResponseMinutes,
                UnreadMessages = await _context.ChatMessages
                    .AsNoTracking()
                    .CountAsync(message => message.ReceiverId == saticiId && message.ReadAt == null),
                TopRequestedProducts = topRequestedProducts,
                ActionSuggestions = actionSuggestions
            };
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

            var demandMetrics = await _context.Talepler
                .AsNoTracking()
                .Select(d => new
                {
                    d.Id,
                    d.AliciId,
                    d.TalepTuru,
                    d.Durum,
                    d.OlusturmaTarihi,
                    d.AliciGerceklestiOnayi,
                    d.SaticiGerceklestiOnayi,
                    HasAnyOffer = d.Teklifler.Any(),
                    FirstOfferAt = d.Teklifler
                        .OrderBy(t => t.OlusturmaTarihi)
                        .Select(t => (DateTime?)t.OlusturmaTarihi)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var totalDemands = demandMetrics.Count;
            var demandsWithOffers = demandMetrics.Count(d => d.HasAnyOffer);
            var agreedDemands = demandMetrics.Count(d => d.Durum == TalepDurumlari.Anlasildi);
            var verifiedCompletedDeals = demandMetrics.Count(d =>
                d.Durum == TalepDurumlari.Anlasildi &&
                (d.AliciGerceklestiOnayi == true || d.SaticiGerceklestiOnayi == true));

            var responseDurations = demandMetrics
                .Where(d => d.FirstOfferAt.HasValue && d.FirstOfferAt.Value >= d.OlusturmaTarihi)
                .Select(d => (d.FirstOfferAt!.Value - d.OlusturmaTarihi).TotalMinutes)
                .ToList();

            var recentBuyers = demandMetrics
                .Where(d => d.OlusturmaTarihi >= sixtyDaysAgo)
                .GroupBy(d => d.AliciId)
                .ToList();
            var repeatBuyers = recentBuyers.Count(g => g.Count() >= 2);

            return new AdminDashboardDto
            {
                TotalUsers = await _context.Users.AsNoTracking().CountAsync(),
                TotalSellers = await _context.SaticiProfilleri.AsNoTracking().CountAsync(),
                ActiveSellers = await _context.SaticiProfilleri.AsNoTracking().CountAsync(seller => seller.AktifMi),
                TotalBuyers = await _context.AliciProfilleri.AsNoTracking().CountAsync(),
                TotalProducts = await _context.Urunler.AsNoTracking().CountAsync(),
                ActiveProducts = await _context.Urunler.AsNoTracking().CountAsync(product => product.AktifMi),
                InactiveProducts = await _context.Urunler.AsNoTracking().CountAsync(product => !product.AktifMi),
                TotalDemands = totalDemands,
                OpenDemands = demandMetrics.Count(d => d.Durum == TalepDurumlari.Acik),
                OpenMarketDemands = demandMetrics.Count(d => d.TalepTuru == TalepTurleri.AcikPazar),
                AgreedDemands = agreedDemands,
                VerifiedCompletedDeals = verifiedCompletedDeals,
                OfferConversionRatePercent = totalDemands == 0
                    ? 0
                    : Math.Round((double)demandsWithOffers / totalDemands * 100, 1),
                DealAgreementRatePercent = totalDemands == 0
                    ? 0
                    : Math.Round((double)agreedDemands / totalDemands * 100, 1),
                AverageSellerResponseMinutes = responseDurations.Count == 0
                    ? 0
                    : Math.Round(responseDurations.Average(), 1),
                RepeatBuyers60Days = repeatBuyers,
                RepeatBuyerRatePercent = recentBuyers.Count == 0
                    ? 0
                    : Math.Round((double)repeatBuyers / recentBuyers.Count * 100, 1),
                ActivePriceAlerts = await _context.FiyatAlarmlari.AsNoTracking().CountAsync(a => a.AktifMi),
                TotalSellerFollows = await _context.SaticiTakipleri.AsNoTracking().CountAsync(),
                TotalReviews = await _context.Yorumlar.AsNoTracking().CountAsync(y => y.AktifMi),
                TotalMessages = await _context.ChatMessages.AsNoTracking().CountAsync(),
                UnreadMessages = await _context.ChatMessages.AsNoTracking().CountAsync(message => message.ReadAt == null)
            };
        }

        private static List<string> BuildSellerSuggestions(
            int unansweredDemands,
            int lowStockProducts,
            int outOfStockProducts,
            int matchedOpenMarketDemands,
            IReadOnlyCollection<UrunTalepOzetDto> topRequestedProducts)
        {
            var suggestions = new List<string>();

            if (unansweredDemands > 0)
            {
                suggestions.Add($"{unansweredDemands} acik talebe henuz teklif gondermediniz; hizli yanit anlasma oranini artirir.");
            }

            if (matchedOpenMarketDemands > 0)
            {
                suggestions.Add($"Kategoriniz ve bolgenizle eslesen {matchedOpenMarketDemands} acik pazar talebi bulunuyor.");
            }

            if (outOfStockProducts > 0)
            {
                suggestions.Add($"{outOfStockProducts} aktif urununuzde stok tukenmis gorunuyor; toplu stok guncellemesi yapabilirsiniz.");
            }
            else if (lowStockProducts > 0)
            {
                suggestions.Add($"{lowStockProducts} urununuzde stok 5 veya altina dustu.");
            }

            var topProduct = topRequestedProducts.FirstOrDefault(p => p.TalepSayisi > 0);
            if (topProduct != null)
            {
                suggestions.Add($"En cok talep alan urununuz: {topProduct.UrunAdi} ({topProduct.TalepSayisi} talep).");
            }

            return suggestions;
        }
    }
}
