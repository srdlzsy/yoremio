using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IYorumServices
    {
        // YorumService ile ilgili özel metodlar buraya eklenebilir
        // Örneğin, ürün ID'sine göre yorumları getirme gibi
        Task<IEnumerable<YorumDto>> GetYorumlarByUrunIdAsync(int urunId);

        // Yorum ekleme metodu
        Task<YorumDto> YorumEkleAsync(YorumEkleDto dto, string kullaniciId);

        // Yorum güncelleme metodu
        Task<YorumEkleDto> GuncelleYorumAsync(YorumEkleDto yorumDto);
        
        // Yorum silme metodu
        Task<bool> SilYorumAsync(int yorumId);

        // Ek metod imzaları buraya eklenebilir

    }
}
