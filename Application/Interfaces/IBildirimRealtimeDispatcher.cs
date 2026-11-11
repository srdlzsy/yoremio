using Application.DTOs;

namespace Application.Interfaces
{
    public interface IBildirimRealtimeDispatcher
    {
        Task DispatchNotificationAsync(string kullaniciId, BildirimDto bildirim, int unreadCount);
        Task DispatchUnreadCountAsync(string kullaniciId, int unreadCount);
    }
}
