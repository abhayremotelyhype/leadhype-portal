using System;

namespace LeadHype.Api.Core.Models.Auth
{
    public class UserSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string UserId { get; set; }
        
        public string RefreshToken { get; set; }
        
        public DateTime RefreshTokenExpiryTime { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        
        public string? DeviceName { get; set; }
        
        public string? IpAddress { get; set; }
        
        public string? UserAgent { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}