using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class RegisterSaticiDto
    {
        [Required(ErrorMessage = "Email boş olamaz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Telefon numarası boş olamaz.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Şifre boş olamaz.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Mağaza adı zorunludur.")]
        [MinLength(3, ErrorMessage = "Mağaza adı en az 3 karakter olmalıdır.")]
        public string MagazaAdi { get; set; } = null!;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        public string VergiNo { get; set; } = null!;

        public string? Adres { get; set; } = null!;
        public string? Sehir { get; set; }
        public string? Ilce { get; set; }
    }
}
