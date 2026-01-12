using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.Auth
{
    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
        
        [Required]
        [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be either 'Admin' or 'User'")]
        public string Role { get; set; }
        
        [StringLength(50)]
        public string? FirstName { get; set; }
        
        [StringLength(50)]
        public string? LastName { get; set; }
        
        [StringLength(50)]
        public string? Username { get; set; }
        
        public List<string>? AssignedClientIds { get; set; }
    }
}