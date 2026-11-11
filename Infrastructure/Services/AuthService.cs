using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Extensions;
using Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ISaticiProfiliRepository _saticiProfiliRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailSend _emailSender;
        private readonly VerificationOptions _verificationOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ISaticiProfiliRepository saticiProfiliRepository,
            IConfiguration configuration,
            IEmailSend emailSender,
            IOptions<VerificationOptions> verificationOptions,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _saticiProfiliRepository = saticiProfiliRepository;
            _configuration = configuration;
            _emailSender = emailSender;
            _verificationOptions = verificationOptions.Value;
            _logger = logger;
        }

        public async Task<(bool Succeeded, string? Error)> RegisterSaticiAsync(RegisterSaticiDto dto)
        {
            var normalizedEmail = dto.Email.Trim();
            var normalizedVergiNo = dto.VergiNo.Trim();

            var existingProfiles = await _saticiProfiliRepository.FindAsync(s => s.VergiNo == normalizedVergiNo);
            if (existingProfiles.Any())
            {
                return (false, "Bu vergi numarasi zaten kayitli.");
            }

            var requireEmailConfirmation = _verificationOptions.RequireConfirmedEmailForSellerLogin;

            var user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                PhoneNumber = dto.PhoneNumber.Trim(),
                EmailConfirmed = !requireEmailConfirmation,
                PhoneNumberConfirmed = true,
                SaticiProfili = new SaticiProfili
                {
                    MagazaAdi = dto.MagazaAdi.Trim(),
                    VergiNo = normalizedVergiNo,
                    Adres = string.IsNullOrWhiteSpace(dto.Adres) ? null : dto.Adres.Trim(),
                    Sehir = string.IsNullOrWhiteSpace(dto.Sehir) ? null : dto.Sehir.Trim(),
                    Ilce = string.IsNullOrWhiteSpace(dto.Ilce) ? null : dto.Ilce.Trim(),
                    AktifMi = !requireEmailConfirmation
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

            var deliveryFailures = new List<string>();

            if (requireEmailConfirmation)
            {
                try
                {
                    await SendEmailConfirmationAsync(user);
                }
                catch (Exception ex)
                {
                    deliveryFailures.Add("Email: " + ex.Message);
                    _logger.LogWarning(
                        ex,
                        "Satici email dogrulama mesaji gonderilemedi. UserId: {UserId}, Email: {Email}",
                        user.Id,
                        user.Email);
                }
            }

            if (deliveryFailures.Count > 0)
            {
                return (true, "Dogrulama mesaji gonderilemedi: " + string.Join(" | ", deliveryFailures));
            }

            return (true, null);
        }

        public async Task<(bool Succeeded, string? Error)> RegisterAliciAsync(RegisterAliciDto dto)
        {
            var normalizedEmail = dto.Email.Trim();
            var desiredUserName = string.IsNullOrWhiteSpace(dto.UserName) ? normalizedEmail : dto.UserName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.UserName))
            {
                var existingUserName = await _userManager.FindByNameAsync(desiredUserName);
                if (existingUserName != null)
                {
                    return (false, "Bu kullanici adi zaten kullaniliyor.");
                }
            }

            var user = new ApplicationUser
            {
                UserName = desiredUserName,
                Email = normalizedEmail,
                AliciProfili = new AliciProfili
                {
                    Adres = string.IsNullOrWhiteSpace(dto.Adres) ? null : dto.Adres.Trim(),
                    Sehir = string.IsNullOrWhiteSpace(dto.Sehir) ? null : dto.Sehir.Trim(),
                    Ilce = string.IsNullOrWhiteSpace(dto.Ilce) ? null : dto.Ilce.Trim(),
                    AktifMi = true
                }
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
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
            if (user == null)
            {
                return (false, null, "Gecersiz email veya sifre.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return (false, null, "Cok fazla basarisiz giris denemesi. Hesabiniz gecici olarak kilitlendi.");
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (signInResult.IsLockedOut)
            {
                return (false, null, "Cok fazla basarisiz giris denemesi. Hesabiniz gecici olarak kilitlendi.");
            }

            if (!signInResult.Succeeded)
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

            if (await _userManager.IsEmailConfirmedAsync(user))
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

            string decodedToken;
            try
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(token);
                decodedToken = Encoding.UTF8.GetString(decodedBytes);
            }
            catch (FormatException)
            {
                return false;
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
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

            if (await _userManager.IsLockedOutAsync(user))
            {
                return (false, user.Id);
            }

            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                "EmailConfirmationCode",
                code.Trim());

            if (!isValid)
            {
                await _userManager.AccessFailedAsync(user);
                return (false, user.Id);
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
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

        private string BuildEmailConfirmationLink(string userId, string encodedEmailToken)
        {
            var baseUrl = GetPublicBaseUrl();
            return $"{baseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(encodedEmailToken)}";
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
