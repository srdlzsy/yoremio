using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UrunController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public UrunController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpPost("urun-ekle")]
        public async Task<IActionResult> UrunEkle([FromForm] UrunEkleDto dto)
        {
            Console.WriteLine("DTO ekleniyor: " + dto);
            try
            {
                // JWT'den satıcı id'sini al
                var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(saticiId))
                    return Unauthorized(new { error = "Kullanıcı doğrulanamadı." });

                // Servise DTO ve satıcı Id gönder
                var urun = await _serviceManager.UrunService.UrunEkleAsync(saticiId, dto);

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        // Tüm ürünleri getir (resim ve videolar dahil)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var urunler = await _serviceManager.UrunService.GetAllUrunlerAsync();
            return Ok(urunler);
        }

        // Id'ye göre tek ürün getir (resimler, videolar dahil)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var urun = await _serviceManager.UrunService.GetUrunByIdAsync(id);
            if (urun == null)
                return NotFound();

            return Ok(urun);
        }

        // JWT ile giriş yapmış kullanıcının ürünlerini getir
        [Authorize]
        [HttpGet("urunlerim")]
        public async Task<IActionResult> GetMyProducts()
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(saticiId))
                return Unauthorized();

            var urunler = await _serviceManager.UrunService.GetUrunlerBySaticiIdAsync(saticiId);
            return Ok(urunler);
        }
        [HttpPut("{urunId}")]
        public async Task<IActionResult> UrunGuncelle(int urunId, [FromForm] UrunGuncelleDto dto)
        {
            try
            {
                var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(saticiId))
                    return Unauthorized("Kullanıcı doğrulanamadı.");

                var updatedUrun = await _serviceManager.UrunService.UrunGuncelleAsync(urunId, saticiId, dto);
                return Ok(updatedUrun);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{urunId}")]
        public async Task<IActionResult> UrunSil(int urunId)
        {
            try
            {
                var success = await _serviceManager.UrunService.UrunSilAsync(urunId);
                if (!success)
                    return NotFound("Ürün bulunamadı veya silinemedi.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{urunId}/resimler/{resimId}")]
        public async Task<IActionResult> UrunResimSil(int urunId, int resimId)
        {
            try
            {
                var success = await _serviceManager.UrunService.UrunResimSilAsync(urunId, resimId);
                if (!success)
                    return NotFound("Resim bulunamadı veya silinemedi.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{urunId}/videolar/{videoId}")]
        public async Task<IActionResult> UrunVideoSil(int urunId, int videoId)
        {
            try
            {
                var success = await _serviceManager.UrunService.UrunVideoSilAsync(urunId, videoId);
                if (!success)
                    return NotFound("Video bulunamadı veya silinemedi.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("deneme")]
        public string Deneme()
        {
            return "deneme";
        }


    }
}
