using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class TalepOlusturDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir urun secilmelidir.")]
        public int UrunId { get; set; }

        [Range(1, 100000, ErrorMessage = "Miktar en az 1 olmalidir.")]
        public int Miktar { get; set; } = 1;

        [MaxLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir.")]
        public string? Not { get; set; }
    }

    public class TalepTeklifOlusturDto
    {
        [Range(0.01, 100000000, ErrorMessage = "Birim fiyat 0'dan buyuk olmalidir.")]
        public decimal? BirimFiyat { get; set; }

        [Required(ErrorMessage = "Teklif mesaji zorunludur.")]
        [MaxLength(1000, ErrorMessage = "Mesaj en fazla 1000 karakter olabilir.")]
        public string Mesaj { get; set; } = string.Empty;
    }

    public class TalepTeklifDto
    {
        public int Id { get; set; }
        public int TalepId { get; set; }
        public string SaticiId { get; set; } = string.Empty;
        public string? SaticiMagazaAdi { get; set; }
        public decimal? BirimFiyat { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public DateTime OlusturmaTarihi { get; set; }
    }

    public class TalepDto
    {
        public int Id { get; set; }
        public string AliciId { get; set; } = string.Empty;
        public int UrunId { get; set; }
        public string? UrunAdi { get; set; }
        public int Miktar { get; set; }
        public string? Not { get; set; }
        public string Durum { get; set; } = string.Empty;
        public DateTime OlusturmaTarihi { get; set; }
        public List<TalepTeklifDto> Teklifler { get; set; } = new();
    }
}
