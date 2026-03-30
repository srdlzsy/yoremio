using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class SaticiProfilDto
    {
        public string KullaniciId { get; set; } = string.Empty;
        public string MagazaAdi { get; set; } = string.Empty;
        public string VergiNo { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public string? Sehir { get; set; }
        public string? Ilce { get; set; }
        public DateTime KayitTarihi { get; set; }
        public bool AktifMi { get; set; }
        public bool DogrulanmisSatici { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class SaticiProfilGuncelleDto
    {
        [MinLength(3, ErrorMessage = "Mağaza adı en az 3 karakter olmalıdır.")]
        public string? MagazaAdi { get; set; }

        public string? Adres { get; set; }

        public string? Sehir { get; set; }

        public string? Ilce { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string? PhoneNumber { get; set; }
    }

    public class SaticiGuvenSkoruDto
    {
        public string KullaniciId { get; set; } = string.Empty;
        public string MagazaAdi { get; set; } = string.Empty;
        public bool DogrulanmisSatici { get; set; }
        public int UrunSayisi { get; set; }
        public double OrtalamaPuan { get; set; }
        public int ToplamPuan { get; set; }
        public int ToplamYorum { get; set; }
        public int ToplamFavori { get; set; }
        public double GuvenSkoru { get; set; }
    }
}
