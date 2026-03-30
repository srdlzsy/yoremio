using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class AliciProfili
    {
        [Key]
        [Required]
        public string KullaniciId { get; set; } = null!;

        [StringLength(200)]
        public string? Adres { get; set; }

        [StringLength(100)]
        public string? FavoriKategori { get; set; }

    

        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

        public bool AktifMi { get; set; } = true;


        [ForeignKey("KullaniciId")]
        public virtual ApplicationUser? Kullanici { get; set; }
    }
}
