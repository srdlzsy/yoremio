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
    public class UrunController : ControllerBase
    {
        private readonly IUrunService _urunService;

        public UrunController(IUrunService urunService)
        {
            _urunService = urunService;
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPost("urun-ekle")]
        public async Task<IActionResult> UrunEkle([FromForm] UrunEkleDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var urun = await _urunService.UrunEkleAsync(saticiId, dto);
            var urunDto = await _urunService.GetUrunByIdAsync(urun.Id);
            return CreatedAtAction(nameof(GetById), new { id = urun.Id },
                ApiResponse<UrunDto?>.Ok(urunDto, "Ürün oluşturuldu.", HttpContext.TraceIdentifier));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] UrunQuery query)
        {
            var urunler = await _urunService.GetAllUrunlerAsync(query);
            return Ok(ApiResponse<PagedResult<UrunDto>>.Ok(urunler, "Ürünler getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("onerilen")]
        public async Task<IActionResult> GetRecommended([FromQuery] int take = 12)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var urunler = await _urunService.GetOnerilenUrunlerAsync(kullaniciId, take);
            return Ok(ApiResponse<IEnumerable<UrunDto>>.Ok(urunler, "Önerilen ürünler getirildi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var urun = await _urunService.GetUrunByIdAsync(id);
            if (urun == null)
            {
                return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<UrunDto>.Ok(urun, "Ürün getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpGet("urunlerim")]
        public async Task<IActionResult> GetMyProducts()
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var urunler = await _urunService.GetUrunlerBySaticiIdAsync(saticiId);
            return Ok(ApiResponse<IEnumerable<UrunDto>>.Ok(urunler, "Kullanıcı ürünleri getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("favorilerim")]
        public async Task<IActionResult> GetFavorilerim()
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var urunler = await _urunService.GetFavoriUrunlerAsync(kullaniciId);
            return Ok(ApiResponse<IEnumerable<UrunDto>>.Ok(urunler, "Favori ürünler getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("{urunId:int}/favori")]
        public async Task<IActionResult> FavoriyeEkle(int urunId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var success = await _urunService.FavoriyeEkleAsync(kullaniciId, urunId);
            if (!success)
            {
                return Ok(ApiResponse<object>.Ok(null, "Ürün zaten favorilerde.", HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Ürün favorilere eklendi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpDelete("{urunId:int}/favori")]
        public async Task<IActionResult> FavoridenCikar(int urunId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var success = await _urunService.FavoridenCikarAsync(kullaniciId, urunId);
            if (!success)
            {
                return NotFound(ApiResponse<object>.Fail("Favori ürün bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Ürün favorilerden çıkarıldı.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPut("{urunId}")]
        public async Task<IActionResult> UrunGuncelle(int urunId, [FromForm] UrunGuncelleDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var updatedUrun = await _urunService.UrunGuncelleAsync(urunId, saticiId, dto);
            return Ok(ApiResponse<UrunDto>.Ok(updatedUrun, "Ürün güncellendi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpDelete("{urunId}")]
        public async Task<IActionResult> UrunSil(int urunId)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var success = await _urunService.UrunSilAsync(urunId, saticiId);
            if (!success)
            {
                return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı veya silinemedi.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Ürün silindi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpDelete("{urunId}/resimler/{resimId}")]
        public async Task<IActionResult> UrunResimSil(int urunId, int resimId)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var success = await _urunService.UrunResimSilAsync(urunId, resimId, saticiId);
            if (!success)
            {
                return NotFound(ApiResponse<object>.Fail("Resim bulunamadı veya silinemedi.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Ürün resmi silindi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpDelete("{urunId}/videolar/{videoId}")]
        public async Task<IActionResult> UrunVideoSil(int urunId, int videoId)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var success = await _urunService.UrunVideoSilAsync(urunId, videoId, saticiId);
            if (!success)
            {
                return NotFound(ApiResponse<object>.Fail("Video bulunamadı veya silinemedi.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Ürün videosu silindi.", HttpContext.TraceIdentifier));
        }
    }
}
