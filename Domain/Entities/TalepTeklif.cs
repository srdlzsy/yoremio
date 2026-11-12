namespace Domain.Entities
{
    public class TalepTeklif
    {
        public int Id { get; set; }
        public int TalepId { get; set; }
        public string SaticiId { get; set; } = null!;
        public decimal? BirimFiyat { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public int? TeslimatSuresiGun { get; set; }
        public bool KargoDahilMi { get; set; } = true;
        public string? GonderimSehir { get; set; }
        public string Durum { get; set; } = Domain.Constants.TalepTeklifDurumlari.Beklemede;
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public DateTime? GuncellemeTarihi { get; set; }

        public Talep? Talep { get; set; }
        public ApplicationUser? Satici { get; set; }
    }
}
