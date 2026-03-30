using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DosyaKaydetService : IDosyaKaydetService
    {
        public async Task<string> KaydetDosyaAsync(IFormFile dosya, string klasor)
        {
            if (dosya == null)
                throw new ArgumentNullException(nameof(dosya));

            var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(dosya.FileName);
            var klasorYolu = Path.Combine("wwwroot", klasor);

            if (!Directory.Exists(klasorYolu))
            {
                Directory.CreateDirectory(klasorYolu);
            }

            var tamYol = Path.Combine(klasorYolu, dosyaAdi);

            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await dosya.CopyToAsync(stream);
            }

            // klasor yolundaki \ işaretlerini / ile değiştiriyoruz ki URL doğru olsun
            var url = "/" + klasor.Replace("\\", "/") + "/" + dosyaAdi;

            return url;
        }
    }
}
