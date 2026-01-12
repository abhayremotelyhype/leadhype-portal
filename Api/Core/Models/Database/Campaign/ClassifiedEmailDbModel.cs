namespace LeadHype.Api.Models;

/// <summary>
/// Tracks emails that have been classified to avoid duplicate RevReply API calls
/// </summary>
public class ClassifiedEmailDbModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AdminUuid { get; set; } = string.Empty;
    public int CampaignId { get; set; }
    public int EmailAccountId { get; set; } // Email account ID from Smartlead for linking/mapping in frontend
    public string MessageId { get; set; } = string.Empty; // Unique message ID from Smartlead
    public string LeadEmail { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty; // SENT, REPLY, etc.
    public DateTime EmailTime { get; set; }
    public string EmailBodyHash { get; set; } = string.Empty; // SHA256 hash of email body for additional uniqueness
    
    // Classification results
    public string ClassificationResult { get; set; } = string.Empty;
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}