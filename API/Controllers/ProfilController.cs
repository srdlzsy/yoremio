using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfilController : ControllerBase
    {
        private readonly ISaticiProfiliService _saticiProfiliService;
        private readonly IAliciProfiliService _aliciProfiliService;

        public ProfilController(ISaticiProfiliService saticiProfiliService, IAliciProfiliService aliciProfiliService)
        {
            _saticiProfiliService = saticiProfiliService;
            _aliciProfiliService = aliciProfiliService;
        }

        [AllowAnonymous]
        [HttpGet("saticilar/one-cikan")]
        public async Task<IActionResult> GetOneCikanSaticilar([FromQuery] int take = 6)
        {
            var saticilar = await _saticiProfiliService.GetOneCikanSaticilarAsync(take);
            return Ok(ApiResponse<IReadOnlyCollection<SaticiOzetDto>>.Ok(saticilar, "One cikan saticilar getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpGet("satici")]
        public async Task<IActionResult> GetSaticiProfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var satici = await _saticiProfiliService.GetSaticiProfilDtoAsync(userId);
            if (satici == null)
            {
                return NotFound(ApiResponse<object>.Fail("Profil bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<SaticiProfilDto>.Ok(satici, "Profil getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPut("satici")]
        public async Task<IActionResult> UpdateSaticiProfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            SaticiProfilGuncelleDto dto;
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                dto = new SaticiProfilGuncelleDto
                {
                    MagazaAdi = form.TryGetValue("magazaAdi", out var magazaAdi) ? magazaAdi.ToString() : null,
                    Adres = form.TryGetValue("adres", out var adres) ? adres.ToString() : null,
                    Sehir = form.TryGetValue("sehir", out var sehir) ? sehir.ToString() : null,
                    Ilce = form.TryGetValue("ilce", out var ilce) ? ilce.ToString() : null,
                    Aciklama = form.TryGetValue("aciklama", out var aciklama) ? aciklama.ToString() : null,
                    PhoneNumber = form.TryGetValue("phoneNumber", out var phoneNumber) ? phoneNumber.ToString() : null,
                    KapakResim = form.Files.GetFile("kapakResim") ?? form.Files.FirstOrDefault()
                };
            }
            else
            {
                dto = await Request.ReadFromJsonAsync<SaticiProfilGuncelleDto>() ?? new SaticiProfilGuncelleDto();
            }

            if (!TryValidateModel(dto))
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Geçersiz değer." : e.ErrorMessage));

                return BadRequest(new
                {
                    success = false,
                    message = "Doğrulama hatası oluştu.",
                    data = (object?)null,
                    errors,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var satici = await _saticiProfiliService.UpdateProfilAsync(userId, dto);
            if (satici == null)
            {
                return NotFound(ApiResponse<object>.Fail("Profil bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<SaticiProfilDto>.Ok(satici, "Profil güncellendi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("alici")]
        public async Task<IActionResult> GetAliciProfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var alici = await _aliciProfiliService.GetAliciProfilDtoAsync(userId);
            if (alici == null)
            {
                return NotFound(ApiResponse<object>.Fail("Alıcı profili bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<AliciProfilDto>.Ok(alici, "Alıcı profili getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPut("alici")]
        public async Task<IActionResult> UpdateAliciProfil([FromBody] AliciProfilGuncelleDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var alici = await _aliciProfiliService.UpdateProfilAsync(userId, dto);
            if (alici == null)
            {
                return NotFound(ApiResponse<object>.Fail("Alıcı profili bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<AliciProfilDto>.Ok(alici, "Alıcı profili güncellendi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("takip-edilenler")]
        public async Task<IActionResult> GetTakipEdilenSaticilar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var saticilar = await _saticiProfiliService.GetTakipEdilenSaticilarAsync(userId);
            return Ok(ApiResponse<IReadOnlyCollection<SaticiOzetDto>>.Ok(saticilar, "Takip edilen üreticiler getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("satici/{saticiId}/takip")]
        public async Task<IActionResult> SaticiTakipEt(string saticiId, [FromBody] SaticiTakipIstekDto? dto = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var added = await _saticiProfiliService.SaticiTakipEtAsync(userId, saticiId, dto?.BildirimAcikMi ?? true);
            var message = added ? "Üretici takip edildi." : "Üretici takip ayarları güncellendi.";
            return Ok(ApiResponse<object>.Ok(null, message, HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpDelete("satici/{saticiId}/takip")]
        public async Task<IActionResult> SaticiTakibiBirak(string saticiId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var removed = await _saticiProfiliService.SaticiTakibiBirakAsync(userId, saticiId);
            if (!removed)
            {
                return NotFound(ApiResponse<object>.Fail("Takip kaydı bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Üretici takibi bırakıldı.", HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpGet("satici/{saticiId}")]
        public async Task<IActionResult> GetSaticiPublicProfil(string saticiId)
        {
            var satici = await _saticiProfiliService.GetPublicSaticiDetayAsync(saticiId);
            if (satici == null)
            {
                return NotFound(ApiResponse<object>.Fail("Satıcı bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<SaticiOzetDto>.Ok(satici, "Satıcı vitrin bilgisi getirildi.", HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpGet("satici/{saticiId}/guven-skoru")]
        public async Task<IActionResult> GetSaticiGuvenSkoru(string saticiId)
        {
            var skor = await _saticiProfiliService.GetGuvenSkoruAsync(saticiId);
            if (skor == null)
            {
                return NotFound(ApiResponse<object>.Fail("Satıcı bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<SaticiGuvenSkoruDto>.Ok(skor, "Satıcı güven skoru getirildi.", HttpContext.TraceIdentifier));
        }
    }
}
