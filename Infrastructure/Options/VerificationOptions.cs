namespace Infrastructure.Options
{
    public class VerificationOptions
    {
        public string PublicBaseUrl { get; set; } = "http://localhost:5089";
        public bool RequireConfirmedEmailForSellerLogin { get; set; } = true;
        public bool RequireConfirmedPhoneForSellerLogin { get; set; } = true;
    }
}
