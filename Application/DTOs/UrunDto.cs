namespace Application.DTOs
{
    public class UrunDto
    {
        public int Id { get; set; }
        public string Adi { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
        public int StokMiktari { get; set; }
        public string? BirimMiktar { get; set; }
        public string? TeslimatSecenekleri { get; set; }
        public bool YeniHasatMi { get; set; }
        public string? UretimSehir { get; set; }
        public string? UretimIlce { get; set; }
        public bool EldenTeslimVarMi { get; set; }
        public bool KargoVarMi { get; set; }
        public string? GonderimSehirleri { get; set; }
        public int GoruntulenmeSayisi { get; set; }
        public string? Slug { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public bool AktifMi { get; set; }
        public int KategoriId { get; set; }
        public string? KategoriAdi { get; set; }
        public string SaticiId { get; set; } = string.Empty;
        public string? SaticiMagazaAdi { get; set; }
        public string? SaticiSehir { get; set; }
        public string? SaticiIlce { get; set; }
        public bool SaticiDogrulanmis { get; set; }
        public double OrtalamaPuan { get; set; }
        public int ToplamPuan { get; set; }
        public int ToplamYorum { get; set; }
        public int ToplamFavori { get; set; }
        public List<YorumDto> Yorumlar { get; set; } = new();
        public List<PuanDto> Puanlar { get; set; } = new();
        public List<UrunResimDto>? Resimler { get; set; }
        public List<UrunVideoDto>? Videolar { get; set; }
    }
}
