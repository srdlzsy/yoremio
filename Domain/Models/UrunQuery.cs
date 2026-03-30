namespace Domain.Models
{
    public class UrunQuery
    {
        private const int MaxPageSize = 100;
        private int _page = 1;
        private int _pageSize = 12;

        public string? Q { get; set; }
        public int? KategoriId { get; set; }
        public decimal? MinFiyat { get; set; }
        public decimal? MaxFiyat { get; set; }
        public string? Sort { get; set; }
        public string? SaticiId { get; set; }
        public string? Sehir { get; set; }
        public string? Ilce { get; set; }
        public bool SadeceAktif { get; set; } = true;
        public bool SadeceStoktaOlanlar { get; set; }
        public double? MinOrtalamaPuan { get; set; }

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 12 : Math.Min(value, MaxPageSize);
        }
    }
}
