using System.ComponentModel.DataAnnotations;

namespace API.Options
{
    public sealed class JwtOptions
    {
        [Required(ErrorMessage = "Jwt:Key ayarı zorunludur.")]
        [MinLength(32, ErrorMessage = "Jwt:Key en az 32 karakter olmalıdır.")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jwt:Issuer ayarı zorunludur.")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jwt:Audience ayarı zorunludur.")]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 1440, ErrorMessage = "Jwt:ExpireMinutes 1 ile 1440 arasında olmalıdır.")]
        public int ExpireMinutes { get; set; }
    }
}
