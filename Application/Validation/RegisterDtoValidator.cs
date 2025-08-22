// Application/Validators/RegisterDtoValidator.cs
using FluentValidation;
using Application.DTOs;

public class RegisterDtoValidator : AbstractValidator<RegisterAliciDto>
{
    public RegisterDtoValidator()
    {
     
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");

    }
}
