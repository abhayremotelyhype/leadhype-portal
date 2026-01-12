namespace LeadHype.Api.Core.Models.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserInfo User { get; set; }
    }
    
    public class UserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; }
        public List<string> AssignedClientIds { get; set; } = new List<string>();
        public DateTime? LastLoginAt { get; set; }
        public string? ApiKey { get; set; }
        public DateTime? ApiKeyCreatedAt { get; set; }
    }
}