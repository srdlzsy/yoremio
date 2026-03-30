using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ISaticiProfiliService : IBaseService<SaticiProfili>
    {
        Task<SaticiProfili?> GetByVergiNoAsync(string vergiNo);
        Task<SaticiProfili?> GetSaticiWithUserByIdAsync(string kullaniciId);
        Task<SaticiProfilDto?> GetSaticiProfilDtoAsync(string kullaniciId);
        Task<SaticiProfilDto?> UpdateProfilAsync(string kullaniciId, SaticiProfilGuncelleDto dto);
        Task<SaticiGuvenSkoruDto?> GetGuvenSkoruAsync(string saticiId);
    }
}
