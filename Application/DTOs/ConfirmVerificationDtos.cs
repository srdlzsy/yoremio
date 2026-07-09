using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ConfirmEmailDto
    {
        [Required(ErrorMessage = "Email bos olamaz.")]
        [EmailAddress(ErrorMessage = "Gecerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dogrulama kodu bos olamaz.")]
        public string Code { get; set; } = string.Empty;
    }
}
