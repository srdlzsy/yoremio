using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UrunVideo
    {
        public int Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public int UrunId { get; set; }  // Foreign key

        public Urun? Urun { get; set; }
    }
}
