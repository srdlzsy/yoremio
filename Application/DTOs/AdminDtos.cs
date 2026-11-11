using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class AdminKullaniciQuery
    {
        public string? Q { get; set; }
        public string? Rol { get; set; }
        public bool SadeceBekleyenSaticilar { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminKullaniciDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool KilitliMi { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public string? MagazaAdi { get; set; }
        public string? VergiNo { get; set; }
        public string? Sehir { get; set; }
        public string? Ilce { get; set; }
        public bool? SaticiAktifMi { get; set; }
        public bool DogrulanmisSatici { get; set; }
        public bool? AdminDogrulamaDurumu { get; set; }
        public int UrunSayisi { get; set; }
        public DateTime? KayitTarihi { get; set; }
    }

    public class AdminSaticiDogrulamaDto
    {
        public bool? DogrulandiMi { get; set; }

        [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Not { get; set; }
    }

    public class AdminDurumGuncelleDto
    {
        public bool AktifMi { get; set; }

        [MaxLength(500, ErrorMessage = "Sebep en fazla 500 karakter olabilir.")]
        public string? Sebep { get; set; }
    }

    public class AdminUrunQuery
    {
        public string? Q { get; set; }
        public int? KategoriId { get; set; }
        public string? SaticiId { get; set; }
        public bool? AktifMi { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminYorumQuery
    {
        public int? UrunId { get; set; }
        public string? Q { get; set; }
        public bool? AktifMi { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminYorumDto
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string? UrunAdi { get; set; }
        public string KullaniciId { get; set; } = string.Empty;
        public string? KullaniciAdi { get; set; }
        public string? KullaniciEmail { get; set; }
        public string Icerik { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public bool AktifMi { get; set; }
    }
}
