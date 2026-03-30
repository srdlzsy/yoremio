namespace Application.DTOs
{
    public class PuanDto
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string KullaniciId { get; set; } = string.Empty;
        public int PuanDegeri { get; set; }
        public DateTime PuanTarihi { get; set; }
    }
}
