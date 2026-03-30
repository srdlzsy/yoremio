using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private const int MaxMessageLength = 1000;

        // SignalR method overloading desteklemez; tek SendMessage girişi kullanılır.
        public async Task SendMessage(string toUserId, string message)
        {
            var contextUserId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(contextUserId))
                throw new HubException("Kimlik doğrulama hatası.");

            await SendMessageInternalAsync(contextUserId, toUserId, message);
        }

        // Eski istemciler için alternatif isimle uyumluluk metodu.
        public async Task SendMessageLegacy(string fromUserId, string toUserId, string message)
        {
            var contextUserId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(contextUserId))
                throw new HubException("Kimlik doğrulama hatası.");

            if (!string.IsNullOrWhiteSpace(fromUserId) && !string.Equals(contextUserId, fromUserId, StringComparison.Ordinal))
                throw new HubException("Kimlik doğrulama hatası.");

            await SendMessageInternalAsync(contextUserId, toUserId, message);
        }

        private async Task SendMessageInternalAsync(string fromUserId, string toUserId, string message)
        {
            toUserId = toUserId?.Trim() ?? string.Empty;
            message = message?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(toUserId) || string.IsNullOrWhiteSpace(message))
                throw new HubException("Geçersiz alıcı veya mesaj.");

            if (message.Length > MaxMessageLength)
                throw new HubException($"Mesaj en fazla {MaxMessageLength} karakter olabilir.");

            await Clients.User(toUserId).SendAsync("ReceiveMessage", fromUserId, message);
            await Clients.Caller.SendAsync("MessageSent", toUserId, message, DateTimeOffset.UtcNow);
        }

        public async Task Typing(string toUserId)
        {
            var fromUserId = Context.UserIdentifier;
            toUserId = toUserId?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fromUserId) || string.IsNullOrWhiteSpace(toUserId))
                return;

            await Clients.User(toUserId).SendAsync("Typing", fromUserId);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Clients.Caller.SendAsync("Connected", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
