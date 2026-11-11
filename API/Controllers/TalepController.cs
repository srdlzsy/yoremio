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
    public class TalepController : ControllerBase
    {
        private readonly ITalepService _talepService;

        public TalepController(ITalepService talepService)
        {
            _talepService = talepService;
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost]
        public async Task<IActionResult> TalepOlustur([FromBody] TalepOlusturDto dto)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.TalepOlusturAsync(aliciId, dto);
            return Ok(ApiResponse<TalepDto>.Ok(result, "Talep olusturuldu.", HttpContext.TraceIdentifier));
        }

        [Authorize]
        [HttpGet("{talepId:int}")]
        public async Task<IActionResult> GetTalepById(int talepId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.GetTalepByIdAsync(talepId, userId);
            return Ok(ApiResponse<TalepDto>.Ok(result, "Talep detayi getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("benim")]
        public async Task<IActionResult> GetBenimTaleplerim(
            [FromQuery] string? durum = null,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                var paged = await _talepService.GetAliciTalepleriPagedAsync(aliciId, durum, page ?? 1, pageSize ?? 12);
                return Ok(ApiResponse<Domain.Models.PagedResult<TalepDto>>.Ok(paged, "Alici talepleri getirildi.", HttpContext.TraceIdentifier));
            }

            var result = await _talepService.GetAliciTalepleriAsync(aliciId, durum);
            return Ok(ApiResponse<IReadOnlyCollection<TalepDto>>.Ok(result, "Alici talepleri getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpGet("satici")]
        public async Task<IActionResult> GetSaticiTalepleri(
            [FromQuery] string? durum = null,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                var paged = await _talepService.GetSaticiTalepleriPagedAsync(saticiId, durum, page ?? 1, pageSize ?? 12);
                return Ok(ApiResponse<Domain.Models.PagedResult<TalepDto>>.Ok(paged, "Satici talepleri getirildi.", HttpContext.TraceIdentifier));
            }

            var result = await _talepService.GetSaticiTalepleriAsync(saticiId, durum);
            return Ok(ApiResponse<IReadOnlyCollection<TalepDto>>.Ok(result, "Satici talepleri getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPost("{talepId:int}/teklif")]
        public async Task<IActionResult> TeklifVer(int talepId, [FromBody] TalepTeklifOlusturDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.TeklifVerAsync(talepId, saticiId, dto);
            return Ok(ApiResponse<TalepTeklifDto>.Ok(result, "Teklif kaydedildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("teklif/{teklifId:int}/kabul")]
        public async Task<IActionResult> TeklifKabulEt(int teklifId)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.TeklifKabulEtAsync(teklifId, aliciId);
            return Ok(ApiResponse<TalepDto>.Ok(result, "Teklif kabul edildi, talep anlasildi olarak guncellendi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("teklif/{teklifId:int}/red")]
        public async Task<IActionResult> TeklifReddet(int teklifId)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.TeklifReddetAsync(teklifId, aliciId);
            return Ok(ApiResponse<TalepDto>.Ok(result, "Teklif reddedildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("{talepId:int}/iptal")]
        public async Task<IActionResult> TalepIptalEt(int talepId)
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.TalepIptalEtAsync(talepId, aliciId);
            return Ok(ApiResponse<TalepDto>.Ok(result, "Talep iptal edildi.", HttpContext.TraceIdentifier));
        }
    }
}
