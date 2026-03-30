using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class KategoriService : BaseService<Kategori>, IKategoriService
    {
        private readonly IKategoriRepository _kategoriRepository;

        public KategoriService(IKategoriRepository kategoriRepository)
            : base(kategoriRepository)
        {
            _kategoriRepository = kategoriRepository;
        }

        public async Task<IEnumerable<KategoriDto>> GetAllDtosAsync()
        {
            var kategoriler = await _kategoriRepository.GetAllAsync();
            return kategoriler.OrderBy(x => x.Adi).Select(MapToDto);
        }

        public async Task<KategoriDto?> GetDtoByIdAsync(int id)
        {
            var kategori = await _kategoriRepository.GetByIdAsync(id);
            return kategori is null ? null : MapToDto(kategori);
        }

        public async Task<KategoriDto> AddAsync(KategoriCreateDto dto)
        {
            var kategori = new Kategori
            {
                Adi = dto.Adi.Trim(),
                Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim()
            };

            await _kategoriRepository.AddAsync(kategori);
            await _kategoriRepository.SaveChangesAsync();

            return MapToDto(kategori);
        }

        public async Task<KategoriDto?> UpdateAsync(int id, KategoriUpdateDto dto)
        {
            var kategori = await _kategoriRepository.GetByIdAsync(id);
            if (kategori is null)
            {
                return null;
            }

            kategori.Adi = dto.Adi.Trim();
            kategori.Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim();

            _kategoriRepository.Update(kategori);
            await _kategoriRepository.SaveChangesAsync();

            return MapToDto(kategori);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var kategori = await _kategoriRepository.GetByIdAsync(id);
            if (kategori is null)
            {
                return false;
            }

            _kategoriRepository.Remove(kategori);
            return await _kategoriRepository.SaveChangesAsync();
        }

        private static KategoriDto MapToDto(Kategori kategori)
        {
            return new KategoriDto
            {
                Id = kategori.Id,
                Adi = kategori.Adi,
                Aciklama = kategori.Aciklama
            };
        }
    }
}
