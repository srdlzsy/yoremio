using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPuanService
    {
        Task<Puan?> GetPuanByUrunIdAndKullaniciIdAsync(int urunId, string kullaniciId);
        Task<IEnumerable<Puan>> GetPuanlarByUrunIdAsync(int urunId);
        Task<IEnumerable<Puan>> GetPuanlarByKullaniciIdAsync(string kullaniciId);
        Task<bool> PuanVarmiAsync(int urunId, string kullaniciId);
        Task<int> GetOrCreatePuanAsync(int urunId, string kullaniciId, int puanDegeri);
        Task<double> GetOrtalamaPuanByUrunIdAsync(int urunId);
    }
    
}
