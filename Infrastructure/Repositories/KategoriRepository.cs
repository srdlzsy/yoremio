using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class KategoriRepository : BaseRepository<Kategori>, IKategoriRepository
    {
        public KategoriRepository(YoremioContext context) : base(context)
        {
        }
    }
}
