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

        [StringLength(1000)]
        public string? Aciklama { get; set; }

        [StringLength(500)]
        public string? KapakResimUrl { get; set; }

        /// <summary>
        /// null: isletme + uretici + e-posta dogrulama kurali, true: admin onayli, false: admin red/iptal.
        /// </summary>
        public bool? AdminDogrulandiMi { get; set; }

        public bool IsletmeDogrulandiMi { get; set; } = false;

        public bool UreticiDogrulandiMi { get; set; } = false;

        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

        public bool AktifMi { get; set; } = false;

        [StringLength(20)]
        public string PaketTuru { get; set; } = Domain.Constants.SaticiPaketTurleri.Ucretsiz;

        [ForeignKey(nameof(KullaniciId))]
        public virtual ApplicationUser? Kullanici { get; set; }

        public List<Urun> Urunler { get; set; } = new();

        public static bool IsVerified(SaticiProfili? satici)
        {
            if (satici is null)
            {
                return false;
            }

            if (satici.AdminDogrulandiMi.HasValue)
            {
                return satici.AdminDogrulandiMi.Value;
            }

            return !string.IsNullOrWhiteSpace(satici.VergiNo)
                && satici.Kullanici is not null
                && satici.Kullanici.EmailConfirmed
                && satici.IsletmeDogrulandiMi
                && satici.UreticiDogrulandiMi;
        }
    }
}
