using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class DestekTalepDto
    {
        [Required(ErrorMessage = "Konu zorunludur.")]
        [MaxLength(160, ErrorMessage = "Konu en fazla 160 karakter olabilir.")]
        public string Konu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj zorunludur.")]
        [MaxLength(4000, ErrorMessage = "Mesaj en fazla 4000 karakter olabilir.")]
        public string Mesaj { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Gecerli bir email adresi giriniz.")]
        [MaxLength(256, ErrorMessage = "Email en fazla 256 karakter olabilir.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Gecerli bir telefon numarasi giriniz.")]
        [MaxLength(32, ErrorMessage = "Telefon en fazla 32 karakter olabilir.")]
        public string? Telefon { get; set; }

        [MaxLength(120, ErrorMessage = "Ekran en fazla 120 karakter olabilir.")]
        public string? Ekran { get; set; }

        [MaxLength(80, ErrorMessage = "Ilgili varlik turu en fazla 80 karakter olabilir.")]
        public string? IlgiliVarlikTuru { get; set; }

        [MaxLength(120, ErrorMessage = "Ilgili varlik id en fazla 120 karakter olabilir.")]
        public string? IlgiliVarlikId { get; set; }
    }

    public class DestekTalepAlindiDto
    {
        public string TalepId { get; set; } = string.Empty;
        public DateTime AlinmaTarihi { get; set; }
    }
}
