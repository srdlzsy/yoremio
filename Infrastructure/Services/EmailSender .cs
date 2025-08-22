using Application.Interfaces; // DİKKAT! Doğru namespace

namespace Infrastructure.Services
{
    public class EmailSender : IEmailSend
    {
        public Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            Console.WriteLine($"Email gönderildi: {to} | {subject} | {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}