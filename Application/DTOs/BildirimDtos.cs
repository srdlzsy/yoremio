using Domain.Models;

namespace Application.DTOs
{
    public class BildirimDto
    {
        public long Id { get; set; }
        public string Tur { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public string? IlgiliVarlikTuru { get; set; }
        public string? IlgiliVarlikId { get; set; }
        public string? AksiyonUrl { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? OkunmaTarihi { get; set; }
        public bool OkunduMu => OkunmaTarihi.HasValue;
    }

    public class BildirimOlusturDto
    {
        public string KullaniciId { get; set; } = string.Empty;
        public string Tur { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public string? IlgiliVarlikTuru { get; set; }
        public string? IlgiliVarlikId { get; set; }
        public string? AksiyonUrl { get; set; }
    }

    public class BildirimPagedResultDto : PagedResult<BildirimDto>
    {
        public int OkunmamisSayisi { get; set; }
    }
}
