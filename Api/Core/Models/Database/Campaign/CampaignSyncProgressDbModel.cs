namespace LeadHype.Api.Models;

/// <summary>
/// Tracks campaign lead sync progress for resume capability
/// </summary>
public class CampaignSyncProgressDbModel
{
    public int CampaignId { get; set; }
    public string? LastProcessedLeadId { get; set; }
    public string? LastProcessedLeadEmail { get; set; }
    public int TotalLeadsInCampaign { get; set; }
    public int LeadsProcessed { get; set; }
    public DateTime? SyncStartedAt { get; set; }
    public DateTime? SyncCompletedAt { get; set; }
    public string SyncStatus { get; set; } = "not_started"; // not_started, in_progress, completed, failed, partial
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
