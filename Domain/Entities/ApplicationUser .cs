using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace Domain.Entities
{
    public class ApplicationUser: IdentityUser
    {
        public string?  Rol { get; set; } // "Satici" veya "Alici"

        public virtual SaticiProfili? SaticiProfili { get; set; }
        public virtual AliciProfili? AliciProfili { get; set; }
    }
}
