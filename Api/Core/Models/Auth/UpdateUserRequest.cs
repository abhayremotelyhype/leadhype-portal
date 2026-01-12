using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Services;

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }
    
    public string? Username { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? Role { get; set; }
    
    public bool? IsActive { get; set; }
    
    public List<string>? AssignedClientIds { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}