using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces
{
    public interface IBildirimRepository : IBaseRepository<Bildirim>
    {
        Task<PagedResult<Bildirim>> GetKullaniciBildirimleriAsync(string kullaniciId, bool sadeceOkunmamis, int page, int pageSize);
        Task<int> GetOkunmamisSayisiAsync(string kullaniciId);
        Task<Bildirim?> GetKullaniciBildirimiAsync(long bildirimId, string kullaniciId);
        Task<int> TumunuOkunduIsaretleAsync(string kullaniciId, DateTime okunmaTarihi);
    }
}
