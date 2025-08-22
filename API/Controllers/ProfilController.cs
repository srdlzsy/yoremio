using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // JWT kimlik doğrulama zorunlu
    public class ProfilController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ProfilController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet("satici")]
        public async Task<IActionResult> GetSaticiProfil()
        {
            // JWT içinden kullanıcı ID’si alınır
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Kullanıcı doğrulanamadı." });

            // Satıcı profili servisten çekilir
            var satici = await _serviceManager.SaticiProfiliService.GetSaticiWithUserByIdAsync(userId);

            if (satici == null)
                return NotFound(new { error = "Profil bulunamadı." });

            // Dönülecek response oluşturulur
            var response = new
            {
                satici.KullaniciId,
                satici.MagazaAdi,
                satici.VergiNo,
                satici.KayitTarihi,
                satici.AktifMi,
                satici.Kullanici?.Email,
                satici.Kullanici?.UserName,
                satici.Kullanici?.PhoneNumber,
            };

            return Ok(response);
        }
    }
}
