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
    public class PlanliGonderimController : ControllerBase
    {
        private readonly IPlanliGonderimService _planliGonderimService;

        public PlanliGonderimController(IPlanliGonderimService planliGonderimService)
        {
            _planliGonderimService = planliGonderimService;
        }

        /// <summary>
        /// "Sehrime Gelen Urunler": hedef sehre veya mensei sehre gore planlanmis gelecek uretici gonderimlerini listeler.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetUpcoming(
            [FromQuery] string? hedefSehir,
            [FromQuery] string? menseiSehir,
            [FromQuery] string? saticiId,
            [FromQuery] int take = 20)
        {
            var items = await _planliGonderimService.GetUpcomingAsync(hedefSehir, menseiSehir, saticiId, take);
            return Ok(ApiResponse<IReadOnlyCollection<PlanliGonderimDto>>.Ok(
                items,
                "Planli gonderimler listelendi.",
                HttpContext.TraceIdentifier));
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRoles.Satici)]
        public async Task<IActionResult> Create([FromBody] PlanliGonderimOlusturDto dto)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Kullanici dogrulanamadi.");

            var created = await _planliGonderimService.OlusturAsync(saticiId, dto);
            return Ok(ApiResponse<PlanliGonderimDto>.Ok(
                created,
                "Planli gonderim olusturuldu.",
                HttpContext.TraceIdentifier));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = ApplicationRoles.Satici)]
        public async Task<IActionResult> Cancel(int id)
        {
            var saticiId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Kullanici dogrulanamadi.");

            await _planliGonderimService.IptalEtAsync(id, saticiId);
            return Ok(ApiResponse<object>.Ok(
                new { id, iptalEdildi = true },
                "Planli gonderim iptal edildi.",
                HttpContext.TraceIdentifier));
        }
    }
}
