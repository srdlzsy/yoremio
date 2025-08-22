using FluentValidation;
using Application.DTOs;
using System.Linq;

public class UrunEkleDtoValidator : AbstractValidator<UrunEkleDto>
{
    public UrunEkleDtoValidator()
    {
        RuleFor(x => x.Adi)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .MinimumLength(5).WithMessage("Ürün adı en az 5 karakter olmalıdır.");

        RuleFor(x => x.Fiyat)
            .GreaterThan(0).WithMessage("Fiyat sıfırdan büyük olmalıdır.");

        RuleFor(x => x.StokMiktari)
            .GreaterThanOrEqualTo(0).WithMessage("Stok miktarı sıfır veya daha büyük olmalıdır.");

        RuleFor(x => x.KategoriId)
            .GreaterThan(0).WithMessage("Kategori seçimi zorunludur.");

        RuleFor(x => x.Resimler)
            .Must(files => files == null || files.All(f => f.ContentType.StartsWith("image/")))
            .WithMessage("Yalnızca resim dosyaları yükleyebilirsiniz.");

        RuleFor(x => x.Videolar)
            .Must(files => files == null || files.All(f => f.ContentType.StartsWith("video/")))
            .WithMessage("Yalnızca video dosyaları yükleyebilirsiniz.");

        RuleForEach(x => x.Resimler).ChildRules(files =>
        {
            files.RuleFor(f => f.Length)
                .LessThanOrEqualTo(5 * 1024 * 1024) // 5 MB
                .WithMessage("Her resim 5 MB'dan büyük olamaz.");
        });

        RuleForEach(x => x.Videolar).ChildRules(files =>
        {
            files.RuleFor(f => f.Length)
                .LessThanOrEqualTo(50 * 1024 * 1024) // 50 MB
                .WithMessage("Her video 50 MB'dan büyük olamaz.");
        });
    }
}
