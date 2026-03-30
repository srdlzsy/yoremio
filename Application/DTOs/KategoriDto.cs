using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class KategoriDto
    {
        public int Id { get; set; }
        public string Adi { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
    }

    public class KategoriCreateDto
    {
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [MinLength(2, ErrorMessage = "Kategori adı en az 2 karakter olmalıdır.")]
        public string Adi { get; set; } = string.Empty;

        public string? Aciklama { get; set; }
    }

    public class KategoriUpdateDto
    {
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [MinLength(2, ErrorMessage = "Kategori adı en az 2 karakter olmalıdır.")]
        public string Adi { get; set; } = string.Empty;

        public string? Aciklama { get; set; }
    }
}
