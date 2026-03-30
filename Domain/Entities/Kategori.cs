using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Kategori
    {
        public int Id { get; set; }

        public string Adi { get; set; } = string.Empty;

        public string? Aciklama { get; set; }

        // Bir kategorinin birden çok ürünü olabilir
        public List<Urun> Urunler { get; set; } = new List<Urun>();
    }

}
