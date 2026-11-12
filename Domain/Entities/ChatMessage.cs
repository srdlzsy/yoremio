namespace Domain.Entities
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public int? UrunId { get; set; }
        public int? TalepId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        public ApplicationUser? Sender { get; set; }
        public ApplicationUser? Receiver { get; set; }
        public Urun? Urun { get; set; }
        public Talep? Talep { get; set; }
    }
}
