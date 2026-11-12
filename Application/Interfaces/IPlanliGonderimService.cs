using Application.DTOs;

namespace Application.Interfaces
{
    public interface IPlanliGonderimService
    {
        Task<IReadOnlyCollection<PlanliGonderimDto>> GetUpcomingAsync(string? hedefSehir = null, string? menseiSehir = null, string? saticiId = null, int take = 20);
        Task<PlanliGonderimDto> OlusturAsync(string saticiId, PlanliGonderimOlusturDto dto);
        Task<bool> IptalEtAsync(int id, string saticiId);
    }
}
