using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
   public interface IUrunRepository : IBaseRepository<Urun>
    {
        Task<Urun> EkleUrunAsync(Urun urun, List<string> resimUrlListesi, List<string> videoUrlListesi);




    }
}
