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

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("benim")]
        public async Task<IActionResult> GetBenimTaleplerim()
        {
            var aliciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(aliciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.GetAliciTalepleriAsync(aliciId);
            return Ok(ApiResponse<IReadOnlyCollection<TalepDto>>.Ok(result, "Alici talepleri getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpGet("satici")]
        public async Task<IActionResult> GetSaticiTalepleri()
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _talepService.GetSaticiTalepleriAsync(saticiId);
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
    }
}
