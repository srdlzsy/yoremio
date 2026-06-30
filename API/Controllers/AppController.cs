using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AppController : ControllerBase
    {
        private static readonly string[] ProductSorts =
        {
            "newest",
            "oldest",
            "price_asc",
            "price_desc",
            "name_asc",
            "name_desc",
            "top_rated",
            "most_reviewed",
            "most_favorited"
        };

        private readonly IKategoriService _kategoriService;
        private readonly IWebHostEnvironment _environment;
        private readonly VerificationOptions _verificationOptions;
        private readonly CloudinaryOptions _cloudinaryOptions;

        public AppController(
            IKategoriService kategoriService,
            IWebHostEnvironment environment,
            IOptions<VerificationOptions> verificationOptions,
            IOptions<CloudinaryOptions> cloudinaryOptions)
        {
            _kategoriService = kategoriService;
            _environment = environment;
            _verificationOptions = verificationOptions.Value;
            _cloudinaryOptions = cloudinaryOptions.Value;
        }

        [HttpGet("bootstrap")]
        public async Task<IActionResult> Bootstrap()
        {
            var categories = await _kategoriService.GetAllDtosAsync();
            var devVerificationEnabled = _environment.IsDevelopment();

            var data = new AppBootstrapDto
            {
                Environment = _environment.EnvironmentName,
                Roles = new[] { ApplicationRoles.Admin, ApplicationRoles.Alici, ApplicationRoles.Satici },
                Categories = categories.ToArray(),
                ProductSorts = ProductSorts,
                Features = new AppFeatureFlagsDto
                {
                    DevVerificationInboxEnabled = devVerificationEnabled,
                    CloudinaryEnabled = _cloudinaryOptions.Enabled
                },
                Verification = new AppVerificationConfigDto
                {
                    RequireConfirmedEmailForSellerLogin = _verificationOptions.RequireConfirmedEmailForSellerLogin,
                    RequireConfirmedPhoneForSellerLogin = _verificationOptions.RequireConfirmedPhoneForSellerLogin,
                    DevVerificationInboxUrl = devVerificationEnabled ? "/dev/verification" : null
                }
            };

            return Ok(ApiResponse<AppBootstrapDto>.Ok(data, "Uygulama baslangic verisi getirildi.", HttpContext.TraceIdentifier));
        }
    }
}
