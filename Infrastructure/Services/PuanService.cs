using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class PuanService : IPuanService
    {
        private readonly IPuanRepository _puanRepository;
        public PuanService(IPuanRepository puanRepository)
        {
            _puanRepository = puanRepository;

        }


        public Task<int> GetOrCreatePuanAsync(int urunId, string kullaniciId, int puanDegeri)
        {
            return _puanRepository.GetOrCreatePuanAsync(urunId, kullaniciId, puanDegeri);
        }

        public Task<double> GetOrtalamaPuanByUrunIdAsync(int urunId)
        {
            return _puanRepository.GetOrtalamaPuanByUrunIdAsync(urunId);
        }

        public Task<Puan?> GetPuanByUrunIdAndKullaniciIdAsync(int urunId, string kullaniciId)
        {
            return _puanRepository.GetPuanByUrunIdAndKullaniciIdAsync(urunId, kullaniciId);
        }

        public Task<IEnumerable<Puan>> GetPuanlarByKullaniciIdAsync(string kullaniciId)
        {
            return _puanRepository.GetPuanlarByKullaniciIdAsync(kullaniciId);
        }

        public Task<IEnumerable<Puan>> GetPuanlarByUrunIdAsync(int urunId)
        {
            return _puanRepository.GetPuanlarByUrunIdAsync(urunId);
        }

        public Task<bool> PuanVarmiAsync(int urunId, string kullaniciId)
        {
            return _puanRepository.PuanVarmiAsync(urunId, kullaniciId);
        }
    }
}
