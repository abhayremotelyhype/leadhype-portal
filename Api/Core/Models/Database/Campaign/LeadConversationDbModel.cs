using LeadHype.Api.Core.Models;

namespace LeadHype.Api.Models;

public class LeadConversationDbModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AdminUuid { get; set; }
    public int CampaignId { get; set; }
    public string LeadEmail { get; set; } = string.Empty;
    public string LeadFirstName { get; set; } = string.Empty;
    public string LeadLastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConversationData { get; set; } = string.Empty; // JSON string for conversation history
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Sync tracking fields (added in migration 002)
    public DateTime? LastSyncedAt { get; set; }
    public string SyncStatus { get; set; } = "pending"; // pending, in_progress, completed, failed
}