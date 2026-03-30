using Application.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Infrastructure.Services
{
    public class SmsSender : ISmsSender
    {
        private readonly HttpClient _httpClient;
        private readonly TwilioSmsOptions _options;
        private readonly ILogger<SmsSender> _logger;

        public SmsSender(HttpClient httpClient, IOptions<TwilioSmsOptions> options, ILogger<SmsSender> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            if (_options.UseMockSender)
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    throw new ArgumentException("Telefon numarası boş olamaz.", nameof(phoneNumber));

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentException("SMS mesajı boş olamaz.", nameof(message));

                _logger.LogWarning(
                    "Mock SMS sender aktif. SMS gönderimi atlandı. To: {Phone}, Message: {Message}",
                    phoneNumber,
                    message);

                return;
            }

            ValidateOptions();

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Telefon numarası boş olamaz.", nameof(phoneNumber));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("SMS mesajı boş olamaz.", nameof(message));

            var requestUri = $"{_options.ApiBaseUrl.TrimEnd('/')}/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Basic", credentials)
                },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = phoneNumber,
                    ["From"] = _options.FromNumber,
                    ["Body"] = message
                })
            };

            using var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"SMS gönderimi başarısız. Durum: {(int)response.StatusCode}, Yanıt: {responseBody}");
            }

            _logger.LogInformation("SMS gönderildi. Alıcı: {Phone}", phoneNumber);
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.ApiBaseUrl) ||
                string.IsNullOrWhiteSpace(_options.AccountSid) ||
                string.IsNullOrWhiteSpace(_options.AuthToken) ||
                string.IsNullOrWhiteSpace(_options.FromNumber))
            {
                throw new InvalidOperationException("SMS sağlayıcı ayarları eksik. Sms:Twilio bölümünü doldurun.");
            }
        }
    }

}
