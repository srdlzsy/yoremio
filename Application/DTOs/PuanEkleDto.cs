using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PuanEkleDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün seçilmelidir.")]
        public int UrunId { get; set; }

        [Range(1, 5, ErrorMessage = "Puan değeri 1 ile 5 arasında olmalıdır.")]
        public int PuanDegeri { get; set; }
    }
}
