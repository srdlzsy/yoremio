using System.ComponentModel.DataAnnotations;
using Application.Validation;

namespace Application.DTOs
{
    public class OrtakAlimOlusturDto
    {
        public string? SaticiId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir urun secilmelidir.")]
        public int? UrunId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir kategori secilmelidir.")]
        public int? KategoriId { get; set; }

        [Required(ErrorMessage = "Ortak alim basligi zorunludur.")]
        [MinLength(4, ErrorMessage = "Baslik en az 4 karakter olmalidir.")]
        [MaxLength(150, ErrorMessage = "Baslik en fazla 150 karakter olabilir.")]
        public string Baslik { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Aciklama en fazla 1000 karakter olabilir.")]
        public string? Aciklama { get; set; }

        [Required(ErrorMessage = "Mensei sehri zorunludur.")]
        [MaxLength(60)]
        public string MenseiSehir { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hedef teslimat sehri zorunludur.")]
        [MaxLength(60)]
        public string HedefSehir { get; set; } = string.Empty;

        [PositiveDecimal(ErrorMessage = "Birim fiyat 0'dan buyuk olmalidir.")]
        public decimal BirimFiyat { get; set; }

        [MaxLength(30)]
        public string Birim { get; set; } = "kg";

        [Range(2, 1000, ErrorMessage = "Minimum katilimci sayisi 2-1000 arasinda olmalidir.")]
        public int MinimumKatilimci { get; set; } = 10;

        public bool SogukZincirGerekliMi { get; set; }

        [MaxLength(500)]
        public string? TeslimatKosullari { get; set; }

        [MaxLength(500)]
        public string? IptalKosullari { get; set; }

        public DateTime SonKatilimTarihi { get; set; } = DateTime.UtcNow.AddDays(7);
    }

    public class OrtakAlimKatilDto
    {
        [Range(1, 1000, ErrorMessage = "Katilim miktari en az 1 olmalidir.")]
        public int KatilimMiktari { get; set; } = 1;

        [MaxLength(300)]
        public string? Not { get; set; }
    }

    public class OrtakAlimDurumGuncelleDto
    {
        [Required(ErrorMessage = "Durum zorunludur.")]
        [MaxLength(30)]
        public string Durum { get; set; } = string.Empty;
    }

    public class OrtakAlimDto
    {
        public int Id { get; set; }
        public string SaticiId { get; set; } = string.Empty;
        public string? SaticiMagazaAdi { get; set; }
        public bool SaticiDogrulanmis { get; set; }
        public string? OlusturanKullaniciId { get; set; }
        public int? UrunId { get; set; }
        public string? UrunAdi { get; set; }
        public string? UrunResimUrl { get; set; }
        public int? KategoriId { get; set; }
        public string? KategoriAdi { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public string MenseiSehir { get; set; } = string.Empty;
        public string HedefSehir { get; set; } = string.Empty;
        public decimal BirimFiyat { get; set; }
        public string Birim { get; set; } = "kg";
        public int MinimumKatilimci { get; set; }
        public int MevcutKatilimci { get; set; }
        public int ToplamTalepMiktari { get; set; }
        public bool SogukZincirGerekliMi { get; set; }
        public string? TeslimatKosullari { get; set; }
        public string? IptalKosullari { get; set; }
        public string Durum { get; set; } = string.Empty;
        public DateTime SonKatilimTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
