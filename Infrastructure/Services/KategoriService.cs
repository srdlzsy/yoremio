using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class KategoriService : BaseService<Kategori>, IKategoriService
    {
        private static readonly IReadOnlyCollection<KategoriSeed> DefaultCategories = new[]
        {
            new KategoriSeed("Sebze", "Mevsiminde toplanmis organik sebzeler"),
            new KategoriSeed("Meyve", "Taze ve dogal meyveler"),
            new KategoriSeed("Sut Urunleri", "Gunluk sut, peynir ve yogurt cesitleri"),
            new KategoriSeed("Bakliyat", "Katkisiz kuru gida urunleri"),
            new KategoriSeed("Kahvaltilik", "Bal, recel, yumurta ve kahvaltilik urunler")
        };

        private readonly IKategoriRepository _kategoriRepository;

        public KategoriService(IKategoriRepository kategoriRepository)
            : base(kategoriRepository)
        {
            _kategoriRepository = kategoriRepository;
        }

        public async Task<IEnumerable<KategoriDto>> GetAllDtosAsync()
        {
            var kategoriler = (await _kategoriRepository.GetAllAsync()).ToList();
            if (kategoriler.Count == 0)
            {
                foreach (var seed in DefaultCategories)
                {
                    await _kategoriRepository.AddAsync(new Kategori
                    {
                        Adi = seed.Name,
                        Aciklama = seed.Description
                    });
                }

                await _kategoriRepository.SaveChangesAsync();
                kategoriler = (await _kategoriRepository.GetAllAsync()).ToList();
            }

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

        private sealed record KategoriSeed(string Name, string Description);
    }
}
