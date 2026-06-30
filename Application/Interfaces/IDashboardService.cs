using Application.DTOs;

namespace Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(string userId, IReadOnlyCollection<string> roles);
        Task<SaticiDashboardDto> GetSaticiDashboardAsync(string saticiId);
        Task<AdminDashboardDto> GetAdminDashboardAsync();
    }
}
