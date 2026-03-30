using Application.Validation;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UrunGuncelleDto
    {
        [MinLength(3, ErrorMessage = "Ürün adı en az 3 karakter olmalıdır.")]
        public string? Adi { get; set; }

        public string? Aciklama { get; set; }

        [PositiveDecimal(ErrorMessage = "Fiyat sıfırdan büyük olmalıdır.")]
        public decimal? Fiyat { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı sıfır veya daha büyük olmalıdır.")]
        public int? StokMiktari { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Kategori seçimi zorunludur.")]
        public int? KategoriId { get; set; }

        [AllowedContentTypes("image/", ErrorMessage = "Yalnızca resim dosyaları yükleyebilirsiniz.")]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Her resim 5 MB'dan büyük olamaz.")]
        public List<IFormFile>? Resimler { get; set; }

        [AllowedContentTypes("video/", ErrorMessage = "Yalnızca video dosyaları yükleyebilirsiniz.")]
        [MaxFileSize(50 * 1024 * 1024, ErrorMessage = "Her video 50 MB'dan büyük olamaz.")]
        public List<IFormFile>? Videolar { get; set; }
    }
}
