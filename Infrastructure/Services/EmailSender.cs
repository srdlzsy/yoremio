using Application.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class EmailSender : IEmailSend
    {
        private readonly SmtpEmailOptions _options;
        private readonly ILogger<EmailSender> _logger;
        private readonly IVerificationOutbox _verificationOutbox;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailSender(
            IOptions<SmtpEmailOptions> options,
            ILogger<EmailSender> logger,
            IVerificationOutbox verificationOutbox,
            IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _logger = logger;
            _verificationOutbox = verificationOutbox;
            _httpClientFactory = httpClientFactory;
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

            var provider = NormalizeOption(_options.Provider);
            if (UsesBrevoApi(provider))
            {
                await SendViaBrevoApiAsync(to, subject, htmlMessage, provider);
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

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException(
                    $"SMTP email gonderimi basarisiz ({host}:{_options.Port}): {detail}",
                    ex);
            }

            _logger.LogInformation("E-posta gönderildi. Provider: {Provider}, Alıcı: {Email}", _options.Provider, to);
        }

        private async Task SendViaBrevoApiAsync(string to, string subject, string htmlMessage, string provider)
        {
            var apiKey = NormalizeOption(_options.ApiKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = NormalizeOption(_options.Password);
            }

            var fromAddress = NormalizeOption(_options.FromAddress);
            var fromName = NormalizeOption(_options.FromName);
            var apiBaseUrl = NormalizeOption(_options.ApiBaseUrl);
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = "https://api.brevo.com";
            }

            ValidateBrevoApiOptions(apiKey, fromAddress);

            if (!Uri.TryCreate(apiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException("Brevo API base URL ayari gecersiz.");
            }

            var endpoint = new Uri(baseUri, "v3/smtp/email");
            var payload = new
            {
                sender = new
                {
                    email = fromAddress,
                    name = fromName
                },
                to = new[]
                {
                    new
                    {
                        email = to
                    }
                },
                subject,
                htmlContent = htmlMessage
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient(nameof(EmailSender));
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));

            using var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Brevo API email gonderimi basarisiz. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(responseBody)}");
            }

            _logger.LogInformation("E-posta Brevo API ile gonderildi. Provider: {Provider}, Alıcı: {Email}", provider, to);
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

        private void ValidateBrevoApiOptions(string apiKey, string fromAddress)
        {
            if (string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException("Brevo API ayarlari eksik. Email:Smtp:ApiKey ve Email:Smtp:FromAddress degerlerini girin.");
            }

            if (ContainsPlaceholder(apiKey) || ContainsPlaceholder(fromAddress))
            {
                throw new InvalidOperationException("Brevo API ayarlari placeholder deger iceriyor. Gercek API key ve sender email girin.");
            }
        }

        private static bool ContainsPlaceholder(string value)
        {
            return value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool UsesBrevoApi(string provider)
        {
            return provider.Equals("BrevoApi", StringComparison.OrdinalIgnoreCase) ||
                   provider.Equals("BrevoHttp", StringComparison.OrdinalIgnoreCase) ||
                   provider.Equals("BrevoTransactionalApi", StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string value)
        {
            const int maxLength = 500;
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength] + "...";
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
