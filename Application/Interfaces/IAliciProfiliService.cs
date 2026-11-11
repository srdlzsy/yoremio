using Application.DTOs;

namespace Application.Interfaces
{
    public interface IAliciProfiliService
    {
        Task<AliciProfilDto?> GetAliciProfilDtoAsync(string kullaniciId);
        Task<AliciProfilDto?> UpdateProfilAsync(string kullaniciId, AliciProfilGuncelleDto dto);
    }
}
