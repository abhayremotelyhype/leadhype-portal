using Dapper;
using System.Data;
using System.Text.Json;

namespace LeadHype.Api.Core.Database.Repositories;

public class CampaignEventRepository : ICampaignEventRepository
{
    private readonly IDbConnectionService _connectionService;

    public CampaignEventRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task AddEventAsync(string campaignId, string eventType, int count = 1, string? emailAccountId = null, Dictionary<string, object>? metadata = null)
    {
        const string sql = "SELECT add_campaign_event(@CampaignId, @EventType, @Count, @EmailAccountId, @Metadata::jsonb)";
        
        var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : "{}";
        
        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new 
        { 
            CampaignId = campaignId, 
            EventType = eventType, 
            Count = count, 
            EmailAccountId = emailAccountId,
            Metadata = metadataJson
        });
    }

    public async Task AddBulkEventsAsync(IEnumerable<CampaignEvent> events)
    {
        const string sql = @"
            INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, email_account_id, metadata)
            VALUES (@CampaignId, @EventType, @EventDate, @EventCount, @EmailAccountId, @Metadata::jsonb)
            ON CONFLICT (campaign_id, event_type, event_date)
            DO UPDATE SET
                event_count = EXCLUDED.event_count,
                metadata = EXCLUDED.metadata";

        using var connection = await _connectionService.GetConnectionAsync();
        var parameters = events.Select(e => new
        {
            CampaignId = e.CampaignId,
            EventType = e.EventType,
            EventDate = e.EventDate,
            EventCount = e.EventCount,
            EmailAccountId = e.EmailAccountId,
            Metadata = e.Metadata != null ? JsonSerializer.Serialize(e.Metadata) : "{}"
        });

        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<CampaignStatsNew?> GetStatsAsync(string campaignId, DateTime startDate, DateTime endDate)
    {
        // Query campaign_events table instead of empty campaign_daily_stat_entries
        const string sql = @"
            SELECT
                campaign_id,
                @StartDate as period_start,
                SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END) as sent,
                SUM(CASE WHEN event_type = 'opened' THEN event_count ELSE 0 END) as opened,
                SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END) as replied,
                SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END) as positive_replies,
                SUM(CASE WHEN event_type = 'bounced' THEN event_count ELSE 0 END) as bounced,
                SUM(CASE WHEN event_type = 'clicked' THEN event_count ELSE 0 END) as clicked,
                MAX(event_date) as last_activity
            FROM campaign_events
            WHERE campaign_id = @CampaignId
              AND event_date >= @StartDate
              AND event_date <= @EndDate
            GROUP BY campaign_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            CampaignId = campaignId,
            StartDate = startDate.Date,
            EndDate = endDate.Date
        });

        if (result == null)
        {
            // Return zero stats for campaigns with no data
            return new CampaignStatsNew
            {
                CampaignId = campaignId,
                PeriodStart = startDate.Date,
                Sent = 0,
                Opened = 0,
                Replied = 0,
                PositiveReplies = 0,
                Bounced = 0,
                Clicked = 0,
                LastActivity = null
            };
        }

        return MapToCampaignStats(result);
    }

    public async Task<IEnumerable<CampaignStatsNew>> GetStatsForCampaignsAsync(IEnumerable<string> campaignIds, DateTime startDate, DateTime endDate, string granularity = "day")
    {
        if (!campaignIds.Any()) return Enumerable.Empty<CampaignStatsNew>();

        // Query campaign_events table instead of empty campaign_daily_stat_entries
        const string sql = @"
            SELECT
                campaign_id,
                @StartDate as period_start,
                SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END) as sent,
                SUM(CASE WHEN event_type = 'opened' THEN event_count ELSE 0 END) as opened,
                SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END) as replied,
                SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END) as positive_replies,
                SUM(CASE WHEN event_type = 'bounced' THEN event_count ELSE 0 END) as bounced,
                SUM(CASE WHEN event_type = 'clicked' THEN event_count ELSE 0 END) as clicked,
                MAX(event_date) as last_activity
            FROM campaign_events
            WHERE campaign_id = ANY(@CampaignIds)
              AND event_date >= @StartDate
              AND event_date <= @EndDate
            GROUP BY campaign_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new
        {
            CampaignIds = campaignIds.ToArray(),
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Granularity = granularity
        });

        return results.Select(MapToCampaignStats);
    }

    public async Task<IEnumerable<DailyStatsNew>> GetAggregatedDailyStatsAsync(DateTime startDate, DateTime endDate, List<string>? campaignIds = null)
    {
        string sql;
        object parameters;

        if (campaignIds != null && campaignIds.Any())
        {
            // Query campaign_events table instead of empty campaign_daily_stat_entries
            sql = @"
                SELECT
                    DATE(event_date) as date,
                    SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END) as total_sent,
                    SUM(CASE WHEN event_type = 'opened' THEN event_count ELSE 0 END) as total_opened,
                    SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END) as total_replied,
                    SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END) as total_positive_replies,
                    SUM(CASE WHEN event_type = 'bounced' THEN event_count ELSE 0 END) as total_bounced,
                    SUM(CASE WHEN event_type = 'clicked' THEN event_count ELSE 0 END) as total_clicked,
                    COUNT(DISTINCT campaign_id) as record_count
                FROM campaign_events
                WHERE event_date >= @StartDate
                  AND event_date <= @EndDate
                  AND campaign_id = ANY(@CampaignIds)
                GROUP BY DATE(event_date)
                ORDER BY DATE(event_date) ASC";

            parameters = new { StartDate = startDate.Date, EndDate = endDate.Date, CampaignIds = campaignIds.ToArray() };
        }
        else
        {
            // Query campaign_events table instead of empty campaign_daily_stat_entries
            sql = @"
                SELECT
                    DATE(event_date) as date,
                    SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END) as total_sent,
                    SUM(CASE WHEN event_type = 'opened' THEN event_count ELSE 0 END) as total_opened,
                    SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END) as total_replied,
                    SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END) as total_positive_replies,
                    SUM(CASE WHEN event_type = 'bounced' THEN event_count ELSE 0 END) as total_bounced,
                    SUM(CASE WHEN event_type = 'clicked' THEN event_count ELSE 0 END) as total_clicked,
                    COUNT(DISTINCT campaign_id) as record_count
                FROM campaign_events
                WHERE event_date >= @StartDate
                  AND event_date <= @EndDate
                GROUP BY DATE(event_date)
                ORDER BY DATE(event_date) ASC";

            parameters = new { StartDate = startDate.Date, EndDate = endDate.Date };
        }

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, parameters, commandTimeout: 30);

        return results.Select(MapToDailyStats);
    }

    public async Task<(int TotalSent, int TotalReplied, int TotalPositive, DateTime? LastReplyDate, DateTime? LastPositiveDate)> GetAggregatedTotalsForCampaignsAsync(List<string> campaignIds)
    {
        if (!campaignIds.Any())
        {
            return (0, 0, 0, null, null);
        }

        // Query campaign_events table instead of empty campaign_daily_stat_entries
        const string sql = @"
            SELECT
                COALESCE(SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END), 0) as total_sent,
                COALESCE(SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END), 0) as total_replied,
                COALESCE(SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END), 0) as total_positive,
                MAX(CASE WHEN event_type = 'replied' AND event_count > 0 THEN event_date ELSE NULL END) as last_reply_date,
                MAX(CASE WHEN event_type = 'positive_reply' AND event_count > 0 THEN event_date ELSE NULL END) as last_positive_date
            FROM campaign_events
            WHERE campaign_id = ANY(@CampaignIds)";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { CampaignIds = campaignIds.ToArray() });

        return (
            (int)(long)(result?.total_sent ?? 0),
            (int)(long)(result?.total_replied ?? 0),
            (int)(long)(result?.total_positive ?? 0),
            result?.last_reply_date as DateTime?,
            result?.last_positive_date as DateTime?
        );
    }

    public async Task RefreshMaterializedViewsAsync()
    {
        const string sql = "SELECT refresh_campaign_stats()";
        
        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, commandTimeout: 300);
    }

    public async Task CreatePartitionForDateAsync(DateTime date)
    {
        const string sql = "SELECT create_campaign_events_partition(@StartDate)";
        
        var startOfMonth = new DateTime(date.Year, date.Month, 1);
        
        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new { StartDate = startOfMonth });
    }

    public async Task CleanupOldPartitionsAsync(int monthsToKeep = 12)
    {
        const string sql = "SELECT drop_old_campaign_events_partitions(@MonthsToKeep)";
        
        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new { MonthsToKeep = monthsToKeep });
    }

    // Helper methods for mapping database results to models
    private static CampaignStatsNew MapToCampaignStats(dynamic row)
    {
        return new CampaignStatsNew
        {
            CampaignId = row.campaign_id ?? "",
            PeriodStart = row.period_start is DateTime dt ? dt : DateTime.MinValue,
            Sent = (int)(row.sent ?? 0),
            Opened = (int)(row.opened ?? 0),
            Replied = (int)(row.replied ?? 0),
            PositiveReplies = (int)(row.positive_replies ?? 0),
            Bounced = (int)(row.bounced ?? 0),
            Clicked = (int)(row.clicked ?? 0),
            LastActivity = row.last_activity as DateTime?
        };
    }

    private static DailyStatsNew MapToDailyStats(dynamic row)
    {
        return new DailyStatsNew
        {
            Date = row.date,
            TotalSent = (int)(row.total_sent ?? 0),
            TotalOpened = (int)(row.total_opened ?? 0),
            TotalReplied = (int)(row.total_replied ?? 0),
            TotalPositiveReplies = (int)(row.total_positive_replies ?? 0),
            TotalBounced = (int)(row.total_bounced ?? 0),
            TotalClicked = (int)(row.total_clicked ?? 0),
            RecordCount = (int)(row.record_count ?? 0)
        };
    }
}