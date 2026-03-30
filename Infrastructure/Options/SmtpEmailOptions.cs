namespace Infrastructure.Options
{
    public class SmtpEmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Yoremio";
        public int TimeoutSeconds { get; set; } = 30;
        public bool UseMockSender { get; set; }
    }
}
