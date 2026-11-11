using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly YoremioContext _context;
        private readonly IBildirimService _bildirimService;

        public AdminService(YoremioContext context, IBildirimService bildirimService)
        {
            _context = context;
            _bildirimService = bildirimService;
        }

        public async Task<PagedResult<AdminKullaniciDto>> GetKullanicilarAsync(AdminKullaniciQuery query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

            var usersQuery = _context.Users
                .AsNoTracking()
                .Include(u => u.SaticiProfili)
                .ThenInclude(sp => sp!.Urunler)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                    (u.SaticiProfili != null && u.SaticiProfili.MagazaAdi.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(query.Rol))
            {
                var targetRole = query.Rol.Trim().ToUpperInvariant();
                var roleUserIds = _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Where(x => x.Name != null && x.Name.ToUpper() == targetRole)
                    .Select(x => x.UserId);

                usersQuery = usersQuery.Where(u => roleUserIds.Contains(u.Id));
            }

            if (query.SadeceBekleyenSaticilar)
            {
                usersQuery = usersQuery.Where(u =>
                    u.SaticiProfili != null &&
                    u.SaticiProfili.AdminDogrulandiMi != true &&
                    (!u.EmailConfirmed || !u.SaticiProfili.AktifMi || u.SaticiProfili.AdminDogrulandiMi == false));
            }

            var totalCount = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderByDescending(u => u.SaticiProfili != null)
                .ThenBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var rolePairs = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
                .ToListAsync();

            var rolesByUser = rolePairs
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(x => x.RoleName).OrderBy(x => x).ToArray());

            var items = users.Select(u => MapKullaniciDto(u, rolesByUser.GetValueOrDefault(u.Id, Array.Empty<string>()))).ToArray();

            return new PagedResult<AdminKullaniciDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<AdminKullaniciDto> SetSaticiDogrulamaAsync(string saticiId, AdminSaticiDogrulamaDto dto)
        {
            var satici = await _context.SaticiProfilleri
                .Include(sp => sp.Kullanici)
                .Include(sp => sp.Urunler)
                .FirstOrDefaultAsync(sp => sp.KullaniciId == saticiId);

            if (satici == null || satici.Kullanici == null)
            {
                throw new KeyNotFoundException("Satıcı profili bulunamadı.");
            }

            satici.AdminDogrulandiMi = dto.DogrulandiMi;
            if (dto.DogrulandiMi == true && !satici.AktifMi)
            {
                satici.AktifMi = true;
            }

            await _context.SaveChangesAsync();

            var bildirimBaslik = dto.DogrulandiMi switch
            {
                true => "Mağazanız doğrulandı",
                false => "Mağaza doğrulaması kaldırıldı",
                null => "Mağaza doğrulama durumu sıfırlandı"
            };
            var bildirimMesaj = dto.DogrulandiMi switch
            {
                true => $"{satici.MagazaAdi} mağazanız yönetici tarafından doğrulandı.",
                false => string.IsNullOrWhiteSpace(dto.Not)
                    ? $"{satici.MagazaAdi} mağazanızın doğrulama rozeti yönetici tarafından kaldırıldı."
                    : $"{satici.MagazaAdi} mağazanızın doğrulama rozeti kaldırıldı. Not: {dto.Not.Trim()}",
                null => $"{satici.MagazaAdi} mağazanız otomatik doğrulama kuralına döndürüldü."
            };

            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = satici.KullaniciId,
                Tur = "SISTEM",
                Baslik = bildirimBaslik,
                Mesaj = bildirimMesaj,
                IlgiliVarlikTuru = "SaticiProfili",
                IlgiliVarlikId = satici.KullaniciId,
                AksiyonUrl = "/satici/profil"
            });

            var roles = await GetUserRolesAsync(satici.KullaniciId);
            return MapKullaniciDto(satici.Kullanici, roles);
        }

        public async Task<AdminKullaniciDto> SetSaticiDurumAsync(string saticiId, AdminDurumGuncelleDto dto)
        {
            var satici = await _context.SaticiProfilleri
                .Include(sp => sp.Kullanici)
                .Include(sp => sp.Urunler)
                .FirstOrDefaultAsync(sp => sp.KullaniciId == saticiId);

            if (satici == null || satici.Kullanici == null)
            {
                throw new KeyNotFoundException("Satıcı profili bulunamadı.");
            }

            satici.AktifMi = dto.AktifMi;
            await _context.SaveChangesAsync();

            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = satici.KullaniciId,
                Tur = "SISTEM",
                Baslik = dto.AktifMi ? "Mağazanız aktifleştirildi" : "Mağazanız askıya alındı",
                Mesaj = dto.AktifMi
                    ? $"{satici.MagazaAdi} mağazanız yeniden yayına alındı."
                    : string.IsNullOrWhiteSpace(dto.Sebep)
                        ? $"{satici.MagazaAdi} mağazanız yönetici tarafından pasife alındı."
                        : $"{satici.MagazaAdi} mağazanız pasife alındı. Sebep: {dto.Sebep.Trim()}",
                IlgiliVarlikTuru = "SaticiProfili",
                IlgiliVarlikId = satici.KullaniciId,
                AksiyonUrl = "/satici/profil"
            });

            var roles = await GetUserRolesAsync(satici.KullaniciId);
            return MapKullaniciDto(satici.Kullanici, roles);
        }

        public async Task<PagedResult<UrunDto>> GetUrunlerAsync(AdminUrunQuery query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

            var urunQuery = _context.Urunler
                .AsNoTracking()
                .Include(u => u.Resimler)
                .Include(u => u.Videolar)
                .Include(u => u.Kategori)
                .Include(u => u.Puanlar)
                .ThenInclude(p => p.Kullanici)
                .Include(u => u.Favoriler)
                .Include(u => u.Satici)
                .ThenInclude(s => s.Kullanici)
                .Include(u => u.Yorumlar)
                .ThenInclude(y => y.YorumYapanKullanici)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim().ToLower();
                urunQuery = urunQuery.Where(u =>
                    u.Adi.ToLower().Contains(term) ||
                    u.Aciklama.ToLower().Contains(term) ||
                    u.Satici.MagazaAdi.ToLower().Contains(term));
            }

            if (query.KategoriId.HasValue)
            {
                urunQuery = urunQuery.Where(u => u.KategoriId == query.KategoriId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SaticiId))
            {
                urunQuery = urunQuery.Where(u => u.SaticiId == query.SaticiId);
            }

            if (query.AktifMi.HasValue)
            {
                urunQuery = urunQuery.Where(u => u.AktifMi == query.AktifMi.Value);
            }

            var totalCount = await urunQuery.CountAsync();
            var items = await urunQuery
                .OrderByDescending(u => u.OlusturmaTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UrunDto>
            {
                Items = items.Select(MapUrunDto).ToArray(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<UrunDto> SetUrunDurumAsync(int urunId, AdminDurumGuncelleDto dto)
        {
            var urun = await _context.Urunler
                .Include(u => u.Resimler)
                .Include(u => u.Videolar)
                .Include(u => u.Kategori)
                .Include(u => u.Puanlar)
                .ThenInclude(p => p.Kullanici)
                .Include(u => u.Favoriler)
                .Include(u => u.Satici)
                .ThenInclude(s => s.Kullanici)
                .Include(u => u.Yorumlar)
                .ThenInclude(y => y.YorumYapanKullanici)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == urunId);

            if (urun == null)
            {
                throw new KeyNotFoundException("Ürün bulunamadı.");
            }

            urun.AktifMi = dto.AktifMi;
            urun.GuncellemeTarihi = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
            {
                KullaniciId = urun.SaticiId,
                Tur = "SISTEM",
                Baslik = dto.AktifMi ? "Ürününüz yayına alındı" : "Ürününüz moderasyonla pasife alındı",
                Mesaj = dto.AktifMi
                    ? $"{urun.Adi} ürününüz yönetici tarafından yeniden aktifleştirildi."
                    : string.IsNullOrWhiteSpace(dto.Sebep)
                        ? $"{urun.Adi} ürününüz moderasyon kapsamında pasife alındı."
                        : $"{urun.Adi} ürününüz pasife alındı. Sebep: {dto.Sebep.Trim()}",
                IlgiliVarlikTuru = "Urun",
                IlgiliVarlikId = urun.Id.ToString(),
                AksiyonUrl = $"/urunler/{urun.Id}"
            });

            return MapUrunDto(urun);
        }

        public async Task<PagedResult<AdminYorumDto>> GetYorumlarAsync(AdminYorumQuery query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

            var yorumQuery = _context.Yorumlar
                .AsNoTracking()
                .Include(y => y.Urun)
                .Include(y => y.YorumYapanKullanici)
                .AsQueryable();

            if (query.UrunId.HasValue)
            {
                yorumQuery = yorumQuery.Where(y => y.UrunId == query.UrunId.Value);
            }

            if (query.AktifMi.HasValue)
            {
                yorumQuery = yorumQuery.Where(y => y.AktifMi == query.AktifMi.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim().ToLower();
                yorumQuery = yorumQuery.Where(y =>
                    y.Icerik.ToLower().Contains(term) ||
                    (y.Urun != null && y.Urun.Adi.ToLower().Contains(term)) ||
                    (y.YorumYapanKullanici != null && y.YorumYapanKullanici.Email != null && y.YorumYapanKullanici.Email.ToLower().Contains(term)));
            }

            var totalCount = await yorumQuery.CountAsync();
            var items = await yorumQuery
                .OrderByDescending(y => y.Tarih)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AdminYorumDto>
            {
                Items = items.Select(MapYorumDto).ToArray(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<AdminYorumDto> SetYorumDurumAsync(int yorumId, AdminDurumGuncelleDto dto)
        {
            var yorum = await _context.Yorumlar
                .Include(y => y.Urun)
                .Include(y => y.YorumYapanKullanici)
                .FirstOrDefaultAsync(y => y.Id == yorumId);

            if (yorum == null)
            {
                throw new KeyNotFoundException("Yorum bulunamadı.");
            }

            yorum.AktifMi = dto.AktifMi;
            await _context.SaveChangesAsync();

            if (!dto.AktifMi)
            {
                await _bildirimService.BildirimOlusturAsync(new BildirimOlusturDto
                {
                    KullaniciId = yorum.YorumYapanKullaniciId,
                    Tur = "SISTEM",
                    Baslik = "Yorumunuz gizlendi",
                    Mesaj = string.IsNullOrWhiteSpace(dto.Sebep)
                        ? $"{yorum.Urun?.Adi ?? "Ürün"} hakkındaki yorumunuz moderasyon nedeniyle gizlendi."
                        : $"{yorum.Urun?.Adi ?? "Ürün"} hakkındaki yorumunuz gizlendi. Sebep: {dto.Sebep.Trim()}",
                    IlgiliVarlikTuru = "Yorum",
                    IlgiliVarlikId = yorum.Id.ToString(),
                    AksiyonUrl = $"/urunler/{yorum.UrunId}"
                });
            }

            return MapYorumDto(yorum);
        }

        private async Task<IReadOnlyCollection<string>> GetUserRolesAsync(string userId)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name ?? string.Empty)
                .OrderBy(r => r)
                .ToArrayAsync();
        }

        private static AdminKullaniciDto MapKullaniciDto(ApplicationUser user, IReadOnlyCollection<string> roles)
        {
            var sp = user.SaticiProfili;
            return new AdminKullaniciDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                KilitliMi = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                Roles = roles,
                MagazaAdi = sp?.MagazaAdi,
                VergiNo = sp?.VergiNo,
                Sehir = sp?.Sehir,
                Ilce = sp?.Ilce,
                SaticiAktifMi = sp?.AktifMi,
                DogrulanmisSatici = SaticiProfili.IsVerified(sp),
                AdminDogrulamaDurumu = sp?.AdminDogrulandiMi,
                UrunSayisi = sp?.Urunler.Count ?? 0,
                KayitTarihi = sp?.KayitTarihi
            };
        }

        private static UrunDto MapUrunDto(Urun urun)
        {
            return new UrunDto
            {
                Id = urun.Id,
                Adi = urun.Adi,
                Aciklama = urun.Aciklama,
                Fiyat = urun.Fiyat,
                StokMiktari = urun.StokMiktari,
                AktifMi = urun.AktifMi,
                KategoriId = urun.KategoriId,
                KategoriAdi = urun.Kategori?.Adi,
                SaticiId = urun.SaticiId,
                SaticiMagazaAdi = urun.Satici?.MagazaAdi,
                SaticiSehir = urun.Satici?.Sehir,
                SaticiIlce = urun.Satici?.Ilce,
                SaticiDogrulanmis = SaticiProfili.IsVerified(urun.Satici),
                OrtalamaPuan = urun.Puanlar.Count == 0 ? 0 : Math.Round(urun.Puanlar.Average(p => p.PuanDegeri), 1),
                ToplamPuan = urun.Puanlar.Count,
                ToplamYorum = urun.Yorumlar.Count(y => y.AktifMi),
                ToplamFavori = urun.Favoriler.Count,
                Puanlar = urun.Puanlar.Select(p => new PuanDto
                {
                    Id = p.Id,
                    UrunId = p.UrunId,
                    KullaniciId = p.KullaniciId,
                    KullaniciAdi = p.Kullanici?.UserName,
                    PuanDegeri = p.PuanDegeri,
                    PuanTarihi = p.PuanTarihi
                }).ToList(),
                Yorumlar = urun.Yorumlar.Select(y => new YorumDto
                {
                    Id = y.Id,
                    UrunId = y.UrunId,
                    Icerik = y.Icerik,
                    Tarih = y.Tarih,
                    KullaniciId = y.YorumYapanKullaniciId,
                    KullaniciAdi = y.YorumYapanKullanici?.UserName
                }).ToList(),
                Resimler = urun.Resimler?.Select(r => new UrunResimDto { Id = r.Id, Url = r.Url }).ToList(),
                Videolar = urun.Videolar?.Select(v => new UrunVideoDto { Id = v.Id, Url = v.Url }).ToList()
            };
        }

        private static AdminYorumDto MapYorumDto(Yorum yorum)
        {
            return new AdminYorumDto
            {
                Id = yorum.Id,
                UrunId = yorum.UrunId,
                UrunAdi = yorum.Urun?.Adi,
                KullaniciId = yorum.YorumYapanKullaniciId,
                KullaniciAdi = yorum.YorumYapanKullanici?.UserName,
                KullaniciEmail = yorum.YorumYapanKullanici?.Email,
                Icerik = yorum.Icerik,
                Tarih = yorum.Tarih,
                AktifMi = yorum.AktifMi
            };
        }
    }
}
