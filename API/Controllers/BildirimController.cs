using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BildirimController : ControllerBase
    {
        private readonly IBildirimService _bildirimService;

        public BildirimController(IBildirimService bildirimService)
        {
            _bildirimService = bildirimService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBildirimler([FromQuery] bool sadeceOkunmamis = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _bildirimService.GetBildirimlerAsync(kullaniciId, sadeceOkunmamis, page, pageSize);

            return Ok(ApiResponse<BildirimPagedResultDto>.Ok(
                result,
                "Bildirimler getirildi.",
                HttpContext.TraceIdentifier));
        }

        [HttpGet("okunmamis-sayisi")]
        public async Task<IActionResult> GetOkunmamisSayisi()
        {
            var kullaniciId = GetCurrentUserId();
            var count = await _bildirimService.GetOkunmamisSayisiAsync(kullaniciId);

            return Ok(ApiResponse<object>.Ok(
                new { okunmamisSayisi = count },
                "Okunmamis bildirim sayisi getirildi.",
                HttpContext.TraceIdentifier));
        }

        [HttpPost("{bildirimId:long}/okundu")]
        public async Task<IActionResult> OkunduIsaretle(long bildirimId)
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _bildirimService.OkunduIsaretleAsync(bildirimId, kullaniciId);

            return Ok(ApiResponse<BildirimDto>.Ok(
                result,
                "Bildirim okundu olarak isaretlendi.",
                HttpContext.TraceIdentifier));
        }

        [HttpPost("tumunu-okundu")]
        public async Task<IActionResult> TumunuOkunduIsaretle()
        {
            var kullaniciId = GetCurrentUserId();
            var markedCount = await _bildirimService.TumunuOkunduIsaretleAsync(kullaniciId);

            return Ok(ApiResponse<object>.Ok(
                new { markedCount },
                "Tum bildirimler okundu olarak isaretlendi.",
                HttpContext.TraceIdentifier));
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Kullanici dogrulanamadi.");
            }

            return userId;
        }
    }
}
