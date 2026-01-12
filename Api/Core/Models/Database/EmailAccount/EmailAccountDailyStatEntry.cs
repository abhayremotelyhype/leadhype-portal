namespace LeadHype.Api.Core.Database.Models;

/// <summary>
/// Improved relational model for email account daily statistics
/// One row per email account per date (normalized approach)
/// </summary>
public class EmailAccountDailyStatEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Admin UUID for data isolation
    /// </summary>
    public string AdminUuid { get; set; }
    
    /// <summary>
    /// Email account ID (references email_accounts.id)
    /// </summary>
    public long EmailAccountId { get; set; }
    
    /// <summary>
    /// Date for this stat entry (YYYY-MM-DD format)
    /// </summary>
    public DateTime StatDate { get; set; }
    
    /// <summary>
    /// Daily statistics for this email account on this date
    /// </summary>
    public int Sent { get; set; } = 0;
    public int Opened { get; set; } = 0;
    public int Replied { get; set; } = 0;
    public int Bounced { get; set; } = 0;
    
    /// <summary>
    /// Metadata
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}