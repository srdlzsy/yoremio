using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IChatMessageRepository : IBaseRepository<ChatMessage>
    {
        Task<bool> UserExistsAsync(string userId);
        Task<ChatMessage?> GetByIdWithUsersAsync(long id);
        Task<IReadOnlyCollection<ChatMessage>> GetRecentMessagesForUserAsync(string userId, int take);
        Task<IReadOnlyCollection<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId, int skip, int take);
        Task<int> GetConversationMessageCountAsync(string firstUserId, string secondUserId);
        Task<int> GetUnreadCountAsync(string receiverId, string senderId);
        Task<int> MarkConversationReadAsync(string receiverId, string senderId, DateTime readAt);
    }
}
