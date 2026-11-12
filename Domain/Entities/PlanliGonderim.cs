using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Ureticinin belirli bir sehre planladigi sevkiyat / teslimat takvimi ("Sehrime Gelen Urunler").
    /// </summary>
    public class PlanliGonderim
    {
        public int Id { get; set; }

        [Required]
        public string SaticiId { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Baslik { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Aciklama { get; set; }

        [Required]
        [StringLength(60)]
        public string MenseiSehir { get; set; } = string.Empty;

        [Required]
        [StringLength(60)]
        public string HedefSehir { get; set; } = string.Empty;

        [StringLength(250)]
        public string? TahminiUrunNotu { get; set; }

        public DateTime GonderimTarihi { get; set; }

        public DateTime SonTalepTarihi { get; set; }

        public bool SogukZincirVarMi { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public SaticiProfili? Satici { get; set; }
    }
}
