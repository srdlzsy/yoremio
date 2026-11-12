namespace Domain.Entities
{
    public class FiyatAlarmi
    {
        public int Id { get; set; }
        public string AliciId { get; set; } = null!;
        public int UrunId { get; set; }
        public decimal HedefFiyat { get; set; }
        public bool AktifMi { get; set; } = true;
        public decimal? SonTetiklenenFiyat { get; set; }
        public DateTime? SonTetiklemeTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public ApplicationUser? Alici { get; set; }
        public Urun? Urun { get; set; }
    }
}
