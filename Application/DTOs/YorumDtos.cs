using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class YorumDto
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string Icerik { get; set; } = null!;
        public DateTime Tarih { get; set; }
        public string KullaniciId { get; set; } = null!;
        public string? KullaniciAdi { get; set; }
    }

}
