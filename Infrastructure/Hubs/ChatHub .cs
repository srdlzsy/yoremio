using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Mesaj gönderme metodu
        public async Task SendMessage(string fromUserId, string toUserId, string message)
        {
            var contextUserId = Context.UserIdentifier;

            // Kullanıcının kendisi gibi davranıp davranmadığını kontrol et
            if (contextUserId != fromUserId)
                throw new HubException("Kimlik doğrulama hatası.");

            if (string.IsNullOrWhiteSpace(toUserId) || string.IsNullOrWhiteSpace(message))
                throw new HubException("Geçersiz alıcı veya mesaj.");

            // Mesajı alıcıya gönder
            await Clients.User(toUserId).SendAsync("ReceiveMessage", fromUserId, message);

            // Gönderene de mesaj onayı göndermek istiyorsan (frontend bu event’i dinlemiyor ama ekleyebilirsin)
            // await Clients.Caller.SendAsync("MessageSent", toUserId, message);
        }

        // Yazıyor indikatörü
        public async Task Typing(string toUserId)
        {
            var fromUserId = Context.UserIdentifier;

            if (string.IsNullOrWhiteSpace(fromUserId) || string.IsNullOrWhiteSpace(toUserId))
                return;

            await Clients.User(toUserId).SendAsync("Typing", fromUserId);
        }

        // Bağlantı olayı
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? "unknown";
            await Clients.All.SendAsync("UserConnected", userId);
            await base.OnConnectedAsync();
        }

        // Bağlantı kopma olayı
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier ?? "unknown";
            await Clients.All.SendAsync("UserDisconnected", userId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
