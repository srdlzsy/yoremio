using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetCurrentUserId();
            var conversations = await _chatService.GetConversationsAsync(userId);

            return Ok(ApiResponse<IReadOnlyCollection<ChatConversationDto>>.Ok(
                conversations,
                "Konuşmalar getirildi.",
                HttpContext.TraceIdentifier));
        }

        [HttpGet("messages/{otherUserId}")]
        public async Task<IActionResult> GetMessages(string otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            var messages = await _chatService.GetConversationMessagesAsync(userId, otherUserId, page, pageSize);

            return Ok(ApiResponse<ChatConversationMessagesDto>.Ok(
                messages,
                "Mesajlar getirildi.",
                HttpContext.TraceIdentifier));
        }

        [HttpPost("messages/{receiverId}")]
        public async Task<IActionResult> SendMessage(string receiverId, [FromBody] ChatSendMessageDto dto)
        {
            var senderId = GetCurrentUserId();
            var message = await _chatService.SendMessageAsync(senderId, receiverId, dto.Message);
            var receiverMessage = new ChatMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Message = message.Message,
                SentAt = message.SentAt,
                ReadAt = message.ReadAt,
                IsMine = false
            };

            await _hubContext.Clients.User(message.ReceiverId).SendAsync("ReceiveMessage", message.SenderId, message.Message);
            await _hubContext.Clients.User(message.ReceiverId).SendAsync("ReceiveMessageV2", receiverMessage);

            return Ok(ApiResponse<ChatMessageDto>.Ok(
                message,
                "Mesaj gönderildi.",
                HttpContext.TraceIdentifier));
        }

        [HttpPost("messages/{otherUserId}/read")]
        public async Task<IActionResult> MarkConversationRead(string otherUserId)
        {
            var userId = GetCurrentUserId();
            var markedCount = await _chatService.MarkConversationReadAsync(userId, otherUserId);
            var readAt = DateTime.UtcNow;

            if (markedCount > 0)
            {
                await _hubContext.Clients.User(otherUserId).SendAsync("MessagesRead", userId, readAt);
            }

            return Ok(ApiResponse<object>.Ok(
                new { markedCount, readAt },
                "Konuşma okundu olarak işaretlendi.",
                HttpContext.TraceIdentifier));
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Kullanıcı doğrulanamadı.");
            }

            return userId;
        }
    }
}
