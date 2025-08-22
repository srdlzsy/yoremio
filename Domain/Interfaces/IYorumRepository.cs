using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IYorumRepository : IBaseRepository<Yorum>
    {
        // YorumRepository ile ilgili özel metodlar buraya eklenebilir
        // Örneğin, ürün ID'sine göre yorumları getirme gibi
        Task<IEnumerable<Yorum>> GetYorumlarByUrunIdAsync(int urunId);
        // Ek metod imzaları buraya eklenebilir
    }
}
