namespace Domain.Entities
{
    public class Talep
    {
        public int Id { get; set; }
        public string AliciId { get; set; } = null!;
        public int? UrunId { get; set; }
        public int? KategoriId { get; set; }
        public string TalepTuru { get; set; } = Domain.Constants.TalepTurleri.Urun;
        public string TalepTipi { get; set; } = Domain.Constants.TalepTipleri.Bireysel;
        public string? ArananUrunAdi { get; set; }
        public int Miktar { get; set; }
        public string? Birim { get; set; }
        public string? TeslimatSehir { get; set; }
        public string? TercihEdilenSehirler { get; set; }
        public decimal? HedefBirimFiyat { get; set; }
        public DateTime? SonTeklifTarihi { get; set; }
        public string? Not { get; set; }
        public string Durum { get; set; } = Domain.Constants.TalepDurumlari.Acik;
        public bool? AliciGerceklestiOnayi { get; set; }
        public bool? SaticiGerceklestiOnayi { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public DateTime? GuncellemeTarihi { get; set; }

        public ApplicationUser? Alici { get; set; }
        public Urun? Urun { get; set; }
        public Kategori? Kategori { get; set; }
        public List<TalepTeklif> Teklifler { get; set; } = new();
        public List<TalepEslesme> Eslesmeler { get; set; } = new();
    }
}
