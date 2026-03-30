using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class YorumEkleDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün seçilmelidir.")]
        public int UrunId { get; set; }

        [Required(ErrorMessage = "Yorum içeriği boş olamaz.")]
        [MinLength(3, ErrorMessage = "Yorum en az 3 karakter olmalıdır.")]
        public string Icerik { get; set; } = null!;
    }
}
