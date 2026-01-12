using System;
using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.Auth
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        [Required]
        public string Role { get; set; } // "Admin" or "User"
        
        public string? FirstName { get; set; }
        
        public string? LastName { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public string? RefreshToken { get; set; }
        
        public DateTime? RefreshTokenExpiryTime { get; set; }
        
        public List<string> AssignedClientIds { get; set; } = new List<string>();
        
        public string? ApiKey { get; set; }
        
        public DateTime? ApiKeyCreatedAt { get; set; }
    }
    
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}