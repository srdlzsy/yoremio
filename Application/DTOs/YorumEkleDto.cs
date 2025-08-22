using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class YorumEkleDto
    {
        public int UrunId { get; set; }
        public string Icerik { get; set; } = null!;
    }
}
