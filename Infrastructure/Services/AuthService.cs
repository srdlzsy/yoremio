using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Extensions;
using Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSend _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly VerificationOptions _verificationOptions;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IEmailSend emailSender,
            ISmsSender smsSender,
            IOptions<VerificationOptions> verificationOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _verificationOptions = verificationOptions.Value;
        }

        public async Task<(bool Succeeded, string? Error)> RegisterSaticiAsync(RegisterSaticiDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                SaticiProfili = new SaticiProfili
                {
                    MagazaAdi = dto.MagazaAdi,
                    VergiNo = dto.VergiNo,
                    Adres = dto.Adres,
                    Sehir = dto.Sehir,
                    Ilce = dto.Ilce,
                    AktifMi = false
                }
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return (false, string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.Satici);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return (false, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
            }

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));
            var emailConfirmationLink = BuildEmailConfirmationLink(user.Id, encodedEmailToken);

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "E-Posta Doğrulama",
                    $"Lütfen e-posta adresinizi doğrulamak için <a href='{emailConfirmationLink}'>buraya tıklayın</a>");
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);
                return (false, "E-posta gönderilemedi: " + ex.Message);
            }

            try
            {
                var smsCode = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber!);
                var phoneConfirmationLink = BuildPhoneConfirmationLink(user.Id, smsCode);
                await _smsSender.SendSmsAsync(
                    user.PhoneNumber!,
                    $"Yoremio telefon doğrulama kodunuz: {smsCode}. Doğrulama bağlantısı: {phoneConfirmationLink}");
            }
            catch (Exception ex)
            {
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
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return (false, string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.Alici);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return (false, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
            }

            return (true, null);
        }

        public async Task<(bool Succeeded, string? Token, string? Error)> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return (false, null, "Geçersiz email veya şifre.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var role = userRoles.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(role))
            {
                return (false, null, "Kullanıcı rolü bulunamadı.");
            }

            if (string.Equals(role, ApplicationRoles.Satici, StringComparison.Ordinal) &&
                !await _userManager.IsEmailConfirmedAsync(user))
            {
                return (false, null, "Email doğrulanmamış.");
            }

            var token = user.JwtGenerateToken(_configuration, role);
            return (true, token, null);
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            var decodedToken = Encoding.UTF8.GetString(decodedBytes);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            return result.Succeeded;
        }

        public async Task<bool> ConfirmPhoneAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.PhoneNumber))
            {
                return false;
            }

            var isValid = await _userManager.VerifyChangePhoneNumberTokenAsync(user, token, user.PhoneNumber);
            if (!isValid)
            {
                return false;
            }

            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, token);
            return result.Succeeded;
        }

        private string BuildEmailConfirmationLink(string userId, string encodedEmailToken)
        {
            var baseUrl = GetPublicBaseUrl();
            return $"{baseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(encodedEmailToken)}";
        }

        private string BuildPhoneConfirmationLink(string userId, string token)
        {
            var baseUrl = GetPublicBaseUrl();
            return $"{baseUrl}/api/auth/confirm-phone?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
        }

        private string GetPublicBaseUrl()
        {
            var baseUrl = _verificationOptions.PublicBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Verification:PublicBaseUrl ayarı boş olamaz.");
            }

            return baseUrl.TrimEnd('/');
        }
    }
}
