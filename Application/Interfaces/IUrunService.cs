using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUrunService
    {
        Task<Urun> UrunEkleAsync(string saticiId, UrunEkleDto dto);
        Task<IEnumerable<UrunDto>> GetAllUrunlerAsync();
        Task<IEnumerable<UrunDto>> GetUrunlerBySaticiIdAsync(string saticiId);
        Task<UrunDto?> GetUrunByIdAsync(int urunId);
        Task<UrunDto> UrunGuncelleAsync(int urunId,string saticiId, UrunGuncelleDto dto);
        Task<bool> UrunSilAsync(int urunId);
        Task<bool> UrunResimSilAsync(int urunId, int resimId);
        Task<bool> UrunVideoSilAsync(int urunId, int videoId);
        Task<Urun?> EkleMedyaToUrunAsync(int urunId, List<string> resimUrlListesi, List<string> videoUrlListesi);



    }
}
