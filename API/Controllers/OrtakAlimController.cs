using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrtakAlimController : ControllerBase
    {
        private readonly IOrtakAlimService _ortakAlimService;

        public OrtakAlimController(IOrtakAlimService ortakAlimService)
        {
            _ortakAlimService = ortakAlimService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? hedefSehir = null,
            [FromQuery] string? menseiSehir = null,
            [FromQuery] string? q = null,
            [FromQuery] string? durum = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var result = await _ortakAlimService.GetAllAsync(hedefSehir, menseiSehir, q, durum, page, pageSize);
            return Ok(ApiResponse<PagedResult<OrtakAlimDto>>.Ok(result, "Ortak alim kampanyalari getirildi.", HttpContext.TraceIdentifier));
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _ortakAlimService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail("Ortak alim bulunamadi.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<OrtakAlimDto>.Ok(result, "Ortak alim detayi getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Olustur([FromBody] OrtakAlimOlusturDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();
            var result = await _ortakAlimService.OlusturAsync(userId, roles, dto);
            return Ok(ApiResponse<OrtakAlimDto>.Ok(result, "Ortak alim olusturuldu.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPost("{id:int}/onayla")]
        public async Task<IActionResult> Onayla(int id)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _ortakAlimService.OnaylaAsync(id, saticiId);
            return Ok(ApiResponse<OrtakAlimDto>.Ok(result, "Ortak alim onaylandi ve katilima acildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("{id:int}/katil")]
        public async Task<IActionResult> Katil(int id, [FromBody] OrtakAlimKatilDto? dto = null)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _ortakAlimService.KatilAsync(id, aliciId, dto);
            return Ok(ApiResponse<OrtakAlimDto>.Ok(result, "Ortak alima katilim kaydedildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpDelete("{id:int}/katil")]
        public async Task<IActionResult> KatilimiIptalEt(int id)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _ortakAlimService.KatilimiIptalEtAsync(id, aliciId);
            return Ok(ApiResponse<OrtakAlimDto>.Ok(result, "Ortak alim katilimi iptal edildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPatch("{id:int}/durum")]
        public async Task<IActionResult> DurumGuncelle(int id, [FromBody] OrtakAlimDurumGuncelleDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _ortakAlimService.DurumGuncelleAsync(id, saticiId, dto.Durum);
            return Ok(ApiResponse<OrtakAlimDto>.Ok(result, "Ortak alim durumu guncellendi.", HttpContext.TraceIdentifier));
        }
    }
}
