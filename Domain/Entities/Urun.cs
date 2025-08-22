using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Urun
    {
        public int Id { get; set; }

        public string Adi { get; set; } = string.Empty;

        public string Aciklama { get; set; } = string.Empty;

        public decimal Fiyat { get; set; }

        public int StokMiktari { get; set; }

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public DateTime? GuncellemeTarihi { get; set; }

        public bool AktifMi { get; set; } = true;

    


        // ===== KATEGORİ =====
        public int KategoriId { get; set; }

        public Kategori? Kategori { get; set; }

        // ===== SATICI (ApplicationUser ya da SaticiProfili) =====
        public string SaticiId { get; set; } = null!;  // ApplicationUser.Id veya SaticiProfili.KullaniciId

      //  [JsonIgnore]
        public SaticiProfili Satici { get; set; } = null!; // SaticiProfili tablosuyla ilişki
        public List<UrunResim> Resimler { get; set; } = new();
        public List<UrunVideo> Videolar { get; set; } = new();
        public List<Yorum> yorumlar { get; set; } = new();

        public List<Puan> Puanlar { get; set; } = new();


    }
}
