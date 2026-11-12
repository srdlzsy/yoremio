using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ChatMessageDto
    {
        public long Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public int? UrunId { get; set; }
        public string? UrunAdi { get; set; }
        public int? TalepId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class ChatConversationDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? MagazaAdi { get; set; }
        public string? Email { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public string LastSenderId { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int? LastUrunId { get; set; }
        public string? LastUrunAdi { get; set; }
        public int? LastTalepId { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatSendMessageDto
    {
        [Required(ErrorMessage = "Mesaj boş olamaz.")]
        [MaxLength(1000, ErrorMessage = "Mesaj en fazla 1000 karakter olabilir.")]
        public string Message { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int? UrunId { get; set; }

        [Range(1, int.MaxValue)]
        public int? TalepId { get; set; }
    }

    public class ChatConversationMessagesDto : PagedResult<ChatMessageDto>
    {
        public string OtherUserId { get; set; } = string.Empty;
    }
}
