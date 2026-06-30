namespace Application.DTOs
{
    public sealed class AppBootstrapDto
    {
        public string Environment { get; set; } = string.Empty;
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<KategoriDto> Categories { get; set; } = Array.Empty<KategoriDto>();
        public IReadOnlyCollection<string> ProductSorts { get; set; } = Array.Empty<string>();
        public AppFeatureFlagsDto Features { get; set; } = new();
        public AppVerificationConfigDto Verification { get; set; } = new();
        public AppUploadConfigDto Uploads { get; set; } = new();
    }

    public sealed class AppFeatureFlagsDto
    {
        public bool ChatEnabled { get; set; } = true;
        public bool DemandFlowEnabled { get; set; } = true;
        public bool FavoritesEnabled { get; set; } = true;
        public bool RatingsEnabled { get; set; } = true;
        public bool ReviewsEnabled { get; set; } = true;
        public bool DevVerificationInboxEnabled { get; set; }
        public bool CloudinaryEnabled { get; set; }
    }

    public sealed class AppVerificationConfigDto
    {
        public bool RequireConfirmedEmailForSellerLogin { get; set; }
        public bool RequireConfirmedPhoneForSellerLogin { get; set; }
        public string? DevVerificationInboxUrl { get; set; }
    }

    public sealed class AppUploadConfigDto
    {
        public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
        public long MaxVideoBytes { get; set; } = 50 * 1024 * 1024;
        public long MaxMultipartBodyBytes { get; set; } = 100_000_000;
        public IReadOnlyCollection<string> ImageContentTypePrefixes { get; set; } = new[] { "image/" };
        public IReadOnlyCollection<string> VideoContentTypePrefixes { get; set; } = new[] { "video/" };
    }
}
