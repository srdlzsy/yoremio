using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ResendVerificationDto
    {
        [Required(ErrorMessage = "Email bos olamaz.")]
        [EmailAddress(ErrorMessage = "Gecerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
    }
}
