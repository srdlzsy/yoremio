namespace Domain.Entities
{
    public class SaticiTakip
    {
        public int Id { get; set; }
        public string AliciId { get; set; } = null!;
        public string SaticiId { get; set; } = null!;
        public bool BildirimAcikMi { get; set; } = true;
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public ApplicationUser? Alici { get; set; }
        public SaticiProfili? Satici { get; set; }
    }
}
