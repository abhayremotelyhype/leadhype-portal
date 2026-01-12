
namespace LeadHype.Api.Models;

public class WarmupMetricsDto
{
    public string ObjectId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Email account id
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Email address this warmup data belongs to
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Total warmup emails sent
    /// </summary>
    public int TotalSent { get; set; } = 0;
    
    /// <summary>
    /// Total warmup emails replied to
    /// </summary>
    public int TotalReplied { get; set; } = 0;
    
    /// <summary>
    /// Total emails saved from spam folder
    /// </summary>
    public int TotalSavedFromSpam { get; set; } = 0;
    
    /// <summary>
    /// Daily warmup statistics
    /// </summary>
    public List<WarmupDailyData> DailyStats { get; set; } = new();
    
    /// <summary>
    /// When the warmup data was last updated
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}

public class WarmupDailyData
{
    public string Date { get; set; } = string.Empty;
    public int Sent { get; set; } = 0;
    public int Replied { get; set; } = 0;
    public int SavedFromSpam { get; set; } = 0;
}