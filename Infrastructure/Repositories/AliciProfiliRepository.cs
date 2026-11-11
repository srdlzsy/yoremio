using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AliciProfiliRepository : BaseRepository<AliciProfili>, IAliciProfiliRepository
    {
        public AliciProfiliRepository(YoremioContext context) : base(context)
        {
        }

        public async Task<AliciProfili?> GetWithUserByIdAsync(string kullaniciId)
        {
            return await _dbSet
                .Include(p => p.Kullanici)
                .FirstOrDefaultAsync(p => p.KullaniciId == kullaniciId);
        }
    }
}
