namespace Application.DTOs
{
    public sealed record VerificationOutboxMessageDto(
        Guid Id,
        string Channel,
        string To,
        string? Subject,
        string Body,
        DateTime CreatedAtUtc);
}
