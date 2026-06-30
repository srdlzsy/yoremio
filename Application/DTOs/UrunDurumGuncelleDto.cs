using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UrunDurumGuncelleDto
    {
        [Required]
        public bool? AktifMi { get; set; }
    }
}
