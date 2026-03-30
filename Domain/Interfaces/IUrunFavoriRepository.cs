using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUrunFavoriRepository : IBaseRepository<UrunFavori>
    {
        Task<UrunFavori?> GetByKullaniciVeUrunAsync(string kullaniciId, int urunId);
        Task<IReadOnlyCollection<Urun>> GetFavoriUrunlerAsync(string kullaniciId);
        Task<IReadOnlyCollection<int>> GetFavoriUrunIdleriAsync(string kullaniciId);
        Task<IReadOnlyCollection<int>> GetKullaniciFavoriKategoriIdleriAsync(string kullaniciId, int take);
    }
}
