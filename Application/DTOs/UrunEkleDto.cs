using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class UrunEkleDto
    {
        public string Adi { get; set; } = null!;
        public string? Aciklama { get; set; }
        public decimal Fiyat { get; set; }
        public int StokMiktari { get; set; }
        public int KategoriId { get; set; }
        public List<IFormFile> Resimler { get; set; } = new List<IFormFile>();
        public List<IFormFile> Videolar { get; set; } = new List<IFormFile>();
    }
}
