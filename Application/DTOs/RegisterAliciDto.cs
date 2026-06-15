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
    }
}
