namespace Application.DTOs
{
    public class AuthMeDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public bool EmailConfirmed { get; set; }
    }
}
