using Application.DTOs;

namespace Application.Interfaces
{
    public interface IBildirimService
    {
        Task<BildirimDto> BildirimOlusturAsync(BildirimOlusturDto dto);
        Task<BildirimPagedResultDto> GetBildirimlerAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize);
        Task<int> GetOkunmamisSayisiAsync(string kullaniciId);
        Task<BildirimDto> OkunduIsaretleAsync(long bildirimId, string kullaniciId);
        Task<int> TumunuOkunduIsaretleAsync(string kullaniciId);
    }
}
