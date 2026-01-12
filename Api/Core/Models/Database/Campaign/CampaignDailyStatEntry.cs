namespace LeadHype.Api.Core.Database.Models;

/// <summary>
/// Improved relational model for campaign daily statistics
/// One row per campaign per date (normalized approach)
/// </summary>
public class CampaignDailyStatEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Admin UUID for data isolation
    /// </summary>
    public string AdminUuid { get; set; }
    
    /// <summary>
    /// Internal campaign DB ID (references campaigns.id)
    /// </summary>
    public string CampaignId { get; set; }
    
    /// <summary>
    /// External Smartlead campaign ID
    /// </summary>
    public int CampaignIdInt { get; set; }
    
    /// <summary>
    /// Date for this stat entry (YYYY-MM-DD format)
    /// </summary>
    public DateTime StatDate { get; set; }
    
    /// <summary>
    /// Daily statistics for this campaign on this date
    /// </summary>
    public int Sent { get; set; } = 0;
    public int Opened { get; set; } = 0;
    public int Clicked { get; set; } = 0;
    public int Replied { get; set; } = 0;
    public int PositiveReplies { get; set; } = 0;
    public int Bounced { get; set; } = 0;
    
    /// <summary>
    /// Metadata
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}