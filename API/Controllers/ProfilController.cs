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
    [Authorize(Roles = ApplicationRoles.Satici)]
    public class ProfilController : ControllerBase
    {
        private readonly ISaticiProfiliService _saticiProfiliService;

        public ProfilController(ISaticiProfiliService saticiProfiliService)
        {
            _saticiProfiliService = saticiProfiliService;
        }

        [AllowAnonymous]
        [HttpGet("saticilar/one-cikan")]
        public async Task<IActionResult> GetOneCikanSaticilar([FromQuery] int take = 6)
        {
            var saticilar = await _saticiProfiliService.GetOneCikanSaticilarAsync(take);
            return Ok(ApiResponse<IReadOnlyCollection<SaticiOzetDto>>.Ok(saticilar, "One cikan saticilar getirildi.", HttpContext.TraceIdentifier));
        }

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

        [HttpPut("satici")]
        public async Task<IActionResult> UpdateSaticiProfil([FromBody] SaticiProfilGuncelleDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var satici = await _saticiProfiliService.UpdateProfilAsync(userId, dto);
            if (satici == null)
            {
                return NotFound(ApiResponse<object>.Fail("Profil bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<SaticiProfilDto>.Ok(satici, "Profil güncellendi.", HttpContext.TraceIdentifier));
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
