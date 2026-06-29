using Application.DTOs;

namespace Application.Interfaces
{
    public interface IVerificationOutbox
    {
        void Add(string channel, string to, string? subject, string body);
        IReadOnlyCollection<VerificationOutboxMessageDto> GetMessages();
        void Clear();
    }
}
