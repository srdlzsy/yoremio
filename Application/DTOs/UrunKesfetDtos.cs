using Domain.Models;

namespace Application.DTOs
{
    public class StokAlarmiDto
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string UrunAdi { get; set; } = string.Empty;
        public string? UrunResimUrl { get; set; }
        public decimal UrunFiyat { get; set; }
        public int StokMiktari { get; set; }
        public bool AktifMi { get; set; }
        public DateTime? SonTetiklemeTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }

    public class BenimIcinBulOneriDto
    {
        public bool UrunBulunamadi { get; set; }
        public string? ArananUrunAdi { get; set; }
        public string? TeslimatSehir { get; set; }
        public int? KategoriId { get; set; }
        public string? AcikTalepEndpoint { get; set; }
    }

    public class AkilliAlternatiflerDto
    {
        public IReadOnlyCollection<UrunDto> AyniYoredenUrunler { get; set; } = Array.Empty<UrunDto>();
        public IReadOnlyCollection<UrunDto> SehreGonderimYapanlar { get; set; } = Array.Empty<UrunDto>();
        public IReadOnlyCollection<UrunDto> StoguTukenenUrunler { get; set; } = Array.Empty<UrunDto>();
        public IReadOnlyCollection<OrtakAlimDto> AktifOrtakAlimlar { get; set; } = Array.Empty<OrtakAlimDto>();
        public IReadOnlyCollection<PlanliGonderimDto> PlanliGonderimler { get; set; } = Array.Empty<PlanliGonderimDto>();
        public BenimIcinBulOneriDto BenimIcinBulOnerisi { get; set; } = new();
    }

    public class UrunKesfetSonucDto
    {
        public PagedResult<UrunDto> Sonuclar { get; set; } = new();
        public AkilliAlternatiflerDto? Alternatifler { get; set; }
    }
}
