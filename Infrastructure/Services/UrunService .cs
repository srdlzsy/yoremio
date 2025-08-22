using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class UrunService : IUrunService
    {
        private readonly IUrunRepository _urunRepository;
        private readonly ISaticiProfiliRepository _saticiRepository;
        private readonly IDosyaKaydetService _dosyaKaydetService;

        public UrunService(
            IUrunRepository urunRepository,
            ISaticiProfiliRepository saticiRepository,
            IDosyaKaydetService dosyaKaydetService)
        {
            _urunRepository = urunRepository;
            _saticiRepository = saticiRepository;
            _dosyaKaydetService = dosyaKaydetService;
        }

        public async Task<Urun> UrunEkleAsync(string saticiId, UrunEkleDto dto)
        {
            // Satıcı bilgisi al
            var satici = await _saticiRepository.GetByIdAsync(saticiId);
            if (satici == null)
                throw new Exception("Satıcı bulunamadı.");

            // Satıcı adı temizle (boşluk, özel karakterler vb için)
            var saticiAdi = Slugify(satici.MagazaAdi ?? "bilinmeyen-satici");

            // 1. Ürünü kaydet (Id oluşması için)
            var urun = await UrunKaydetAsync(saticiId, dto);
            Console.WriteLine("Oluşan ürün ID: " + urun.Id);


            // 2. Resimleri kaydet
            var resimUrlListesi = await DosyalariKaydetAsync(dto.Resimler, saticiAdi, urun.Id, "resimler");

            // 3. Videoları kaydet
            var videoUrlListesi = await DosyalariKaydetAsync(dto.Videolar, saticiAdi, urun.Id, "videolar");

            // 4. Ürün ile dosya URL’lerini ilişkilendir
            var kayitliUrun = await _urunRepository.EkleUrunAsync(urun, resimUrlListesi, videoUrlListesi);

            return kayitliUrun;
        }

        public async Task<IEnumerable<UrunDto>> GetAllUrunlerAsync()
        {
            var urunler = await _urunRepository.GetAllAsync();
            return urunler.Select(u => MapToDto(u));
        }

        public async Task<IEnumerable<UrunDto>> GetUrunlerBySaticiIdAsync(string saticiId)
        {
            var urunler = await _urunRepository.FindAsync(u => u.SaticiId == saticiId);
            return urunler.Select(u => MapToDto(u));
        }

        public async Task<UrunDto?> GetUrunByIdAsync(int urunId)
        {
            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null) return null;

            return MapToDto(urun);
        }

        private UrunDto MapToDto(Urun urun)
        {
            return new UrunDto
            {
                Id = urun.Id,
                Adi = urun.Adi,
                Aciklama = urun.Aciklama,
                Fiyat = urun.Fiyat,
                StokMiktari = urun.StokMiktari,
                KategoriId = urun.KategoriId,
                SaticiId = urun.SaticiId,
                Resimler = urun.Resimler?.Select(r => new UrunResimDto { Id = r.Id, Url = r.Url }).ToList(),
                Videolar = urun.Videolar?.Select(v => new UrunVideoDto { Id = v.Id, Url = v.Url }).ToList()
            };
        }

        private async Task<Urun> UrunKaydetAsync(string saticiId, UrunEkleDto dto)
        {
            var urun = new Urun
            {
                Adi = dto.Adi,
                Aciklama = dto.Aciklama ?? string.Empty,
                Fiyat = dto.Fiyat,
                StokMiktari = dto.StokMiktari,
                KategoriId = dto.KategoriId,
                SaticiId = saticiId
            };

            // EKLEME işlemi eksikti!
            await _urunRepository.AddAsync(urun); // ✅ Veritabanına ekle

            return urun;
        }

        private async Task<List<string>> DosyalariKaydetAsync(IEnumerable<IFormFile> dosyalar, string saticiAdi, int urunId, string dosyaTuru)
        {
            var urlListesi = new List<string>();
            if (dosyalar == null || !dosyalar.Any())
                return urlListesi;

            foreach (var dosya in dosyalar)
            {
                var klasorYolu = Path.Combine(saticiAdi, urunId.ToString(), dosyaTuru);
                var url = await _dosyaKaydetService.KaydetDosyaAsync(dosya, klasorYolu);
                urlListesi.Add(url);
            }

            return urlListesi;
        }

        // Basit slugify fonksiyonu: Boşlukları tire yapar, özel karakterleri kaldırır, küçük harf yapar
        private string Slugify(string text)
        {
            text = text.ToLowerInvariant();
            // Türkçe karakterleri ingilizce karşılıklarıyla değiştir (isteğe bağlı)
            text = text.Replace("ı", "i").Replace("ö", "o").Replace("ü", "u").Replace("ş", "s").Replace("ğ", "g").Replace("ç", "c");

            // Boşluk ve özel karakterleri tire yap
            text = Regex.Replace(text, @"\s+", "-");

            // Geçersiz karakterleri kaldır
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");

            // Fazla tireyi tek tire yap
            text = Regex.Replace(text, @"-+", "-").Trim('-');

            return text;
        }

        public async Task<UrunDto> UrunGuncelleAsync(int urunId, string saticiId, UrunGuncelleDto dto)
        {
            var satici = await _saticiRepository.GetByIdAsync(saticiId);
            if (satici == null)
                throw new Exception("Satıcı bulunamadı.");

            var saticiAdi = Slugify(satici.MagazaAdi ?? "bilinmeyen-satici");

            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");

            // DTO null değilse ve alan doluysa güncelle
            if (dto != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.Adi))
                    urun.Adi = dto.Adi;

                if (!string.IsNullOrWhiteSpace(dto.Aciklama))
                    urun.Aciklama = dto.Aciklama;

                if (dto.Fiyat.HasValue)
                    urun.Fiyat = dto.Fiyat.Value;

                if (dto.StokMiktari.HasValue)
                    urun.StokMiktari = dto.StokMiktari.Value;

                if (dto.KategoriId.HasValue)
                    urun.KategoriId = dto.KategoriId.Value;

                // Resimler eklenmişse
                if (dto.Resimler is { Count: > 0 })
                {
                    var resimUrlListesi = await DosyalariKaydetAsync(dto.Resimler, saticiAdi, urun.Id, "resimler");
                    foreach (var url in resimUrlListesi)
                        urun.Resimler.Add(new UrunResim { Url = url });
                }

                // Videolar eklenmişse
                if (dto.Videolar is { Count: > 0 })
                {
                    var videoUrlListesi = await DosyalariKaydetAsync(dto.Videolar, saticiAdi, urun.Id, "videolar");
                    foreach (var url in videoUrlListesi)
                        urun.Videolar.Add(new UrunVideo { Url = url });
                }
            }

            await _urunRepository.SaveChangesAsync();
            return MapToDto(urun);
        }


        public Task<bool> UrunSilAsync(int urunId)
        {
            var urun = _urunRepository.GetByIdAsync(urunId).Result;
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");
            _urunRepository.Remove(urun);
            return _urunRepository.SaveChangesAsync();

        }

        public async Task<bool> UrunResimSilAsync(int urunId, int resimId)
        {
            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");

            var resim = urun.Resimler.FirstOrDefault(r => r.Id == resimId);
            if (resim == null)
                throw new Exception("Resim bulunamadı.");

            urun.Resimler.Remove(resim);

            return await _urunRepository.SaveChangesAsync();
        }
        public async Task<bool> UrunVideoSilAsync(int urunId, int videoId)
        {
            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");

            if (urun.Videolar == null)
                throw new Exception("Video listesi boş.");

            var video = urun.Videolar.FirstOrDefault(v => v.Id == videoId);
            if (video == null)
                throw new Exception("Video bulunamadı.");

            urun.Videolar.Remove(video);
            return await _urunRepository.SaveChangesAsync();
        }

        public async Task<Urun?> EkleMedyaToUrunAsync(int urunId, List<string> resimUrlListesi, List<string> videoUrlListesi)
        {
            var urun = await _urunRepository.GetByIdAsync(urunId);
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");

            urun.Resimler ??= new List<UrunResim>();
            urun.Videolar ??= new List<UrunVideo>();

            if (resimUrlListesi != null && resimUrlListesi.Count > 0)
            {
                foreach (var url in resimUrlListesi)
                {
                    urun.Resimler.Add(new UrunResim { Url = url, UrunId = urunId });
                }
            }

            if (videoUrlListesi != null && videoUrlListesi.Count > 0)
            {
                foreach (var url in videoUrlListesi)
                {
                    urun.Videolar.Add(new UrunVideo { Url = url, UrunId = urunId });
                }
            }

            await _urunRepository.SaveChangesAsync();
            return urun;
        }
    }
}