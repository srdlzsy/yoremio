using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

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

        public async Task MarkConversationRead(string otherUserId)
        {
            var contextUserId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(contextUserId))
                throw new HubException("Kimlik doğrulama hatası.");

            try
            {
                var readAt = DateTime.UtcNow;
                var markedCount = await _chatService.MarkConversationReadAsync(contextUserId, otherUserId);
                var normalizedOtherUserId = otherUserId?.Trim() ?? string.Empty;

                await Clients.Caller.SendAsync("ConversationRead", normalizedOtherUserId, markedCount, readAt);

                if (markedCount > 0)
                {
                    await Clients.User(normalizedOtherUserId).SendAsync("MessagesRead", contextUserId, readAt);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException)
            {
                throw new HubException(ex.Message);
            }
        }

        private async Task SendMessageInternalAsync(string fromUserId, string toUserId, string message)
        {
            try
            {
                var savedMessage = await _chatService.SendMessageAsync(fromUserId, toUserId, message);
                var receiverMessage = CloneForReceiver(savedMessage);

                // Legacy events: mevcut istemciler kırılmasın.
                await Clients.User(savedMessage.ReceiverId).SendAsync("ReceiveMessage", savedMessage.SenderId, savedMessage.Message);
                await Clients.Caller.SendAsync("MessageSent", savedMessage.ReceiverId, savedMessage.Message, savedMessage.SentAt);

                // V2 events: yeni istemciler id/read state dahil tam DTO alır.
                await Clients.User(savedMessage.ReceiverId).SendAsync("ReceiveMessageV2", receiverMessage);
                await Clients.Caller.SendAsync("MessageSentV2", savedMessage);
            }
            catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException)
            {
                throw new HubException(ex.Message);
            }
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

        private static ChatMessageDto CloneForReceiver(ChatMessageDto message)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Message = message.Message,
                SentAt = message.SentAt,
                ReadAt = message.ReadAt,
                IsMine = false
            };
        }
    }
}
