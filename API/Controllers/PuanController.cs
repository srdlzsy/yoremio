using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuanController : ControllerBase
    {
        private readonly IPuanService _puanService;

        public PuanController(IPuanService puanService)
        {
            _puanService = puanService;
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("puan-ekle")]
        public async Task<IActionResult> PuanEkle([FromBody] PuanEkleDto dto)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (kullaniciId == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı girişi yapılmamış.", traceId: HttpContext.TraceIdentifier));
            }

            if (dto.PuanDegeri < 1 || dto.PuanDegeri > 5)
            {
                return BadRequest(ApiResponse<object>.Fail("Puan değeri 1 ile 5 arasında olmalıdır.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _puanService.GetOrCreatePuanAsync(dto.UrunId, kullaniciId, dto.PuanDegeri);
            return Ok(ApiResponse<object>.Ok(new { ortalama = result }, "Puan kaydedildi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("urun/{urunId}")]
        public async Task<IActionResult> GetPuanlarByUrunId(int urunId)
        {
            var puanlar = await _puanService.GetPuanlarByUrunIdAsync(urunId);
            var response = puanlar.Select(p => new PuanDto
            {
                Id = p.Id,
                UrunId = p.UrunId,
                KullaniciId = p.KullaniciId,
                PuanDegeri = p.PuanDegeri,
                PuanTarihi = p.PuanTarihi
            });

            return Ok(ApiResponse<IEnumerable<PuanDto>>.Ok(response, "Puanlar getirildi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("ortalama/{urunId}")]
        public async Task<IActionResult> GetOrtalamaPuanByUrunId(int urunId)
        {
            var ortalamaPuan = await _puanService.GetOrtalamaPuanByUrunIdAsync(urunId);
            return Ok(ApiResponse<object>.Ok(new { ortalama = ortalamaPuan }, "Ortalama puan getirildi.", HttpContext.TraceIdentifier));
        }
    }
}
