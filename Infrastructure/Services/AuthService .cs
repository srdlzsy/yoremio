using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

using System.Text;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IServiceManager _serviceManager;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IServiceManager serviceManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _serviceManager = serviceManager;
        }

        public async Task<(bool Succeeded, string? Error)> RegisterSaticiAsync(RegisterSaticiDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Rol = "Satici",
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                SaticiProfili = new SaticiProfili
                {
                    MagazaAdi = dto.MagazaAdi,
                    VergiNo = dto.VergiNo,
                    Adres = dto.Adres,
                    AktifMi = false

                }

            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return (false, string.Join(" | ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Satici");

            // E-posta doğrulama tokenı oluştur
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));
            var emailConfirmationLink = $"https://senin-site.com/api/auth/confirmemail?userId={user.Id}&token={encodedEmailToken}";

            try
            {
                // E-posta gönder
                await _serviceManager.EmailSender.SendEmailAsync(user.Email, "E-Posta Doğrulama",
                    $"Lütfen e-posta adresinizi doğrulamak için <a href='{emailConfirmationLink}'>buraya tıklayın</a>");
            }
            catch (Exception ex)
            {
                // E-posta gönderilemezse kullanıcıyı sil
                await _userManager.DeleteAsync(user);
                return (false, "E-posta gönderilemedi: " + ex.Message);
            }

            try
            {
                // Telefon doğrulama kodu oluştur
                var smsCode = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
                // SMS gönder
                await _serviceManager.SmsSender.SendSmsAsync(user.PhoneNumber, $"Doğrulama kodunuz: {smsCode}");
            }
            catch (Exception ex)
            {
                // SMS gönderilemezse kullanıcıyı sil
                await _userManager.DeleteAsync(user);
                return (false, "SMS gönderilemedi: " + ex.Message);
            }

            return (true, null);
        }

        public async Task<(bool Succeeded, string? Error)> RegisterAliciAsync(RegisterAliciDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Rol = "Alici",

            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return (false, string.Join(" | ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Alici");

            return (true, null);
        }

   public async Task<(bool Succeeded, string? Token, string? Error)> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return (false, null, "Geçersiz email veya şifre.");
            if (user.Rol == "SaticiProfili")
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                    return (false, null, "Email doğrulanmamış.");
            }

           

            var token = user.JwtGenerateToken(_configuration); // ✅ Extension metodu
            return (true, token, null);
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            var decodedToken = Encoding.UTF8.GetString(decodedBytes);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            return result.Succeeded;
        }

        public async Task<bool> ConfirmPhoneAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.PhoneNumber))
                return false;

            var isValid = await _userManager.VerifyChangePhoneNumberTokenAsync(user, token, user.PhoneNumber);
            if (!isValid)
                return false;

            // ❗ Telefon doğrulama başarılıysa veritabanına yansıt
            user.PhoneNumberConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }


      

    }
}