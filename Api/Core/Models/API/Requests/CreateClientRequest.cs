using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Models.UI;

public class CreateClientRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [EmailAddress]
    public string? Email { get; set; }
    
    public string? Company { get; set; }
    
    public string? Notes { get; set; }
}

public class UpdateClientRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [EmailAddress]
    public string? Email { get; set; }
    
    public string? Company { get; set; }
    
    public string Status { get; set; } = "active";
    
    public string? Notes { get; set; }
}