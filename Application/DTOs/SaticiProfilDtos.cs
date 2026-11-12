using Application.Validation;
using Microsoft.AspNetCore.Http;
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
        public string? Aciklama { get; set; }
        public string? KapakResimUrl { get; set; }
        public DateTime KayitTarihi { get; set; }
        public bool AktifMi { get; set; }
        public string PaketTuru { get; set; } = Domain.Constants.SaticiPaketTurleri.Ucretsiz;
        public bool DogrulanmisSatici { get; set; }
        public bool? AdminDogrulamaDurumu { get; set; }
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

        [MaxLength(1000, ErrorMessage = "Mağaza açıklaması en fazla 1000 karakter olabilir.")]
        public string? Aciklama { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string? PhoneNumber { get; set; }

        [AllowedContentTypes("image/", ErrorMessage = "Kapak fotoğrafı yalnızca geçerli bir resim dosyası olmalıdır.")]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Kapak fotoğrafı 5 MB'dan büyük olamaz.")]
        public IFormFile? KapakResim { get; set; }
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

    public class SaticiOzetDto
    {
        public string KullaniciId { get; set; } = string.Empty;
        public string MagazaAdi { get; set; } = string.Empty;
        public string? Sehir { get; set; }
        public string? Ilce { get; set; }
        public string? Aciklama { get; set; }
        public bool DogrulanmisSatici { get; set; }
        public int UrunSayisi { get; set; }
        public double OrtalamaPuan { get; set; }
        public int ToplamYorum { get; set; }
        public int ToplamFavori { get; set; }
        public double GuvenSkoru { get; set; }
        public int TakipciSayisi { get; set; }
        public string? KapakResimUrl { get; set; }
        public int? VitrinUrunId { get; set; }
    }

    public class SaticiTakipIstekDto
    {
        public bool BildirimAcikMi { get; set; } = true;
    }
}
