using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // JWT doğrulaması için

    public class PuanController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public PuanController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }
        [HttpPost("puan-ekle")]
        public async Task<IActionResult> PuanEkle([FromBody] PuanEkleDto dto)
        {
            // JWT içindeki kullanıcıyı al
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (kullaniciId == null)
                return Unauthorized("Kullanıcı girişi yapılmamış.");

            if (dto.PuanDegeri < 1 || dto.PuanDegeri > 5)
                return BadRequest("Puan değeri 1 ile 5 arasında olmalıdır.");

            var result = await _serviceManager.PuanService
                .GetOrCreatePuanAsync(dto.UrunId, kullaniciId, dto.PuanDegeri);

            return Ok(new { Ortalama = result });
        }

        [HttpGet("urun/{urunId}")]
        public async Task<IActionResult> GetPuanlarByUrunId(int urunId)
        {
            var puanlar = await _serviceManager.PuanService.GetPuanlarByUrunIdAsync(urunId);
            return Ok(puanlar);
        }
        [HttpGet("ortalama/{urunId}")]
        public async Task<IActionResult> GetOrtalamaPuanByUrunId(int urunId)
        {
            var ortalamaPuan = await _serviceManager.PuanService.GetOrtalamaPuanByUrunIdAsync(urunId);
            return Ok(ortalamaPuan);
        }
    }
}
