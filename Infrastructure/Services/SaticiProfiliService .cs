using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SaticiProfiliService : BaseService<SaticiProfili>, ISaticiProfiliService
    {
        private readonly ISaticiProfiliRepository _saticiRepo;

        public SaticiProfiliService(ISaticiProfiliRepository saticiRepo)
            : base(saticiRepo)
        {
            _saticiRepo = saticiRepo;
        }

        public async Task<SaticiProfili?> GetByVergiNoAsync(string vergiNo)
        {
            var result = await _saticiRepo.FindAsync(x => x.VergiNo == vergiNo);
            return result.FirstOrDefault();
        }
        public async Task<SaticiProfili?> GetSaticiWithUserByIdAsync(string kullaniciId)
        {
            return await _saticiRepo.GetSaticiWithUserByIdAsync(kullaniciId);
        }
    }
}
