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

    public class SaticiDashboardDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalReviews { get; set; }
        public int TotalRatings { get; set; }
        public double AverageRating { get; set; }
        public double TrustScore { get; set; }
        public int OpenDemands { get; set; }
        public int AgreedDemands { get; set; }
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int RejectedOffers { get; set; }
        public int UnreadMessages { get; set; }
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
        public int AgreedDemands { get; set; }
        public int TotalReviews { get; set; }
        public int TotalMessages { get; set; }
        public int UnreadMessages { get; set; }
    }
}
