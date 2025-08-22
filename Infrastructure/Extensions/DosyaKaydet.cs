using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Extensions
{
    public static class DosyaKaydet
    {
        public static async Task<string> KaydetDosyaAsync(IFormFile dosya, string klasor)
        {
            if (dosya == null || dosya.Length == 0)
                throw new ArgumentException("Dosya geçerli değil.");

            var dosyaAdi = Guid.NewGuid() + Path.GetExtension(dosya.FileName);
            var yol = Path.Combine("wwwroot", klasor);

            // Klasör varsa oluştur
            Directory.CreateDirectory(yol);

            var tamYol = Path.Combine(yol, dosyaAdi);

            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await dosya.CopyToAsync(stream);
            }

            // Örnek dönüş: "/resimler/abcd.jpg"
            return $"/{klasor}/{dosyaAdi}";
        }
    }
}
