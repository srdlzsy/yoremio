using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISaticiProfiliService _saticiProfiliService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly VerificationOptions _verificationOptions;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ISaticiProfiliService saticiProfiliService,
            UserManager<ApplicationUser> userManager,
            IOptions<VerificationOptions> verificationOptions,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _saticiProfiliService = saticiProfiliService;
            _userManager = userManager;
            _verificationOptions = verificationOptions.Value;
            _logger = logger;
        }

        [HttpPost("register/satici")]
        public async Task<IActionResult> RegisterSatici([FromBody] RegisterSaticiDto dto)
        {
            var result = await _authService.RegisterSaticiAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<object>.Fail("Satici kaydi basarisiz.", result.Error, HttpContext.TraceIdentifier));
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                return Ok(ApiResponse<object>.Ok(
                    new
                    {
                        verificationMessageSent = false,
                        warning = result.Error
                    },
                    "Satici kaydi alindi. Email dogrulama mesaji gonderilemedi; daha sonra yeniden gonderebilirsiniz.",
                    HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Satici kaydi basarili.", HttpContext.TraceIdentifier));
        }

        [HttpPost("register/alici")]
        public async Task<IActionResult> RegisterAlici([FromBody] RegisterAliciDto dto)
        {
            var result = await _authService.RegisterAliciAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<object>.Fail("Alici kaydi basarisiz.", result.Error, HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Alici kaydi basarili.", HttpContext.TraceIdentifier));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Token))
            {
                return Unauthorized(ApiResponse<object>.Fail("Giris basarisiz.", result.Error, HttpContext.TraceIdentifier));
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);
            var claims = jwtToken.Claims.ToList();
            var roles = claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var response = new LoginResponseDto
            {
                Token = result.Token,
                UserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                Email = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value,
                Role = roles.FirstOrDefault(),
                Roles = roles
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Giris basarili.", HttpContext.TraceIdentifier));
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto)
        {
            var result = await _authService.ResendVerificationAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<object>.Fail("Email dogrulama mesaji gonderilemedi.", result.Error, HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(
                null,
                "Email dogrulama mesaji varsa yeniden gonderildi.",
                HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmailCode([FromBody] ConfirmEmailDto dto)
        {
            var result = await _authService.ConfirmEmailCodeAsync(dto.Email, dto.Code);
            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.UserId))
            {
                return BadRequest(ApiResponse<object>.Fail("Email dogrulama basarisiz.", traceId: HttpContext.TraceIdentifier));
            }

            await TryActivateVerifiedSellerAsync(result.UserId);
            return Ok(ApiResponse<object>.Ok(null, "Email basariyla dogrulandi.", HttpContext.TraceIdentifier));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail("Kullanici bulunamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var data = new AuthMeDto
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault(),
                Roles = roles.ToArray(),
                EmailConfirmed = user.EmailConfirmed
            };

            return Ok(ApiResponse<AuthMeDto>.Ok(data, "Kullanici bilgisi getirildi.", HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Email dogrulama: userId veya token bos. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest(ApiResponse<object>.Fail("Gecersiz dogrulama parametreleri.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _authService.ConfirmEmailAsync(userId, token);
            if (!result)
            {
                _logger.LogWarning("Email dogrulama basarisiz. UserId: {UserId}, IP: {IP}", userId, HttpContext.Connection.RemoteIpAddress);
                return BadRequest(ApiResponse<object>.Fail("Email dogrulama basarisiz.", traceId: HttpContext.TraceIdentifier));
            }

            await TryActivateVerifiedSellerAsync(userId);

            _logger.LogInformation("Email basariyla dogrulandi. UserId: {UserId}", userId);
            return Ok(ApiResponse<object>.Ok(null, "Email basariyla dogrulandi.", HttpContext.TraceIdentifier));
        }

        private async Task TryActivateVerifiedSellerAsync(string userId)
        {
            var satici = await _saticiProfiliService.GetSaticiWithUserByIdAsync(userId);
            if (satici?.Kullanici is null || satici.AktifMi)
            {
                return;
            }

            var emailOk = !_verificationOptions.RequireConfirmedEmailForSellerLogin || satici.Kullanici.EmailConfirmed;
            if (!emailOk)
            {
                return;
            }

            satici.AktifMi = true;
            await _saticiProfiliService.UpdateAsync(satici);
        }

        public class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
            public string? UserId { get; set; }
            public string? Email { get; set; }
            public string? Role { get; set; }
            public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        }
    }
}
