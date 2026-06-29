using Application.DTOs;
using Application.Interfaces;
using System.Collections.Concurrent;

namespace Infrastructure.Services
{
    public sealed class InMemoryVerificationOutbox : IVerificationOutbox
    {
        private const int MaxMessages = 100;
        private readonly ConcurrentQueue<VerificationOutboxMessageDto> _messages = new();

        public void Add(string channel, string to, string? subject, string body)
        {
            _messages.Enqueue(new VerificationOutboxMessageDto(
                Guid.NewGuid(),
                channel,
                to,
                subject,
                body,
                DateTime.UtcNow));

            while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
            {
            }
        }

        public IReadOnlyCollection<VerificationOutboxMessageDto> GetMessages()
        {
            return _messages
                .OrderByDescending(message => message.CreatedAtUtc)
                .ToArray();
        }

        public void Clear()
        {
            while (_messages.TryDequeue(out _))
            {
            }
        }
    }
}
