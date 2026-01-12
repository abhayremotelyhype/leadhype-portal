using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Models.UI;

public class SettingsRequest
{
    public string Id { get; set; } = string.Empty;
    
    [Required]
    public CampaignRefreshSettings CampaignRefresh { get; set; } = new();
}

public class CampaignRefreshSettings
{
    [Required]
    public bool Enabled { get; set; } = true;
    
    [Required]
    [Range(15, 300, ErrorMessage = "Interval must be between 15 and 300 minutes")]
    public int IntervalMinutes { get; set; } = 15;
}