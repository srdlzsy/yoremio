using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class SaticiProfiliRepository : BaseRepository<SaticiProfili>, ISaticiProfiliRepository
    {
        public SaticiProfiliRepository(YoremioContext context) : base(context)
        {
        }


        public async Task<SaticiProfili?> GetSaticiWithUserByIdAsync(string kullaniciId)
        {
            return await _dbSet.Include(p => p.Kullanici)
                               .FirstOrDefaultAsync(p => p.KullaniciId == kullaniciId);
        }

        // Ekstra metodlar burada yazılabilir
    }
}
