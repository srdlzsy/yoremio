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
            var requireEmailConfirmation = _verificationOptions.RequireConfirmedEmailForSellerLogin;
            var requirePhoneConfirmation = _verificationOptions.RequireConfirmedPhoneForSellerLogin;

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = !requireEmailConfirmation,
                PhoneNumberConfirmed = !requirePhoneConfirmation,
                SaticiProfili = new SaticiProfili
                {
                    MagazaAdi = dto.MagazaAdi,
                    VergiNo = dto.VergiNo,
                    Adres = dto.Adres,
                    Sehir = dto.Sehir,
                    Ilce = dto.Ilce,
                    AktifMi = !requireEmailConfirmation && !requirePhoneConfirmation
                },
                AliciProfili = new AliciProfili
                {
                    AktifMi = true
                }
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return (false, string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await _userManager.AddToRolesAsync(user, new[] { ApplicationRoles.Satici, ApplicationRoles.Alici });
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return (false, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
            }

            try
            {
                if (requireEmailConfirmation)
                {
                    await SendEmailConfirmationAsync(user);
                }

                if (requirePhoneConfirmation)
                {
                    await SendPhoneConfirmationAsync(user);
                }
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);
                return (false, "Dogrulama mesaji gonderilemedi: " + ex.Message);
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
                return (false, null, "Gecersiz email veya sifre.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Count == 0)
            {
                return (false, null, "Kullanici rolu bulunamadi.");
            }

            if (userRoles.Contains(ApplicationRoles.Satici) &&
                _verificationOptions.RequireConfirmedEmailForSellerLogin &&
                !await _userManager.IsEmailConfirmedAsync(user))
            {
                return (false, null, "Email dogrulanmamis.");
            }

            if (userRoles.Contains(ApplicationRoles.Satici) &&
                _verificationOptions.RequireConfirmedPhoneForSellerLogin &&
                !await _userManager.IsPhoneNumberConfirmedAsync(user))
            {
                return (false, null, "Telefon dogrulanmamis.");
            }

            var token = user.JwtGenerateToken(_configuration, userRoles);
            return (true, token, null);
        }

        public async Task<(bool Succeeded, string? Error)> ResendVerificationAsync(ResendVerificationDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return (true, null);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(ApplicationRoles.Satici))
            {
                return (true, null);
            }

            if (await _userManager.IsEmailConfirmedAsync(user) &&
                await _userManager.IsPhoneNumberConfirmedAsync(user))
            {
                return (true, null);
            }

            try
            {
                if (_verificationOptions.RequireConfirmedEmailForSellerLogin &&
                    !await _userManager.IsEmailConfirmedAsync(user))
                {
                    await SendEmailConfirmationAsync(user);
                }

                if (_verificationOptions.RequireConfirmedPhoneForSellerLogin &&
                    !await _userManager.IsPhoneNumberConfirmedAsync(user))
                {
                    await SendPhoneConfirmationAsync(user);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "Dogrulama mesaji gonderilemedi: " + ex.Message);
            }
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

        public async Task<(bool Succeeded, string? UserId)> ConfirmEmailCodeAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return (false, null);
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return (true, user.Id);
            }

            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                "EmailConfirmationCode",
                code.Trim());

            if (!isValid)
            {
                return (false, user.Id);
            }

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
            return (result.Succeeded, user.Id);
        }

        public async Task<(bool Succeeded, string? UserId)> ConfirmPhoneCodeAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null || string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                return (false, null);
            }

            if (await _userManager.IsPhoneNumberConfirmedAsync(user))
            {
                return (true, user.Id);
            }

            var isValid = await _userManager.VerifyChangePhoneNumberTokenAsync(user, code.Trim(), user.PhoneNumber);
            if (!isValid)
            {
                return (false, user.Id);
            }

            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, code.Trim());
            return (result.Succeeded, user.Id);
        }

        private async Task SendEmailConfirmationAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException("Kullanici email adresi bos olamaz.");
            }

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));
            var emailConfirmationLink = BuildEmailConfirmationLink(user.Id, encodedEmailToken);
            var emailCode = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, "EmailConfirmationCode");

            await _emailSender.SendEmailAsync(
                user.Email,
                "Yoremio email dogrulama",
                $"Yoremio email dogrulama kodunuz: <strong>{emailCode}</strong><br />" +
                $"Isterseniz bu linkten de dogrulayabilirsiniz: <a href='{emailConfirmationLink}'>Email adresimi dogrula</a>");
        }

        private async Task SendPhoneConfirmationAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                throw new InvalidOperationException("Kullanici telefon numarasi bos olamaz.");
            }

            var smsCode = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
            var phoneConfirmationLink = BuildPhoneConfirmationLink(user.Id, smsCode);

            await _smsSender.SendSmsAsync(
                user.PhoneNumber,
                $"Yoremio telefon dogrulama kodunuz: {smsCode}. Dogrulama baglantisi: {phoneConfirmationLink}");
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
                throw new InvalidOperationException("Verification:PublicBaseUrl ayari bos olamaz.");
            }

            return baseUrl.TrimEnd('/');
        }
    }
}
