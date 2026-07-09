using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DestekController : ControllerBase
    {
        private readonly ILogger<DestekController> _logger;

        public DestekController(ILogger<DestekController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("talep")]
        public IActionResult TalepOlustur([FromBody] DestekTalepDto dto)
        {
            var talepId = $"DST-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30];
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var alinmaTarihi = DateTime.UtcNow;

            _logger.LogInformation(
                "Destek talebi alindi. TalepId: {TalepId}, UserId: {UserId}, Konu: {Konu}, Email: {Email}, Telefon: {Telefon}, Ekran: {Ekran}, IlgiliVarlikTuru: {IlgiliVarlikTuru}, IlgiliVarlikId: {IlgiliVarlikId}",
                talepId,
                userId,
                dto.Konu,
                dto.Email,
                dto.Telefon,
                dto.Ekran,
                dto.IlgiliVarlikTuru,
                dto.IlgiliVarlikId);

            return Ok(ApiResponse<DestekTalepAlindiDto>.Ok(
                new DestekTalepAlindiDto
                {
                    TalepId = talepId,
                    AlinmaTarihi = alinmaTarihi
                },
                "Destek talebiniz alindi.",
                HttpContext.TraceIdentifier));
        }
    }
}
