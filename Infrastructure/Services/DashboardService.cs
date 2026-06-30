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
                        demand.Urun != null &&
                        demand.Urun.SaticiId == userId);

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

            return new SaticiDashboardDto
            {
                TotalProducts = await products.CountAsync(),
                ActiveProducts = await products.CountAsync(product => product.AktifMi),
                InactiveProducts = await products.CountAsync(product => !product.AktifMi),
                OutOfStockProducts = await products.CountAsync(product => product.AktifMi && product.StokMiktari <= 0),
                TotalFavorites = await _context.UrunFavoriler
                    .AsNoTracking()
                    .CountAsync(favorite => favorite.Urun != null && favorite.Urun.SaticiId == saticiId),
                TotalReviews = await _context.Yorumlar
                    .AsNoTracking()
                    .CountAsync(review => review.Urun != null && review.Urun.SaticiId == saticiId),
                TotalRatings = await ratingValues.CountAsync(),
                AverageRating = await ratingValues.AnyAsync()
                    ? Math.Round(await ratingValues.AverageAsync(), 1)
                    : 0,
                TrustScore = trustScore?.GuvenSkoru ?? 0,
                OpenDemands = await _context.Talepler
                    .AsNoTracking()
                    .CountAsync(demand =>
                        demand.Durum == TalepDurumlari.Acik &&
                        demand.Urun != null &&
                        demand.Urun.SaticiId == saticiId),
                AgreedDemands = await _context.Talepler
                    .AsNoTracking()
                    .CountAsync(demand =>
                        demand.Durum == TalepDurumlari.Anlasildi &&
                        demand.Urun != null &&
                        demand.Urun.SaticiId == saticiId),
                PendingOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == saticiId && offer.Durum == TalepTeklifDurumlari.Beklemede),
                AcceptedOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == saticiId && offer.Durum == TalepTeklifDurumlari.Kabul),
                RejectedOffers = await _context.TalepTeklifler
                    .AsNoTracking()
                    .CountAsync(offer => offer.SaticiId == saticiId && offer.Durum == TalepTeklifDurumlari.Red),
                UnreadMessages = await _context.ChatMessages
                    .AsNoTracking()
                    .CountAsync(message => message.ReceiverId == saticiId && message.ReadAt == null)
            };
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            return new AdminDashboardDto
            {
                TotalUsers = await _context.Users.AsNoTracking().CountAsync(),
                TotalSellers = await _context.SaticiProfilleri.AsNoTracking().CountAsync(),
                ActiveSellers = await _context.SaticiProfilleri.AsNoTracking().CountAsync(seller => seller.AktifMi),
                TotalBuyers = await _context.AliciProfilleri.AsNoTracking().CountAsync(),
                TotalProducts = await _context.Urunler.AsNoTracking().CountAsync(),
                ActiveProducts = await _context.Urunler.AsNoTracking().CountAsync(product => product.AktifMi),
                InactiveProducts = await _context.Urunler.AsNoTracking().CountAsync(product => !product.AktifMi),
                TotalDemands = await _context.Talepler.AsNoTracking().CountAsync(),
                OpenDemands = await _context.Talepler.AsNoTracking().CountAsync(demand => demand.Durum == TalepDurumlari.Acik),
                AgreedDemands = await _context.Talepler.AsNoTracking().CountAsync(demand => demand.Durum == TalepDurumlari.Anlasildi),
                TotalReviews = await _context.Yorumlar.AsNoTracking().CountAsync(),
                TotalMessages = await _context.ChatMessages.AsNoTracking().CountAsync(),
                UnreadMessages = await _context.ChatMessages.AsNoTracking().CountAsync(message => message.ReadAt == null)
            };
        }
    }
}
