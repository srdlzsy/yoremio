using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Yorum
    {
        public int Id { get; set; }
        public string YorumYapanKullaniciId { get; set; } = null!; // Yorum yapan kullanıcının ID'si
        public int UrunId { get; set; } // Yorumun yapıldığı ürünün ID'si
        public string Icerik { get; set; } = null!; // Yorum içeriği
        public DateTime Tarih { get; set; } = DateTime.UtcNow; // Yorumun yapıldığı tarih
        // Navigation properties
        public virtual ApplicationUser? YorumYapanKullanici { get; set; }
        public virtual Urun? Urun { get; set; }



    }
}
     
