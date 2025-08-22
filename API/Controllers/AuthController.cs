using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IServiceManager _serviceManager;

        public AuthController(IAuthService authService,IServiceManager serviceManager)
        {
            _authService = authService;
            _serviceManager = serviceManager;
        }

        [HttpPost("register/satici")]
        public async Task<IActionResult> RegisterSatici([FromBody] RegisterSaticiDto dto)
        {
            var result = await _authService.RegisterSaticiAsync(dto);

            if (!result.Succeeded)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Satıcı kaydı başarılı." });
        }

        [HttpPost("register/alici")]
        public async Task<IActionResult> RegisterAlici([FromBody] RegisterAliciDto dto)
        {
            var result = await _authService.RegisterAliciAsync(dto);

            if (!result.Succeeded)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Alıcı kaydı başarılı." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Succeeded)
                return Unauthorized(new { error = result.Error });

            return Ok(new { token = result.Token });
        }

 

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailAsync(userId, token);

            if (!result)
                return BadRequest(new { error = "Email doğrulama başarısız." });

            var satici = await _serviceManager.SaticiProfiliService.GetSaticiWithUserByIdAsync(userId);

            if (satici != null)
            {
                satici.AktifMi = true;
                await _serviceManager.SaticiProfiliService.UpdateAsync(satici);
            }

            return Ok(new { message = "Email başarıyla doğrulandı." });
        }


        [HttpGet("confirm-phone")]
        public async Task<IActionResult> ConfirmPhone([FromQuery] string userId, [FromQuery] string token)
        {
            var result = await _authService.ConfirmPhoneAsync(userId, token);

            if (!result)
                return BadRequest(new { error = "Telefon doğrulama başarısız." });

            return Ok(new { message = "Telefon başarıyla doğrulandı." });
        }
    }
}
