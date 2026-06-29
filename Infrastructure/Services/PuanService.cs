using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class PuanService : IPuanService
    {
        private readonly IPuanRepository _puanRepository;
        private readonly IUrunRepository _urunRepository;
        private readonly ITalepRepository _talepRepository;

        public PuanService(IPuanRepository puanRepository, IUrunRepository urunRepository, ITalepRepository talepRepository)
        {
            _puanRepository = puanRepository;
            _urunRepository = urunRepository;
            _talepRepository = talepRepository;
        }


        public async Task<int> GetOrCreatePuanAsync(int urunId, string kullaniciId, int puanDegeri)
        {
            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null || !urun.AktifMi)
            {
                throw new KeyNotFoundException("Ürün bulunamadı.");
            }

            if (!await _talepRepository.HasAcceptedDemandForProductAsync(kullaniciId, urunId))
            {
                throw new UnauthorizedAccessException("Puan verebilmek icin bu urunle ilgili kabul edilmis bir talebiniz olmalidir.");
            }

            return await _puanRepository.GetOrCreatePuanAsync(urunId, kullaniciId, puanDegeri);
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
