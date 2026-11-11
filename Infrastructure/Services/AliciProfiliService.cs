using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services
{
    public class AliciProfiliService : IAliciProfiliService
    {
        private readonly IAliciProfiliRepository _aliciRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public AliciProfiliService(IAliciProfiliRepository aliciRepo, UserManager<ApplicationUser> userManager)
        {
            _aliciRepo = aliciRepo;
            _userManager = userManager;
        }

        public async Task<AliciProfilDto?> GetAliciProfilDtoAsync(string kullaniciId)
        {
            var profil = await GetOrCreateProfilAsync(kullaniciId);
            return profil is null ? null : MapToDto(profil);
        }

        public async Task<AliciProfilDto?> UpdateProfilAsync(string kullaniciId, AliciProfilGuncelleDto dto)
        {
            var profil = await GetOrCreateProfilAsync(kullaniciId);
            if (profil is null || profil.Kullanici is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.UserName))
            {
                var yeniUserName = dto.UserName.Trim();
                if (!string.Equals(profil.Kullanici.UserName, yeniUserName, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUser = await _userManager.FindByNameAsync(yeniUserName);
                    if (existingUser != null && !string.Equals(existingUser.Id, kullaniciId, StringComparison.Ordinal))
                    {
                        throw new ArgumentException("Bu kullanıcı adı zaten kullanılıyor.");
                    }

                    var setUserNameResult = await _userManager.SetUserNameAsync(profil.Kullanici, yeniUserName);
                    if (!setUserNameResult.Succeeded)
                    {
                        throw new ArgumentException(string.Join(" | ", setUserNameResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (dto.PhoneNumber is not null)
            {
                profil.Kullanici.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            }

            if (dto.Adres is not null)
            {
                profil.Adres = string.IsNullOrWhiteSpace(dto.Adres) ? null : dto.Adres.Trim();
            }

            if (dto.Sehir is not null)
            {
                profil.Sehir = string.IsNullOrWhiteSpace(dto.Sehir) ? null : dto.Sehir.Trim();
            }

            if (dto.Ilce is not null)
            {
                profil.Ilce = string.IsNullOrWhiteSpace(dto.Ilce) ? null : dto.Ilce.Trim();
            }

            if (dto.FavoriKategori is not null)
            {
                profil.FavoriKategori = string.IsNullOrWhiteSpace(dto.FavoriKategori) ? null : dto.FavoriKategori.Trim();
            }

            _aliciRepo.Update(profil);
            await _aliciRepo.SaveChangesAsync();

            return MapToDto(profil);
        }

        private async Task<AliciProfili?> GetOrCreateProfilAsync(string kullaniciId)
        {
            var profil = await _aliciRepo.GetWithUserByIdAsync(kullaniciId);
            if (profil != null)
            {
                return profil;
            }

            var user = await _userManager.FindByIdAsync(kullaniciId);
            if (user == null)
            {
                return null;
            }

            profil = new AliciProfili
            {
                KullaniciId = user.Id,
                AktifMi = true,
                Kullanici = user
            };

            await _aliciRepo.AddAsync(profil);
            await _aliciRepo.SaveChangesAsync();
            return profil;
        }

        private static AliciProfilDto MapToDto(AliciProfili profil)
        {
            return new AliciProfilDto
            {
                KullaniciId = profil.KullaniciId,
                Email = profil.Kullanici?.Email,
                UserName = profil.Kullanici?.UserName,
                PhoneNumber = profil.Kullanici?.PhoneNumber,
                Adres = profil.Adres,
                Sehir = profil.Sehir,
                Ilce = profil.Ilce,
                FavoriKategori = profil.FavoriKategori,
                KayitTarihi = profil.KayitTarihi,
                AktifMi = profil.AktifMi
            };
        }
    }
}
