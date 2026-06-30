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
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetCurrentUserId();
            var roles = User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var result = await _dashboardService.GetSummaryAsync(userId, roles);
            return Ok(ApiResponse<DashboardSummaryDto>.Ok(result, "Kullanici ozeti getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpGet("satici")]
        public async Task<IActionResult> GetSaticiDashboard()
        {
            var saticiId = GetCurrentUserId();
            var result = await _dashboardService.GetSaticiDashboardAsync(saticiId);
            return Ok(ApiResponse<SaticiDashboardDto>.Ok(result, "Satici dashboard ozeti getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var result = await _dashboardService.GetAdminDashboardAsync();
            return Ok(ApiResponse<AdminDashboardDto>.Ok(result, "Admin dashboard ozeti getirildi.", HttpContext.TraceIdentifier));
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
