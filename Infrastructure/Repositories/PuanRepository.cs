using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PuanRepository : BaseRepository<Puan>, IPuanRepository
    {
        private readonly YoremioContext _yoremiocontext;

        public PuanRepository(YoremioContext context) : base(context)
        {
            _yoremiocontext = context;
        }

        /// <summary>
        /// Kullanıcının ürüne verdiği puanı ekler veya varsa günceller.
        /// Puan değeri 1 ile 5 arasında olmalıdır.
        /// </summary>
        /// <param name="urunId">Puan verilen ürünün ID'si</param>
        /// <param name="kullaniciId">Puan veren kullanıcının ID'si</param>
        /// <param name="puanDegeri">1 ile 5 arasında puan değeri</param>
        /// <returns>Eklenen veya güncellenen puan değeri</returns>
        public async Task<int> GetOrCreatePuanAsync(int urunId, string kullaniciId, int puanDegeri)
        {
            // Geçersiz puan değerini engelle
            if (puanDegeri < 1 || puanDegeri > 5)
                throw new ArgumentOutOfRangeException(nameof(puanDegeri), "Puan değeri 1 ile 5 arasında olmalıdır.");

            // Kullanıcının bu ürüne daha önce puan verip vermediğini kontrol et
            var mevcutPuan = await _yoremiocontext.Puanlar
                .FirstOrDefaultAsync(p => p.UrunId == urunId && p.KullaniciId == kullaniciId);

            if (mevcutPuan != null)
            {
                // Puan varsa güncelle
                mevcutPuan.PuanDegeri = puanDegeri;
                _yoremiocontext.Puanlar.Update(mevcutPuan);
            }
            else
            {
                // Puan yoksa yeni puan oluştur ve ekle
                mevcutPuan = new Puan
                {
                    UrunId = urunId,
                    KullaniciId = kullaniciId,
                    PuanDegeri = puanDegeri,
                    PuanTarihi = DateTime.UtcNow
                };
                await _yoremiocontext.Puanlar.AddAsync(mevcutPuan);
            }

            // Değişiklikleri kaydet
            await _yoremiocontext.SaveChangesAsync();

            return mevcutPuan.PuanDegeri  ;
        }

        /// <summary>
        /// Belirli bir ürün ve kullanıcı için verilen puanı getirir.
        /// </summary>
        /// <param name="urunId">Ürün ID'si</param>
        /// <param name="kullaniciId">Kullanıcı ID'si</param>
        /// <returns>Puan varsa Puan nesnesi, yoksa null</returns>
        public async Task<Puan?> GetPuanByUrunIdAndKullaniciIdAsync(int urunId, string kullaniciId)
        {
            return await _yoremiocontext.Puanlar
                .FirstOrDefaultAsync(p => p.UrunId == urunId && p.KullaniciId == kullaniciId);
        }

        /// <summary>
        /// Belirli bir ürüne ait tüm puanları listeler.
        /// </summary>
        /// <param name="urunId">Ürün ID'si</param>
        /// <returns>Ürüne ait puanların listesi</returns>
        public async Task<IEnumerable<Puan>> GetPuanlarByUrunIdAsync(int urunId)
        {
            return await _yoremiocontext.Puanlar
                .Where(p => p.UrunId == urunId)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir kullanıcıya ait tüm puanları getirir.
        /// </summary>
        /// <param name="kullaniciId">Kullanıcı ID'si</param>
        /// <returns>Kullanıcının verdiği puanlar</returns>
        public async Task<IEnumerable<Puan>> GetPuanlarByKullaniciIdAsync(string kullaniciId)
        {
            return await _yoremiocontext.Puanlar
                .Where(p => p.KullaniciId == kullaniciId)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir kullanıcı ve ürün için puan verilip verilmediğini kontrol eder.
        /// </summary>
        /// <param name="urunId">Ürün ID'si</param>
        /// <param name="kullaniciId">Kullanıcı ID'si</param>
        /// <returns>Puan varsa true, yoksa false</returns>
        public async Task<bool> PuanVarmiAsync(int urunId, string kullaniciId)
        {
            return await _yoremiocontext.Puanlar
                .AnyAsync(p => p.UrunId == urunId && p.KullaniciId == kullaniciId);
        }

        /// <summary>
        /// Belirli bir ürünün ortalama puanını hesaplar.
        /// Puan yoksa 0 döner.
        /// </summary>
        /// <param name="urunId">Ürün ID'si</param>
        /// <returns>Ortalama puan (1-5 arası), puan yoksa 0</returns>
        public async Task<double> GetOrtalamaPuanByUrunIdAsync(int urunId)
        {
            var puanlar = await _yoremiocontext.Puanlar
                .Where(p => p.UrunId == urunId)
                .ToListAsync();

            if (puanlar.Count == 0)
                return 0;

            // Ortalama değeri 1 ondalık basamağa yuvarla
            return Math.Round(puanlar.Average(p => p.PuanDegeri), 1);
        }

        // Buraya istersen ekstra metodlar ekleyebilirsin.
    }
}
