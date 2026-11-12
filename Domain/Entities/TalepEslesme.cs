namespace Domain.Entities
{
    public class TalepEslesme
    {
        public int Id { get; set; }
        public int TalepId { get; set; }
        public string SaticiId { get; set; } = null!;
        public int EslesmeSkoru { get; set; }
        public string? Sebep { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public Talep? Talep { get; set; }
        public SaticiProfili? Satici { get; set; }
    }
}
