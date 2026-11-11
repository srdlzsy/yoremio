using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class RegisterAliciDto
    {
        [Required(ErrorMessage = "Email boş olamaz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifre boş olamaz.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalı.")]
        public string Password { get; set; } = null!;

        [MinLength(3, ErrorMessage = "Kullanıcı adı en az 3 karakter olmalıdır.")]
        [MaxLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        public string? UserName { get; set; }

        [MaxLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir.")]
        public string? Sehir { get; set; }

        [MaxLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
        public string? Ilce { get; set; }

        [MaxLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
        public string? Adres { get; set; }
    }
}
