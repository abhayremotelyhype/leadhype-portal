using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Models;

public class Client
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [EmailAddress]
    public string? Email { get; set; }
    
    public string? Company { get; set; }
    
    public string Status { get; set; } = "active"; // active, inactive, prospect
    
    public string Color { get; set; } = "#3B82F6"; // Default blue color
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties for campaign counts (not stored in DB)
    public int CampaignCount { get; set; } = 0;
    
    public int ActiveCampaigns { get; set; } = 0;
    
    public int EmailAccountCount { get; set; } = 0;
}