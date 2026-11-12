using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class TalepOlusturDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir urun secilmelidir.")]
        public int UrunId { get; set; }

        [Range(1, 100000, ErrorMessage = "Miktar en az 1 olmalidir.")]
        public int Miktar { get; set; } = 1;

        [MaxLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir.")]
        public string? Not { get; set; }
    }

    public class AcikTalepOlusturDto
    {
        [Required(ErrorMessage = "Aranan urun adi zorunludur.")]
        [MinLength(2, ErrorMessage = "Aranan urun adi en az 2 karakter olmalidir.")]
        [MaxLength(150, ErrorMessage = "Aranan urun adi en fazla 150 karakter olabilir.")]
        public string ArananUrunAdi { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir kategori secilmelidir.")]
        public int? KategoriId { get; set; }

        [Range(1, 100000, ErrorMessage = "Miktar en az 1 olmalidir.")]
        public int Miktar { get; set; } = 1;

        [MaxLength(30, ErrorMessage = "Birim en fazla 30 karakter olabilir.")]
        public string? Birim { get; set; } = "kg";

        [Required(ErrorMessage = "Teslimat sehri zorunludur.")]
        [MaxLength(60, ErrorMessage = "Teslimat sehri en fazla 60 karakter olabilir.")]
        public string TeslimatSehir { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Tercih edilen sehirler en fazla 200 karakter olabilir.")]
        public string? TercihEdilenSehirler { get; set; }

        [Range(0.01, 100000000, ErrorMessage = "Hedef birim fiyat 0'dan buyuk olmalidir.")]
        public decimal? HedefBirimFiyat { get; set; }

        public DateTime? SonTeklifTarihi { get; set; }

        [MaxLength(20)]
        public string? TalepTipi { get; set; }

        [MaxLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir.")]
        public string? Not { get; set; }
    }

    public class TalepTekrarlaDto
    {
        [Range(1, 100000, ErrorMessage = "Miktar en az 1 olmalidir.")]
        public int? Miktar { get; set; }

        [MaxLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir.")]
        public string? Not { get; set; }
    }

    public class AnlasmaOnayiDto
    {
        public bool GerceklestiMi { get; set; }
    }

    public class TalepTeklifOlusturDto
    {
        [Range(0.01, 100000000, ErrorMessage = "Birim fiyat 0'dan buyuk olmalidir.")]
        public decimal? BirimFiyat { get; set; }

        [Required(ErrorMessage = "Teklif mesaji zorunludur.")]
        [MaxLength(1000, ErrorMessage = "Mesaj en fazla 1000 karakter olabilir.")]
        public string Mesaj { get; set; } = string.Empty;

        [Range(1, 90, ErrorMessage = "Teslimat suresi 1-90 gun arasinda olmalidir.")]
        public int? TeslimatSuresiGun { get; set; }

        public bool KargoDahilMi { get; set; } = true;

        [MaxLength(60, ErrorMessage = "Gonderim sehri en fazla 60 karakter olabilir.")]
        public string? GonderimSehir { get; set; }
    }

    public class TalepTeklifDto
    {
        public int Id { get; set; }
        public int TalepId { get; set; }
        public string SaticiId { get; set; } = string.Empty;
        public string? SaticiMagazaAdi { get; set; }
        public string? SaticiSehir { get; set; }
        public bool SaticiDogrulanmis { get; set; }
        public decimal? BirimFiyat { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public int? TeslimatSuresiGun { get; set; }
        public bool KargoDahilMi { get; set; }
        public string? GonderimSehir { get; set; }
        public string Durum { get; set; } = string.Empty;
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
    }

    public class TalepDto
    {
        public int Id { get; set; }
        public string AliciId { get; set; } = string.Empty;
        public string? AliciAdi { get; set; }
        public string TalepTuru { get; set; } = Domain.Constants.TalepTurleri.Urun;
        public string TalepTipi { get; set; } = Domain.Constants.TalepTipleri.Bireysel;
        public int? UrunId { get; set; }
        public string? UrunAdi { get; set; }
        public string? UrunResimUrl { get; set; }
        public decimal? UrunFiyat { get; set; }
        public int? KategoriId { get; set; }
        public string? KategoriAdi { get; set; }
        public string? ArananUrunAdi { get; set; }
        public string? Birim { get; set; }
        public string? TeslimatSehir { get; set; }
        public string? TercihEdilenSehirler { get; set; }
        public decimal? HedefBirimFiyat { get; set; }
        public DateTime? SonTeklifTarihi { get; set; }
        public string? SaticiId { get; set; }
        public string? SaticiMagazaAdi { get; set; }
        public string? SaticiSehir { get; set; }
        public string? SaticiIlce { get; set; }
        public int Miktar { get; set; }
        public string? Not { get; set; }
        public string Durum { get; set; } = string.Empty;
        public bool? AliciGerceklestiOnayi { get; set; }
        public bool? SaticiGerceklestiOnayi { get; set; }
        public int? EslesmeSkoru { get; set; }
        public string? EslesmeSebebi { get; set; }
        public int EslesenSaticiSayisi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public List<TalepTeklifDto> Teklifler { get; set; } = new();
    }
}
