namespace Domain.Entities
{
    public class UrunFavori
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string KullaniciId { get; set; } = null!;
        public DateTime EklenmeTarihi { get; set; } = DateTime.UtcNow;

        public Urun? Urun { get; set; }
        public ApplicationUser? Kullanici { get; set; }
    }
}
