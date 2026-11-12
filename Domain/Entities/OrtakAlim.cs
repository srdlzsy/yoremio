namespace Domain.Entities
{
    public class OrtakAlim
    {
        public int Id { get; set; }
        public string SaticiId { get; set; } = null!;
        public string? OlusturanKullaniciId { get; set; }
        public int? UrunId { get; set; }
        public int? KategoriId { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public string MenseiSehir { get; set; } = string.Empty;
        public string HedefSehir { get; set; } = string.Empty;
        public decimal BirimFiyat { get; set; }
        public string Birim { get; set; } = "kg";
        public int MinimumKatilimci { get; set; } = 10;
        public bool SogukZincirGerekliMi { get; set; }
        public string? TeslimatKosullari { get; set; }
        public string? IptalKosullari { get; set; }
        public string Durum { get; set; } = Domain.Constants.OrtakAlimDurumlari.Acik;
        public DateTime SonKatilimTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public DateTime? GuncellemeTarihi { get; set; }

        public SaticiProfili? Satici { get; set; }
        public Urun? Urun { get; set; }
        public Kategori? Kategori { get; set; }
        public List<OrtakAlimKatilim> Katilimlar { get; set; } = new();
    }
}
