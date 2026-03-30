using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ISaticiProfiliService saticiProfiliService,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _saticiProfiliService = saticiProfiliService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("register/satici")]
        public async Task<IActionResult> RegisterSatici([FromBody] RegisterSaticiDto dto)
        {
            var result = await _authService.RegisterSaticiAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<object>.Fail("Satıcı kaydı başarısız.", result.Error, HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Satıcı kaydı başarılı.", HttpContext.TraceIdentifier));
        }

        [HttpPost("register/alici")]
        public async Task<IActionResult> RegisterAlici([FromBody] RegisterAliciDto dto)
        {
            var result = await _authService.RegisterAliciAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<object>.Fail("Alıcı kaydı başarısız.", result.Error, HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Alıcı kaydı başarılı.", HttpContext.TraceIdentifier));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Token))
            {
                return Unauthorized(ApiResponse<object>.Fail("Giriş başarısız.", result.Error, HttpContext.TraceIdentifier));
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);
            var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            var response = new LoginResponseDto
            {
                Token = result.Token,
                UserId = claims.GetValueOrDefault(ClaimTypes.NameIdentifier),
                Email = claims.GetValueOrDefault(JwtRegisteredClaimNames.Email),
                Role = claims.GetValueOrDefault(ClaimTypes.Role)
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Giriş başarılı.", HttpContext.TraceIdentifier));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail("Kullanıcı bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var data = new AuthMeDto
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault(),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed
            };

            return Ok(ApiResponse<AuthMeDto>.Ok(data, "Kullanıcı bilgisi getirildi.", HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Email doğrulama: userId veya token boş. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest(ApiResponse<object>.Fail("Geçersiz doğrulama parametreleri.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _authService.ConfirmEmailAsync(userId, token);
            if (!result)
            {
                _logger.LogWarning("Email doğrulama başarısız. UserId: {UserId}, IP: {IP}", userId, HttpContext.Connection.RemoteIpAddress);
                return BadRequest(ApiResponse<object>.Fail("Email doğrulama başarısız.", traceId: HttpContext.TraceIdentifier));
            }

            var satici = await _saticiProfiliService.GetSaticiWithUserByIdAsync(userId);
            if (satici != null)
            {
                satici.AktifMi = true;
                await _saticiProfiliService.UpdateAsync(satici);
            }

            _logger.LogInformation("Email başarıyla doğrulandı. UserId: {UserId}", userId);
            return Ok(ApiResponse<object>.Ok(null, "Email başarıyla doğrulandı.", HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpGet("confirm-phone")]
        public async Task<IActionResult> ConfirmPhone([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Telefon doğrulama: userId veya token boş. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest(ApiResponse<object>.Fail("Geçersiz doğrulama parametreleri.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _authService.ConfirmPhoneAsync(userId, token);
            if (!result)
            {
                _logger.LogWarning("Telefon doğrulama başarısız. UserId: {UserId}, IP: {IP}", userId, HttpContext.Connection.RemoteIpAddress);
                return BadRequest(ApiResponse<object>.Fail("Telefon doğrulama başarısız.", traceId: HttpContext.TraceIdentifier));
            }

            _logger.LogInformation("Telefon başarıyla doğrulandı. UserId: {UserId}", userId);
            return Ok(ApiResponse<object>.Ok(null, "Telefon başarıyla doğrulandı.", HttpContext.TraceIdentifier));
        }

        public class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
            public string? UserId { get; set; }
            public string? Email { get; set; }
            public string? Role { get; set; }
        }
    }
}
