using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class SaticiProfili
    {
        [Key]
        [Required]
        public string KullaniciId { get; set; } = null!; // ApplicationUser.Id ile birebir

        [Required]
        [StringLength(100)]
        public string MagazaAdi { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string VergiNo { get; set; } = null!;

        [StringLength(100)]
        public string? Adres { get; set; }

        [StringLength(100)]
        public string? Sehir { get; set; }

        [StringLength(100)]
        public string? Ilce { get; set; }

     
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

        public bool AktifMi { get; set; } = false;

        [ForeignKey(nameof(KullaniciId))]
        public virtual ApplicationUser? Kullanici { get; set; }

        public List<Urun> Urunler { get; set; } = new();
    }
}
