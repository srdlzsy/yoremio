using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class AliciProfilDto
    {
        public string KullaniciId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Adres { get; set; }
        public string? Sehir { get; set; }
        public string? Ilce { get; set; }
        public string? FavoriKategori { get; set; }
        public DateTime KayitTarihi { get; set; }
        public bool AktifMi { get; set; }
    }

    public class AliciProfilGuncelleDto
    {
        [MinLength(3, ErrorMessage = "Kullanıcı adı en az 3 karakter olmalıdır.")]
        [MaxLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        public string? UserName { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string? PhoneNumber { get; set; }

        [MaxLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
        public string? Adres { get; set; }

        [MaxLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir.")]
        public string? Sehir { get; set; }

        [MaxLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
        public string? Ilce { get; set; }

        [MaxLength(100, ErrorMessage = "Favori kategori en fazla 100 karakter olabilir.")]
        public string? FavoriKategori { get; set; }
    }
}
