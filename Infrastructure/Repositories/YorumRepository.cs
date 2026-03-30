using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class YorumRepository : BaseRepository<Yorum>, IYorumRepository
    {
        private readonly YoremioContext _dbContext;

        public YorumRepository(YoremioContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<IEnumerable<Yorum>> GetYorumlarByUrunIdAsync(int urunId)
        {
            return await _dbContext.Yorumlar
                .Include(y => y.YorumYapanKullanici)
                .Where(y => y.UrunId == urunId)
                .OrderByDescending(y => y.Tarih)
                .ToListAsync();
        }

        public async Task<Yorum?> GetByIdWithUserAsync(int yorumId)
        {
            return await _dbContext.Yorumlar
                .Include(y => y.YorumYapanKullanici)
                .FirstOrDefaultAsync(y => y.Id == yorumId);
        }
    }
}
