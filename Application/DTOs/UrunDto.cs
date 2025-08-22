using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class UrunDto
    {
        public int Id { get; set; } 
        public string Adi { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
        public int StokMiktari { get; set; }
        public int KategoriId { get; set; }
        public string SaticiId { get; set; } = string.Empty;
        public List<UrunResimDto>? Resimler { get; set; }
        public List<UrunVideoDto>? Videolar { get; set; }
    }

}
