using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("kullanicilar")]
        public async Task<IActionResult> GetKullanicilar([FromQuery] AdminKullaniciQuery query)
        {
            var result = await _adminService.GetKullanicilarAsync(query);
            return Ok(ApiResponse<PagedResult<AdminKullaniciDto>>.Ok(result, "Kullanıcılar getirildi.", HttpContext.TraceIdentifier));
        }

        [HttpPatch("saticilar/{saticiId}/dogrulama")]
        public async Task<IActionResult> SetSaticiDogrulama(string saticiId, [FromBody] AdminSaticiDogrulamaDto dto)
        {
            var result = await _adminService.SetSaticiDogrulamaAsync(saticiId, dto);
            return Ok(ApiResponse<AdminKullaniciDto>.Ok(result, "Satıcı doğrulama durumu güncellendi.", HttpContext.TraceIdentifier));
        }

        [HttpPatch("saticilar/{saticiId}/durum")]
        public async Task<IActionResult> SetSaticiDurum(string saticiId, [FromBody] AdminDurumGuncelleDto dto)
        {
            var result = await _adminService.SetSaticiDurumAsync(saticiId, dto);
            return Ok(ApiResponse<AdminKullaniciDto>.Ok(result, "Satıcı durumu güncellendi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("urunler")]
        public async Task<IActionResult> GetUrunler([FromQuery] AdminUrunQuery query)
        {
            var result = await _adminService.GetUrunlerAsync(query);
            return Ok(ApiResponse<PagedResult<UrunDto>>.Ok(result, "Ürünler getirildi.", HttpContext.TraceIdentifier));
        }

        [HttpPatch("urunler/{urunId:int}/durum")]
        public async Task<IActionResult> SetUrunDurum(int urunId, [FromBody] AdminDurumGuncelleDto dto)
        {
            var result = await _adminService.SetUrunDurumAsync(urunId, dto);
            return Ok(ApiResponse<UrunDto>.Ok(result, "Ürün durumu güncellendi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("yorumlar")]
        public async Task<IActionResult> GetYorumlar([FromQuery] AdminYorumQuery query)
        {
            var result = await _adminService.GetYorumlarAsync(query);
            return Ok(ApiResponse<PagedResult<AdminYorumDto>>.Ok(result, "Yorumlar getirildi.", HttpContext.TraceIdentifier));
        }

        [HttpPatch("yorumlar/{yorumId:int}/durum")]
        public async Task<IActionResult> SetYorumDurum(int yorumId, [FromBody] AdminDurumGuncelleDto dto)
        {
            var result = await _adminService.SetYorumDurumAsync(yorumId, dto);
            return Ok(ApiResponse<AdminYorumDto>.Ok(result, "Yorum durumu güncellendi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("talep-bosluklari")]
        public async Task<IActionResult> GetTalepBosluklari([FromQuery] int top = 20)
        {
            var result = await _adminService.GetTalepBosluklariAsync(top);
            return Ok(ApiResponse<IReadOnlyCollection<TalepBosluguDto>>.Ok(result, "Bölgesel talep boşlukları getirildi.", HttpContext.TraceIdentifier));
        }
    }
}
