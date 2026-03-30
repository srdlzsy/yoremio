using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KategoriController : ControllerBase
    {
        private readonly IKategoriService _kategoriService;

        public KategoriController(IKategoriService kategoriService)
        {
            _kategoriService = kategoriService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var kategoriler = await _kategoriService.GetAllDtosAsync();
            return Ok(ApiResponse<IEnumerable<KategoriDto>>.Ok(kategoriler, "Kategoriler getirildi.", HttpContext.TraceIdentifier));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var kategori = await _kategoriService.GetDtoByIdAsync(id);
            if (kategori == null)
            {
                return NotFound(ApiResponse<object>.Fail("Kategori bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<KategoriDto>.Ok(kategori, "Kategori getirildi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] KategoriCreateDto dto)
        {
            var kategori = await _kategoriService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = kategori.Id },
                ApiResponse<KategoriDto>.Ok(kategori, "Kategori oluşturuldu.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] KategoriUpdateDto dto)
        {
            var kategori = await _kategoriService.UpdateAsync(id, dto);
            if (kategori == null)
            {
                return NotFound(ApiResponse<object>.Fail("Kategori bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<KategoriDto>.Ok(kategori, "Kategori güncellendi.", HttpContext.TraceIdentifier));
        }

        [Authorize(Roles = ApplicationRoles.Satici)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _kategoriService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.Fail("Kategori bulunamadı.", traceId: HttpContext.TraceIdentifier));
            }

            return Ok(ApiResponse<object>.Ok(null, "Kategori silindi.", HttpContext.TraceIdentifier));
        }
    }
}
