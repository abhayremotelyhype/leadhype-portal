# Campaign Statistics Migration Plan

## Overview
Migrate from inefficient `campaign_daily_stat_entries` table (2.6M rows, 93% waste, 2GB storage) to efficient event-sourced model (expect ~95% storage reduction).

## Current Problems
- **2.4M rows with zero activity** (93.37% waste)
- **2GB total size** (524MB data + 1.5GB indexes)
- **9 indexes** on mostly empty data
- **Query performance degradation** due to scanning irrelevant data
- **No data lifecycle management**

## New Architecture Benefits
- ✅ **Event-sourced design** - only store actual events
- ✅ **95% storage reduction** - from 2GB to ~100MB
- ✅ **Faster queries** - materialized views with selective indexing
- ✅ **Flexible aggregation** - any time period, any granularity
- ✅ **Automatic data lifecycle** - partitioned tables, easy cleanup
- ✅ **Better scalability** - horizontal partitioning ready

## Migration Strategy

### Phase 1: Preparation (1-2 days)
1. **Create new schema** alongside existing tables
2. **Implement new repository classes** without touching existing code
3. **Set up background jobs** for materialized view refresh
4. **Test data migration script** on subset of data
5. **Performance testing** with new structure

### Phase 2: Dual-Write Implementation (3-5 days)
1. **Modify data ingestion** to write to both old and new tables
2. **Implement feature flags** to switch between old/new for reads
3. **Monitor data consistency** between old and new systems
4. **Update application services** to use new repositories
5. **Comprehensive testing** of dashboard and analytics

### Phase 3: Migration Execution (1-2 days)
1. **Schedule maintenance window**
2. **Run full data migration** (only meaningful data ~172MB)
3. **Switch application** to use new tables exclusively
4. **Verify dashboard functionality**
5. **Monitor performance improvements**

### Phase 4: Cleanup (1 day)
1. **Drop old table** and unused indexes
2. **Update backup strategies**
3. **Document new data model**
4. **Set up monitoring** for materialized view freshness

## Detailed Migration Steps

### Step 1: Schema Creation
```sql
-- Run the new_efficient_campaign_stats_design.sql
-- This creates all new tables, views, and functions
```

### Step 2: Data Migration Script
```sql
-- Migrate only meaningful data (where any stat > 0)
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count)
SELECT 
    campaign_id, 
    unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) as event_type,
    stat_date as event_date,
    CASE 
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'sent' THEN sent
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'opened' THEN opened
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'replied' THEN replied
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'positive_reply' THEN positive_replies
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'bounced' THEN bounced
    END as event_count
FROM campaign_daily_stat_entries 
WHERE (sent > 0 OR opened > 0 OR replied > 0 OR positive_replies > 0 OR bounced > 0)
  AND CASE 
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'sent' THEN sent
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'opened' THEN opened
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'replied' THEN replied
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'positive_reply' THEN positive_replies
        WHEN unnest(ARRAY['sent', 'opened', 'replied', 'positive_reply', 'bounced']) = 'bounced' THEN bounced
    END > 0;

-- Build initial materialized views
SELECT refresh_campaign_stats();
```

### Step 3: Application Code Updates

#### New Repository Interface
```csharp
public interface ICampaignEventRepository
{
    Task AddEventAsync(string campaignId, string eventType, int count = 1, string? emailAccountId = null);
    Task AddBulkEventsAsync(IEnumerable<CampaignEvent> events);
    Task<CampaignStats> GetStatsAsync(string campaignId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<CampaignStats>> GetStatsForCampaignsAsync(IEnumerable<string> campaignIds, DateTime startDate, DateTime endDate);
    Task<IEnumerable<DailyStats>> GetAggregatedDailyStatsAsync(DateTime startDate, DateTime endDate, string granularity = "day");
}
```

#### New Repository Implementation
```csharp
public class CampaignEventRepository : ICampaignEventRepository
{
    private readonly IDbConnectionService _connectionService;

    public async Task AddEventAsync(string campaignId, string eventType, int count = 1, string? emailAccountId = null)
    {
        const string sql = "SELECT add_campaign_event(@CampaignId, @EventType, @Count, @EmailAccountId, '{}')";
        
        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new 
        { 
            CampaignId = campaignId, 
            EventType = eventType, 
            Count = count, 
            EmailAccountId = emailAccountId 
        });
    }

    public async Task<CampaignStats> GetStatsAsync(string campaignId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT 
                campaign_id,
                SUM(sent) as sent,
                SUM(opened) as opened,
                SUM(replied) as replied,
                SUM(positive_replies) as positive_replies,
                SUM(bounced) as bounced,
                SUM(clicked) as clicked
            FROM campaign_daily_stats
            WHERE campaign_id = @CampaignId
              AND event_date >= @StartDate
              AND event_date <= @EndDate
            GROUP BY campaign_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new 
        { 
            CampaignId = campaignId, 
            StartDate = startDate, 
            EndDate = endDate 
        });

        return MapToCampaignStats(result, campaignId);
    }

    public async Task<IEnumerable<DailyStats>> GetAggregatedDailyStatsAsync(DateTime startDate, DateTime endDate, string granularity = "day")
    {
        // Use the new helper function
        const string sql = "SELECT * FROM get_campaign_stats(NULL, @StartDate, @EndDate, @Granularity)";
        
        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            StartDate = startDate, 
            EndDate = endDate, 
            Granularity = granularity 
        });

        return results.Select(MapToDailyStats);
    }
}
```

#### Service Layer Updates
```csharp
public class CampaignStatsService
{
    private readonly ICampaignEventRepository _eventRepository;
    private readonly ICampaignDailyStatEntryRepository _legacyRepository; // For fallback
    private readonly IFeatureFlags _featureFlags;

    public async Task<CampaignStats> GetCampaignStatsAsync(string campaignId, DateTime startDate, DateTime endDate)
    {
        if (_featureFlags.IsEnabled("UseNewCampaignStats"))
        {
            return await _eventRepository.GetStatsAsync(campaignId, startDate, endDate);
        }
        else
        {
            // Fallback to old implementation
            return await _legacyRepository.GetAggregatedStatsByCampaignAsync(campaignId, startDate, endDate);
        }
    }

    public async Task RecordCampaignEventAsync(string campaignId, string eventType, int count = 1)
    {
        // Always write to new system during migration period
        await _eventRepository.AddEventAsync(campaignId, eventType, count);
        
        // Temporarily also update old system for consistency checking
        if (_featureFlags.IsEnabled("DualWriteCampaignStats"))
        {
            await UpdateLegacyStats(campaignId, eventType, count);
        }
    }
}
```

### Step 4: Background Jobs Setup
```csharp
public class CampaignStatsMaintenance : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Refresh materialized views every hour
                await RefreshMaterializedViews();
                
                // Clean up old partitions monthly
                if (DateTime.UtcNow.Day == 1 && DateTime.UtcNow.Hour == 2)
                {
                    await CleanupOldPartitions();
                }
                
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in campaign stats maintenance");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task RefreshMaterializedViews()
    {
        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync("SELECT refresh_campaign_stats()", commandTimeout: 300);
    }
}
```

## Risk Mitigation

### Data Consistency Checks
```sql
-- Compare old vs new aggregations during migration
WITH old_stats AS (
    SELECT campaign_id, stat_date, sent, opened, replied, positive_replies, bounced
    FROM campaign_daily_stat_entries
    WHERE sent > 0 OR opened > 0 OR replied > 0 OR positive_replies > 0 OR bounced > 0
),
new_stats AS (
    SELECT campaign_id, event_date as stat_date, sent, opened, replied, positive_replies, bounced
    FROM campaign_daily_stats
)
SELECT 
    COALESCE(o.campaign_id, n.campaign_id) as campaign_id,
    COALESCE(o.stat_date, n.stat_date) as stat_date,
    o.sent as old_sent, n.sent as new_sent,
    o.opened as old_opened, n.opened as new_opened,
    CASE WHEN o.sent != n.sent OR o.opened != n.opened THEN 'MISMATCH' ELSE 'OK' END as status
FROM old_stats o
FULL OUTER JOIN new_stats n ON o.campaign_id = n.campaign_id AND o.stat_date = n.stat_date
WHERE o.sent != n.sent OR o.opened != n.opened OR o.campaign_id IS NULL OR n.campaign_id IS NULL
LIMIT 100;
```

### Rollback Plan
1. **Keep old table** until migration is verified (1 week)
2. **Feature flags** allow instant rollback to old system
3. **Database backups** before migration execution
4. **Dual-write period** ensures data consistency

## Performance Expectations

### Storage Improvements
- **Before**: 2.6M rows, 2GB total size
- **After**: ~172K meaningful events, ~100MB total size
- **Reduction**: 95% storage savings

### Query Performance Improvements
- **Daily aggregation queries**: 10x-50x faster (materialized views vs scanning 2.6M rows)
- **Date range queries**: 20x-100x faster (only meaningful data, better indexes)
- **Dashboard load time**: Expect 60-80% reduction

### Maintenance Benefits
- **Backup time**: 95% reduction
- **Index maintenance**: Minimal (only 3-4 essential indexes vs 9 over-indexes)
- **Data lifecycle**: Automatic via partitioning

## Timeline

| Phase | Duration | Tasks |
|-------|----------|-------|
| Preparation | 2 days | Schema creation, new repositories, testing |
| Dual-Write | 4 days | Implementation, feature flags, monitoring |
| Migration | 1 day | Data migration, cutover, verification |
| Cleanup | 1 day | Remove old table, documentation |
| **Total** | **8 days** | **Complete migration** |

## Success Metrics

1. **Storage reduction**: Target 90%+ reduction in campaign stats storage
2. **Query performance**: 10x improvement in dashboard load times
3. **Data accuracy**: 100% consistency between old and new aggregations
4. **Zero downtime**: No service interruption during migration
5. **Maintainability**: Simplified maintenance with automatic data lifecycle

This new architecture will scale much better and eliminate the current inefficiencies while providing more flexibility for future analytics needs.