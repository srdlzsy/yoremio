using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class RegisterSaticiDto
    {
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string MagazaAdi { get; set; } = null!;
        public string VergiNo { get; set; } = null!;
        public string? Adres { get; set; } = null!;
    }
}
