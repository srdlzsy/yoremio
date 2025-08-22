using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YorumController : ControllerBase
    {
        private readonly IServiceManager  _serviceManager;

        public YorumController( IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> YorumEkle([FromBody] YorumEkleDto dto)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (kullaniciId == null)
                return Unauthorized();

            var result = await _serviceManager.YorumService.YorumEkleAsync(dto, kullaniciId);
            return Ok(result);
        }

        [HttpGet("{urunId}")]
        public async Task<IActionResult> GetYorumlarByUrunId(int urunId)
        {
            var yorumlar = await _serviceManager.YorumService.GetYorumlarByUrunIdAsync(urunId);
            return Ok(yorumlar);
        }
    }
}

