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

        [MaxLength(50, ErrorMessage = "Birim/miktar bilgisi en fazla 50 karakter olabilir.")]
        public string? BirimMiktar { get; set; }

        [MaxLength(200, ErrorMessage = "Teslimat seçenekleri en fazla 200 karakter olabilir.")]
        public string? TeslimatSecenekleri { get; set; }

        public bool? YeniHasatMi { get; set; }

        [MaxLength(100, ErrorMessage = "Uretim sehri en fazla 100 karakter olabilir.")]
        public string? UretimSehir { get; set; }

        [MaxLength(100, ErrorMessage = "Uretim ilcesi en fazla 100 karakter olabilir.")]
        public string? UretimIlce { get; set; }

        public bool? EldenTeslimVarMi { get; set; }

        public bool? KargoVarMi { get; set; }

        [MaxLength(500, ErrorMessage = "Gonderim sehirleri en fazla 500 karakter olabilir.")]
        public string? GonderimSehirleri { get; set; }

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
