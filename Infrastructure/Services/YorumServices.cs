using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class YorumServices : IYorumServices
    {
        private readonly IYorumRepository _yorumRepository;
        private readonly IUrunRepository _urunRepository;
        public YorumServices(IYorumRepository yorumRepository,IUrunRepository urunRepository)
        {
            _yorumRepository = yorumRepository;
            _urunRepository = urunRepository;

        }
        public async Task<YorumDto> YorumEkleAsync(YorumEkleDto dto, string kullaniciId)
        {
            var urun = await _urunRepository.GetByIdAsync(dto.UrunId);
            if (urun == null)
                throw new Exception("Ürün bulunamadı.");

            var yorum = new Yorum
            {
                YorumYapanKullaniciId = kullaniciId,
                UrunId = dto.UrunId,
                Icerik = dto.Icerik,
                Tarih = DateTime.UtcNow


            };

            await _yorumRepository.AddAsync(yorum);
            await _yorumRepository.SaveChangesAsync();

            return new YorumDto
            {
                Id = yorum.Id,
                Icerik = yorum.Icerik,
                Tarih = yorum.Tarih,
                KullaniciId = yorum.YorumYapanKullaniciId,
                UrunId = yorum.UrunId,
                KullaniciAdi = yorum.YorumYapanKullanici?.UserName


            };
        }

        public async Task<IEnumerable<YorumDto>> GetYorumlarByUrunIdAsync(int urunId)
        {
            var yorumlar = await _yorumRepository.GetYorumlarByUrunIdAsync(urunId);
            return yorumlar.Select(y => new YorumDto
            {
                Id = y.Id,
                Icerik = y.Icerik,
                Tarih = y.Tarih,
                KullaniciId = y.YorumYapanKullaniciId,
                KullaniciAdi = y.YorumYapanKullanici?.UserName,
                UrunId = y.UrunId
            });
        }

        public Task<YorumEkleDto> GuncelleYorumAsync(YorumEkleDto yorumDto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SilYorumAsync(int yorumId)
        {
            throw new NotImplementedException();
        }
    }
}
