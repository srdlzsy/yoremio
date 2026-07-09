using Application.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    public class EmailSender : IEmailSend
    {
        private readonly SmtpEmailOptions _options;
        private readonly ILogger<EmailSender> _logger;
        private readonly IVerificationOutbox _verificationOutbox;

        public EmailSender(
            IOptions<SmtpEmailOptions> options,
            ILogger<EmailSender> logger,
            IVerificationOutbox verificationOutbox)
        {
            _options = options.Value;
            _logger = logger;
            _verificationOutbox = verificationOutbox;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            if (_options.UseMockSender)
            {
                if (string.IsNullOrWhiteSpace(to))
                {
                    throw new ArgumentException("Alıcı email adresi boş olamaz.", nameof(to));
                }

                _logger.LogWarning(
                    "Mock email sender aktif. Email gönderimi atlandı. To: {Email}, Subject: {Subject}, Body: {Body}",
                    to,
                    subject,
                    htmlMessage);
                _verificationOutbox.Add("email", to, subject, htmlMessage);

                await Task.CompletedTask;
                return;
            }

            var host = NormalizeOption(_options.Host);
            var userName = NormalizeOption(_options.UserName);
            var password = NormalizeOption(_options.Password);
            var fromAddress = NormalizeOption(_options.FromAddress);
            var fromName = NormalizeOption(_options.FromName);

            ValidateOptions(host, userName, password, fromAddress);

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            using var smtpClient = new SmtpClient(host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(userName, password),
                Timeout = _options.TimeoutSeconds * 1000
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("E-posta gönderildi. Provider: {Provider}, Alıcı: {Email}", _options.Provider, to);
        }

        private void ValidateOptions(string host, string userName, string password, string fromAddress)
        {
            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException("SMTP ayarları eksik. Email:Smtp bölümünü doldurun.");
            }

            if (ContainsPlaceholder(userName) ||
                ContainsPlaceholder(password) ||
                ContainsPlaceholder(fromAddress))
            {
                throw new InvalidOperationException("SMTP ayarları placeholder değer içeriyor. Gerçek Email:Smtp credential bilgilerini girin.");
            }

            if (_options.Port <= 0)
            {
                throw new InvalidOperationException("SMTP port ayarı geçersiz.");
            }
        }

        private static bool ContainsPlaceholder(string value)
        {
            return value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeOption(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim();
            if (normalized.Length >= 2 &&
                ((normalized[0] == '"' && normalized[^1] == '"') ||
                 (normalized[0] == '\'' && normalized[^1] == '\'')))
            {
                normalized = normalized[1..^1];
            }

            return normalized
                .Replace("\\r", string.Empty, StringComparison.Ordinal)
                .Replace("\\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Trim();
        }
    }
}
