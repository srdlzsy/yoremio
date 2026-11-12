using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PlanliGonderimOlusturDto
    {
        [Required(ErrorMessage = "Gonderim basligi zorunludur.")]
        [MinLength(4, ErrorMessage = "Baslik en az 4 karakter olmalidir.")]
        [MaxLength(150)]
        public string Baslik { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Aciklama { get; set; }

        [Required(ErrorMessage = "Mensei sehri zorunludur.")]
        [MaxLength(60)]
        public string MenseiSehir { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hedef teslimat sehri zorunludur.")]
        [MaxLength(60)]
        public string HedefSehir { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? TahminiUrunNotu { get; set; }

        public DateTime GonderimTarihi { get; set; }

        public DateTime SonTalepTarihi { get; set; }

        public bool SogukZincirVarMi { get; set; }
    }

    public class PlanliGonderimDto
    {
        public int Id { get; set; }
        public string SaticiId { get; set; } = string.Empty;
        public string? SaticiMagazaAdi { get; set; }
        public bool SaticiDogrulanmis { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public string MenseiSehir { get; set; } = string.Empty;
        public string HedefSehir { get; set; } = string.Empty;
        public string? TahminiUrunNotu { get; set; }
        public DateTime GonderimTarihi { get; set; }
        public DateTime SonTalepTarihi { get; set; }
        public bool SogukZincirVarMi { get; set; }
        public bool AktifMi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
