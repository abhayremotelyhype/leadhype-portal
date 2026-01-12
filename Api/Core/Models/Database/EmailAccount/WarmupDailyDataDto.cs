namespace LeadHype.Api.Models;

/// <summary>
/// Represents warmup metrics for a specific date
/// </summary>
public class WarmupDailyDataDto
{
    /// <summary>
    /// Date in YYYY-MM-DD format
    /// </summary>
    public string Date { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of warmup emails sent on this date
    /// </summary>
    public int Sent { get; set; } = 0;
    
    /// <summary>
    /// Number of warmup emails replied to on this date
    /// </summary>
    public int Replied { get; set; } = 0;
    
    /// <summary>
    /// Number of emails saved from spam on this date
    /// </summary>
    public int SavedFromSpam { get; set; } = 0;
    
    /// <summary>
    /// Reply rate for this date (percentage)
    /// </summary>
    public double ReplyRate => Sent > 0 ? Math.Round((double)Replied / Sent * 100, 2) : 0;
    
    /// <summary>
    /// Spam protection rate for this date (percentage)
    /// </summary>
    public double SpamProtectionRate => Sent > 0 ? Math.Round((double)SavedFromSpam / Sent * 100, 2) : 0;
}