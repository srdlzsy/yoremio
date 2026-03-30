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

        public EmailSender(IOptions<SmtpEmailOptions> options, ILogger<EmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
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

                await Task.CompletedTask;
                return;
            }

            ValidateOptions();

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            using var smtpClient = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.UserName, _options.Password),
                Timeout = _options.TimeoutSeconds * 1000
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("E-posta gönderildi. Alıcı: {Email}", to);
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.Host) ||
                string.IsNullOrWhiteSpace(_options.UserName) ||
                string.IsNullOrWhiteSpace(_options.Password) ||
                string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                throw new InvalidOperationException("SMTP ayarları eksik. Email:Smtp bölümünü doldurun.");
            }

            if (_options.Port <= 0)
            {
                throw new InvalidOperationException("SMTP port ayarı geçersiz.");
            }
        }
    }
}