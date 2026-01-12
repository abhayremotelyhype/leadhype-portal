using LeadHype.Api.Core.Models;

namespace LeadHype.Api.Models;

public class CampaignDetailsDbModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string AdminUuid { get; set; }
    public int CampaignId { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    
    public string? ClientName { get; set; }
    
    public string? ClientColor { get; set; }
    
    public string? Status { get; set; } = "active";

    public int? TotalPositiveReplies { get; set; }
    
    // Campaign metrics
    public int? TotalLeads { get; set; } = 0;
    public int? TotalSent { get; set; } = 0;
    public int? TotalOpened { get; set; } = 0;
    public int? TotalReplied { get; set; } = 0;
    public int? TotalBounced { get; set; } = 0;
    public int? TotalClicked { get; set; } = 0;
    

    //Email Ids
    public List<long> EmailIds { get; set; } = [];
    
    // Tags support
    public List<string> Tags { get; set; } = new List<string>();
    
    // Notes
    public string? Notes { get; set; }
    
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
}