using LeadHype.Api.Core.Database.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface ICampaignEventRepository
{
    // Event recording methods
    Task AddEventAsync(string campaignId, string eventType, int count = 1, string? emailAccountId = null, Dictionary<string, object>? metadata = null);
    Task AddBulkEventsAsync(IEnumerable<CampaignEvent> events);
    
    // Stats retrieval methods (using materialized views)
    Task<CampaignStatsNew?> GetStatsAsync(string campaignId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<CampaignStatsNew>> GetStatsForCampaignsAsync(IEnumerable<string> campaignIds, DateTime startDate, DateTime endDate, string granularity = "day");
    Task<IEnumerable<DailyStatsNew>> GetAggregatedDailyStatsAsync(DateTime startDate, DateTime endDate, List<string>? campaignIds = null);
    
    // Aggregated metrics (for dashboard)
    Task<(int TotalSent, int TotalReplied, int TotalPositive, DateTime? LastReplyDate, DateTime? LastPositiveDate)> GetAggregatedTotalsForCampaignsAsync(List<string> campaignIds);
    
    // Maintenance methods
    Task RefreshMaterializedViewsAsync();
    Task CreatePartitionForDateAsync(DateTime date);
    Task CleanupOldPartitionsAsync(int monthsToKeep = 12);
}

public class CampaignEvent
{
    public string CampaignId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int EventCount { get; set; } = 1;
    public string? EmailAccountId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class CampaignStatsNew
{
    public string CampaignId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public int Sent { get; set; }
    public int Opened { get; set; }
    public int Replied { get; set; }
    public int PositiveReplies { get; set; }
    public int Bounced { get; set; }
    public int Clicked { get; set; }
    public DateTime? LastActivity { get; set; }
    
    // Calculated properties
    public double OpenRate => Sent > 0 ? (double)Opened / Sent * 100 : 0;
    public double ReplyRate => Sent > 0 ? (double)Replied / Sent * 100 : 0;
    public double PositiveReplyRate => Sent > 0 ? (double)PositiveReplies / Sent * 100 : 0;
    public double BounceRate => Sent > 0 ? (double)Bounced / Sent * 100 : 0;
}

public class DailyStatsNew
{
    public DateTime Date { get; set; }
    public int TotalSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalReplied { get; set; }
    public int TotalPositiveReplies { get; set; }
    public int TotalBounced { get; set; }
    public int TotalClicked { get; set; }
    public int RecordCount { get; set; }
    
    // Calculated properties
    public double OpenRate => TotalSent > 0 ? (double)TotalOpened / TotalSent * 100 : 0;
    public double ReplyRate => TotalSent > 0 ? (double)TotalReplied / TotalSent * 100 : 0;
    public double PositiveReplyRate => TotalSent > 0 ? (double)TotalPositiveReplies / TotalSent * 100 : 0;
    public double BounceRate => TotalSent > 0 ? (double)TotalBounced / TotalSent * 100 : 0;
}