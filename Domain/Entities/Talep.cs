namespace Domain.Entities
{
    public class Talep
    {
        public int Id { get; set; }
        public string AliciId { get; set; } = null!;
        public int UrunId { get; set; }
        public int Miktar { get; set; }
        public string? Not { get; set; }
        public string Durum { get; set; } = Domain.Constants.TalepDurumlari.Acik;
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public DateTime? GuncellemeTarihi { get; set; }

        public ApplicationUser? Alici { get; set; }
        public Urun? Urun { get; set; }
        public List<TalepTeklif> Teklifler { get; set; } = new();
    }
}
