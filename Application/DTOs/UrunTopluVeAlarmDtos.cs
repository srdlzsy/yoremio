using Application.Validation;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class TopluStokKalemiDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir urun secilmelidir.")]
        public int UrunId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stok miktari sifir veya daha buyuk olmalidir.")]
        public int StokMiktari { get; set; }

        [PositiveDecimal(ErrorMessage = "Fiyat sifirdan buyuk olmalidir.")]
        public decimal? Fiyat { get; set; }

        public bool? YeniHasatMi { get; set; }
    }

    public class TopluStokGuncelleDto
    {
        [Required(ErrorMessage = "En az bir urun kalemi gonderilmelidir.")]
        [MinLength(1, ErrorMessage = "En az bir urun kalemi gonderilmelidir.")]
        public List<TopluStokKalemiDto> Kalemler { get; set; } = new();
    }

    public class FiyatAlarmiOlusturDto
    {
        [PositiveDecimal(ErrorMessage = "Hedef fiyat sifirdan buyuk olmalidir.")]
        public decimal HedefFiyat { get; set; }
    }

    public class FiyatAlarmiDto
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string UrunAdi { get; set; } = string.Empty;
        public string? UrunResimUrl { get; set; }
        public decimal UrunFiyat { get; set; }
        public decimal HedefFiyat { get; set; }
        public bool AktifMi { get; set; }
        public bool TetiklendiMi { get; set; }
        public decimal? SonTetiklenenFiyat { get; set; }
        public DateTime? SonTetiklemeTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
