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
    public class YorumController : ControllerBase
    {
        private readonly IYorumServices _yorumService;

        public YorumController(IYorumServices yorumService)
        {
            _yorumService = yorumService;
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRoles.Alici)]
        public async Task<IActionResult> YorumEkle([FromBody] YorumEkleDto dto)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (kullaniciId == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _yorumService.YorumEkleAsync(dto, kullaniciId);
            return Ok(ApiResponse<YorumDto>.Ok(result, "Yorum eklendi.", HttpContext.TraceIdentifier));
        }

        [HttpPut("{yorumId:int}")]
        [Authorize(Roles = ApplicationRoles.Alici)]
        public async Task<IActionResult> YorumGuncelle(int yorumId, [FromBody] YorumEkleDto dto)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (kullaniciId == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var result = await _yorumService.GuncelleYorumAsync(yorumId, dto, kullaniciId);
            return Ok(ApiResponse<YorumDto>.Ok(result, "Yorum güncellendi.", HttpContext.TraceIdentifier));
        }

        [HttpDelete("{yorumId:int}")]
        [Authorize(Roles = ApplicationRoles.Alici)]
        public async Task<IActionResult> YorumSil(int yorumId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (kullaniciId == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var deleted = await _yorumService.SilYorumAsync(yorumId, kullaniciId);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.Fail("Yorum bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Yorum silindi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("{urunId}")]
        public async Task<IActionResult> GetYorumlarByUrunId(int urunId)
        {
            var yorumlar = await _yorumService.GetYorumlarByUrunIdAsync(urunId);
            return Ok(ApiResponse<IEnumerable<YorumDto>>.Ok(yorumlar, "Yorumlar getirildi.", HttpContext.TraceIdentifier));
        }
    }
}
