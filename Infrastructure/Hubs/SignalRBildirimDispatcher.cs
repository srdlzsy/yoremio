using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs
{
    public sealed class SignalRBildirimDispatcher : IBildirimRealtimeDispatcher
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRBildirimDispatcher(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task DispatchNotificationAsync(string kullaniciId, BildirimDto bildirim, int unreadCount)
        {
            if (string.IsNullOrWhiteSpace(kullaniciId))
            {
                return;
            }

            bildirim.OkunmamisSayisi = unreadCount;
            await _hubContext.Clients.User(kullaniciId).SendAsync("NotificationReceived", bildirim);
            await _hubContext.Clients.User(kullaniciId).SendAsync("UnreadNotificationCountUpdated", unreadCount);
        }

        public async Task DispatchUnreadCountAsync(string kullaniciId, int unreadCount)
        {
            if (string.IsNullOrWhiteSpace(kullaniciId))
            {
                return;
            }

            await _hubContext.Clients.User(kullaniciId).SendAsync("UnreadNotificationCountUpdated", unreadCount);
        }
    }
}
