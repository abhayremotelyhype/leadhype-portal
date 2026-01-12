using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api;

public class CreateAccountRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
        
    [Required]
    public string Password { get; set; } = string.Empty;
        
    [Required]
    public string AccountType { get; set; } = string.Empty;
}