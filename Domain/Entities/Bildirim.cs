namespace Domain.Entities
{
    public class Bildirim
    {
        public long Id { get; set; }
        public string KullaniciId { get; set; } = null!;
        public string Tur { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public string? IlgiliVarlikTuru { get; set; }
        public string? IlgiliVarlikId { get; set; }
        public string? AksiyonUrl { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public DateTime? OkunmaTarihi { get; set; }

        public ApplicationUser? Kullanici { get; set; }
    }
}
