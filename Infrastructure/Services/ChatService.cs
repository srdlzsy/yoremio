using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private const int MaxMessageLength = 1000;
        private const int MaxConversationPageSize = 100;
        private const int ConversationScanLimit = 5000;

        private readonly IChatMessageRepository _chatMessageRepository;

        public ChatService(IChatMessageRepository chatMessageRepository)
        {
            _chatMessageRepository = chatMessageRepository;
        }

        public async Task<ChatMessageDto> SendMessageAsync(string senderId, string receiverId, string message)
        {
            senderId = senderId?.Trim() ?? string.Empty;
            receiverId = receiverId?.Trim() ?? string.Empty;
            message = message?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new UnauthorizedAccessException("Kullanıcı doğrulanamadı.");
            }

            if (string.IsNullOrWhiteSpace(receiverId))
            {
                throw new ArgumentException("Alıcı kullanıcı boş olamaz.");
            }

            if (string.Equals(senderId, receiverId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Kullanıcı kendisine mesaj gönderemez.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Mesaj boş olamaz.");
            }

            if (message.Length > MaxMessageLength)
            {
                throw new ArgumentException($"Mesaj en fazla {MaxMessageLength} karakter olabilir.");
            }

            if (!await _chatMessageRepository.UserExistsAsync(receiverId))
            {
                throw new KeyNotFoundException("Alıcı kullanıcı bulunamadı.");
            }

            var entity = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.UtcNow
            };

            await _chatMessageRepository.AddAsync(entity);
            await _chatMessageRepository.SaveChangesAsync();

            return MapToDto(entity, senderId);
        }

        public async Task<IReadOnlyCollection<ChatConversationDto>> GetConversationsAsync(string currentUserId)
        {
            currentUserId = ValidateUserId(currentUserId);
            var messages = await _chatMessageRepository.GetRecentMessagesForUserAsync(currentUserId, ConversationScanLimit);

            var conversations = new List<ChatConversationDto>();
            foreach (var group in messages.GroupBy(message => GetOtherUserId(message, currentUserId)))
            {
                var lastMessage = group
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.Id)
                    .First();

                var otherUser = string.Equals(lastMessage.SenderId, currentUserId, StringComparison.Ordinal)
                    ? lastMessage.Receiver
                    : lastMessage.Sender;

                conversations.Add(new ChatConversationDto
                {
                    UserId = group.Key,
                    UserName = otherUser?.UserName,
                    Email = otherUser?.Email,
                    LastMessage = lastMessage.Message,
                    LastSenderId = lastMessage.SenderId,
                    LastMessageAt = lastMessage.SentAt,
                    UnreadCount = await _chatMessageRepository.GetUnreadCountAsync(currentUserId, group.Key)
                });
            }

            return conversations
                .OrderByDescending(conversation => conversation.LastMessageAt)
                .ToList();
        }

        public async Task<ChatConversationMessagesDto> GetConversationMessagesAsync(string currentUserId, string otherUserId, int page, int pageSize)
        {
            currentUserId = ValidateUserId(currentUserId);
            otherUserId = otherUserId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(otherUserId))
            {
                throw new ArgumentException("Konuşma kullanıcısı boş olamaz.");
            }

            if (!await _chatMessageRepository.UserExistsAsync(otherUserId))
            {
                throw new KeyNotFoundException("Konuşma kullanıcısı bulunamadı.");
            }

            var effectivePage = page < 1 ? 1 : page;
            var effectivePageSize = pageSize < 1 ? 50 : Math.Min(pageSize, MaxConversationPageSize);
            var skip = (effectivePage - 1) * effectivePageSize;

            var totalCount = await _chatMessageRepository.GetConversationMessageCountAsync(currentUserId, otherUserId);
            var messages = await _chatMessageRepository.GetConversationMessagesAsync(currentUserId, otherUserId, skip, effectivePageSize);

            return new ChatConversationMessagesDto
            {
                OtherUserId = otherUserId,
                Items = messages.Select(message => MapToDto(message, currentUserId)).ToList(),
                Page = effectivePage,
                PageSize = effectivePageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> MarkConversationReadAsync(string currentUserId, string otherUserId)
        {
            currentUserId = ValidateUserId(currentUserId);
            otherUserId = otherUserId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(otherUserId))
            {
                throw new ArgumentException("Konuşma kullanıcısı boş olamaz.");
            }

            return await _chatMessageRepository.MarkConversationReadAsync(currentUserId, otherUserId, DateTime.UtcNow);
        }

        private static ChatMessageDto MapToDto(ChatMessage message, string currentUserId)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Message = message.Message,
                SentAt = message.SentAt,
                ReadAt = message.ReadAt,
                IsMine = string.Equals(message.SenderId, currentUserId, StringComparison.Ordinal)
            };
        }

        private static string ValidateUserId(string userId)
        {
            userId = userId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Kullanıcı doğrulanamadı.");
            }

            return userId;
        }

        private static string GetOtherUserId(ChatMessage message, string currentUserId)
        {
            return string.Equals(message.SenderId, currentUserId, StringComparison.Ordinal)
                ? message.ReceiverId
                : message.SenderId;
        }
    }
}
