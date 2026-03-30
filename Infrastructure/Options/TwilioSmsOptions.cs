namespace Infrastructure.Options
{
    public class TwilioSmsOptions
    {
        public string ApiBaseUrl { get; set; } = "https://api.twilio.com";
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public bool UseMockSender { get; set; }
    }
}
