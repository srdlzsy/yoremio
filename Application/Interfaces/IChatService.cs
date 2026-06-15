using Application.DTOs;

namespace Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatMessageDto> SendMessageAsync(string senderId, string receiverId, string message);
        Task<IReadOnlyCollection<ChatConversationDto>> GetConversationsAsync(string currentUserId);
        Task<ChatConversationMessagesDto> GetConversationMessagesAsync(string currentUserId, string otherUserId, int page, int pageSize);
        Task<int> MarkConversationReadAsync(string currentUserId, string otherUserId);
    }
}
