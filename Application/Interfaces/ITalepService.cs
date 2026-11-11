using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces
{
    public interface ITalepService
    {
        Task<TalepDto> TalepOlusturAsync(string aliciId, TalepOlusturDto dto);
        Task<TalepDto> GetTalepByIdAsync(int talepId, string userId);
        Task<IReadOnlyCollection<TalepDto>> GetAliciTalepleriAsync(string aliciId, string? durum = null);
        Task<PagedResult<TalepDto>> GetAliciTalepleriPagedAsync(string aliciId, string? durum, int page, int pageSize);
        Task<IReadOnlyCollection<TalepDto>> GetSaticiTalepleriAsync(string saticiId, string? durum = null);
        Task<PagedResult<TalepDto>> GetSaticiTalepleriPagedAsync(string saticiId, string? durum, int page, int pageSize);
        Task<TalepTeklifDto> TeklifVerAsync(int talepId, string saticiId, TalepTeklifOlusturDto dto);
        Task<TalepDto> TeklifKabulEtAsync(int teklifId, string aliciId);
        Task<TalepDto> TeklifReddetAsync(int teklifId, string aliciId);
        Task<TalepDto> TalepIptalEtAsync(int talepId, string aliciId);
    }
}
