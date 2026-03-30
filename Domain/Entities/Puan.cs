using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public  class Puan
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public Urun Urun { get; set; } = null!;
        public string KullaniciId { get; set; } = null!;
        public ApplicationUser Kullanici { get; set; } = null!;
        public int PuanDegeri { get; set; }
        public DateTime PuanTarihi { get; set; } = DateTime.UtcNow;
    }
}
