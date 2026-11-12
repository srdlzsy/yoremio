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
        private readonly IYorumServices _yorumService;

        public UrunController(IUrunService urunService, IYorumServices yorumService)
        {
            _urunService = urunService;
            _yorumService = yorumService;
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

        [AllowAnonymous]
        [HttpGet("kesfet")]
        public async Task<IActionResult> Kesfet([FromQuery] UrunQuery query)
        {
            var sonuc = await _urunService.KesfetAsync(query);
            return Ok(ApiResponse<UrunKesfetSonucDto>.Ok(sonuc, "Keşif sonuçları getirildi.", HttpContext.TraceIdentifier));
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
            var urun = await _urunService.GetUrunByIdAsync(id, incrementView: true);
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
        [HttpGet("{urunId:int}/yorum-yetkisi")]
        public async Task<IActionResult> GetYorumYetkisi(int urunId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            var yetki = await _yorumService.GetYorumYetkisiAsync(urunId, kullaniciId);
            return Ok(ApiResponse<YorumYetkisiDto>.Ok(yetki, "Yorum yetkisi getirildi.", HttpContext.TraceIdentifier));
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
        [HttpPatch("toplu-stok")]
        public async Task<IActionResult> TopluStokGuncelle([FromBody] TopluStokGuncelleDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var updatedUrunler = await _urunService.TopluStokGuncelleAsync(saticiId, dto);
            return Ok(ApiResponse<IReadOnlyCollection<UrunDto>>.Ok(updatedUrunler, "Toplu stok/fiyat güncellemesi uygulandı.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("fiyat-alarmlarim")]
        public async Task<IActionResult> GetFiyatAlarmlarim()
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var alarmlar = await _urunService.GetFiyatAlarmlarimAsync(kullaniciId);
            return Ok(ApiResponse<IReadOnlyCollection<FiyatAlarmiDto>>.Ok(alarmlar, "Fiyat alarmları getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("{urunId:int}/fiyat-alarmi")]
        public async Task<IActionResult> FiyatAlarmiKaydet(int urunId, [FromBody] FiyatAlarmiOlusturDto dto)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var alarm = await _urunService.FiyatAlarmiKaydetAsync(kullaniciId, urunId, dto);
            return Ok(ApiResponse<FiyatAlarmiDto>.Ok(alarm, "Fiyat alarmı kaydedildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpDelete("{urunId:int}/fiyat-alarmi")]
        public async Task<IActionResult> FiyatAlarmiSil(int urunId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var removed = await _urunService.FiyatAlarmiSilAsync(kullaniciId, urunId);
            if (!removed)
            {
                return NotFound(ApiResponse<object>.Fail("Fiyat alarmı bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Fiyat alarmı silindi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpGet("stok-alarmlarim")]
        public async Task<IActionResult> GetStokAlarmlarim()
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var alarmlar = await _urunService.GetStokAlarmlarimAsync(kullaniciId);
            return Ok(ApiResponse<IReadOnlyCollection<StokAlarmiDto>>.Ok(alarmlar, "Stok alarmları getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpPost("{urunId:int}/stok-alarmi")]
        public async Task<IActionResult> StokAlarmiKaydet(int urunId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var alarm = await _urunService.StokAlarmiKaydetAsync(kullaniciId, urunId);
            return Ok(ApiResponse<StokAlarmiDto>.Ok(alarm, "Stok alarmı kaydedildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Alici)]
        [HttpDelete("{urunId:int}/stok-alarmi")]
        public async Task<IActionResult> StokAlarmiSil(int urunId)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanıcı doğrulanamadı.", traceId: HttpContext.TraceIdentifier));
            }

            var removed = await _urunService.StokAlarmiSilAsync(kullaniciId, urunId);
            if (!removed)
            {
                return NotFound(ApiResponse<object>.Fail("Stok alarmı bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Stok alarmı silindi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPatch("{urunId:int}/status")]
        public async Task<IActionResult> UrunDurumuGuncelle(int urunId, [FromBody] UrunDurumGuncelleDto dto)
        {
            return await UrunDurumuGuncelleInternal(urunId, dto);
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPut("{urunId:int}/status")]
        public async Task<IActionResult> UrunDurumuPutGuncelle(int urunId, [FromBody] UrunDurumGuncelleDto dto)
        {
            return await UrunDurumuGuncelleInternal(urunId, dto);
        }

        private async Task<IActionResult> UrunDurumuGuncelleInternal(int urunId, UrunDurumGuncelleDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Kullanici dogrulanamadi.", traceId: HttpContext.TraceIdentifier));
            }

            if (!dto.AktifMi.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("AktifMi alani zorunludur.", traceId: HttpContext.TraceIdentifier));
            }

            var updatedUrun = await _urunService.UrunDurumuGuncelleAsync(urunId, saticiId, dto.AktifMi.Value);
            return Ok(ApiResponse<UrunDto>.Ok(updatedUrun, "Urun durumu guncellendi.", HttpContext.TraceIdentifier));
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
