namespace Application.DTOs
{
    public class DashboardSummaryDto
    {
        public string[] Roles { get; set; } = Array.Empty<string>();
        public int UnreadMessages { get; set; }
        public int FavoriteProducts { get; set; }
        public int MyProducts { get; set; }
        public int OpenDemands { get; set; }
        public int BuyerOpenDemands { get; set; }
        public int SellerOpenDemands { get; set; }
        public int PendingOffers { get; set; }
        public int BuyerPendingOffers { get; set; }
        public int SellerPendingOffers { get; set; }
    }

    public class UrunTalepOzetDto
    {
        public int UrunId { get; set; }
        public string UrunAdi { get; set; } = string.Empty;
        public int TalepSayisi { get; set; }
        public int GoruntulenmeSayisi { get; set; }
        public int StokMiktari { get; set; }
    }

    public class SaticiDashboardDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalProductViews { get; set; }
        public int TotalFavorites { get; set; }
        public int FollowerCount { get; set; }
        public int TotalReviews { get; set; }
        public int TotalRatings { get; set; }
        public double AverageRating { get; set; }
        public double TrustScore { get; set; }
        public int OpenDemands { get; set; }
        public int UnansweredDemands { get; set; }
        public int MatchedOpenMarketDemands { get; set; }
        public int AgreedDemands { get; set; }
        public int VerifiedCompletedDeals { get; set; }
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int RejectedOffers { get; set; }
        public double AverageResponseMinutes { get; set; }
        public int UnreadMessages { get; set; }
        public List<UrunTalepOzetDto> TopRequestedProducts { get; set; } = new();
        public List<string> ActionSuggestions { get; set; } = new();
    }

    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int ActiveSellers { get; set; }
        public int TotalBuyers { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int TotalDemands { get; set; }
        public int OpenDemands { get; set; }
        public int OpenMarketDemands { get; set; }
        public int AgreedDemands { get; set; }
        public int VerifiedCompletedDeals { get; set; }
        public double OfferConversionRatePercent { get; set; }
        public double DealAgreementRatePercent { get; set; }
        public double AverageSellerResponseMinutes { get; set; }
        public int RepeatBuyers60Days { get; set; }
        public double RepeatBuyerRatePercent { get; set; }
        public int ActivePriceAlerts { get; set; }
        public int TotalSellerFollows { get; set; }
        public int TotalReviews { get; set; }
        public int TotalMessages { get; set; }
        public int UnreadMessages { get; set; }
    }
}
