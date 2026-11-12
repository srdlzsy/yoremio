namespace Domain.Entities
{
    public class OrtakAlimKatilim
    {
        public int Id { get; set; }
        public int OrtakAlimId { get; set; }
        public string AliciId { get; set; } = null!;
        public int KatilimMiktari { get; set; } = 1;
        public string? Not { get; set; }
        public bool AktifMi { get; set; } = true;
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public OrtakAlim? OrtakAlim { get; set; }
        public ApplicationUser? Alici { get; set; }
    }
}
