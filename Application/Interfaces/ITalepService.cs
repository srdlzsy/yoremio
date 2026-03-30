using Application.DTOs;

namespace Application.Interfaces
{
    public interface ITalepService
    {
        Task<TalepDto> TalepOlusturAsync(string aliciId, TalepOlusturDto dto);
        Task<IReadOnlyCollection<TalepDto>> GetAliciTalepleriAsync(string aliciId);
        Task<IReadOnlyCollection<TalepDto>> GetSaticiTalepleriAsync(string saticiId);
        Task<TalepTeklifDto> TeklifVerAsync(int talepId, string saticiId, TalepTeklifOlusturDto dto);
        Task<TalepDto> TeklifKabulEtAsync(int teklifId, string aliciId);
    }
}
