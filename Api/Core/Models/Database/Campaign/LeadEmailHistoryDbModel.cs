namespace LeadHype.Api.Models;

public class LeadEmailHistoryDbModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AdminUuid { get; set; }
    public int CampaignId { get; set; }
    public string LeadId { get; set; } = string.Empty; // The lead ID from Smartlead
    public string LeadEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string Type { get; set; } = "SENT"; // SENT or REPLY
    public DateTime? Time { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // RevReply Classification fields
    public string? ClassificationResult { get; set; }
    public DateTime? ClassifiedAt { get; set; }
    public bool IsClassified { get; set; } = false;
}