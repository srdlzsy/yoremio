using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IKategoriService : IBaseService<Kategori>
    {
        Task<IEnumerable<KategoriDto>> GetAllDtosAsync();
        Task<KategoriDto?> GetDtoByIdAsync(int id);
        Task<KategoriDto> AddAsync(KategoriCreateDto dto);
        Task<KategoriDto?> UpdateAsync(int id, KategoriUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
