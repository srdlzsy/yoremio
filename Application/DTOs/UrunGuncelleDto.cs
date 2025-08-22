using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class UrunGuncelleDto
    {
        public string? Adi { get; set; }
        public string? Aciklama { get; set; }
        public decimal? Fiyat { get; set; }
        public int? StokMiktari { get; set; }
        public int? KategoriId { get; set; }
        public List<IFormFile>? Resimler { get; set; }
        public List<IFormFile>? Videolar { get; set; }
    }


}