using LeadHype.Api.Core.Database.Models;
using Dapper;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class CampaignDailyStatEntryRepository : ICampaignDailyStatEntryRepository
{
    private readonly IDbConnectionService _connectionService;

    public CampaignDailyStatEntryRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdAsync(string campaignId)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id, 
                campaign_id_int,
                stat_date, 
                sent, opened, clicked, replied, positive_replies, bounced, 
                created_at, 
                updated_at
            FROM campaign_daily_stat_entries 
            WHERE campaign_id = @CampaignId 
            ORDER BY stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new { CampaignId = campaignId });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdAndDateRangeAsync(
        string campaignId, 
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id, 
                campaign_id_int,
                stat_date, 
                sent, opened, clicked, replied, positive_replies, bounced, 
                created_at, 
                updated_at
            FROM campaign_daily_stat_entries 
            WHERE campaign_id = @CampaignId 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            ORDER BY stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            CampaignId = campaignId, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdsAndDateRangeAsync(
        List<string> campaignIds, 
        DateTime startDate, 
        DateTime endDate)
    {
        if (!campaignIds.Any()) return Enumerable.Empty<CampaignDailyStatEntry>();

        // Optimized query using materialized view for much better performance
        const string sql = @"
            SELECT
                campaign_id || '_' || stat_date::text as id,
                '' as admin_uuid,
                campaign_id,
                0 as campaign_id_int,
                stat_date,
                sent, opened, clicked, replied, positive_replies, bounced,
                NOW() as created_at,
                NOW() as updated_at
            FROM campaign_daily_stat_entries
            WHERE campaign_id = ANY(@CampaignIds)
              AND stat_date >= @StartDate
              AND stat_date <= @EndDate
            ORDER BY campaign_id, stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        
        // Set command timeout for large queries
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            CampaignIds = campaignIds.ToArray(), 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        }, commandTimeout: 30); // 30 second timeout
        
        return results.Select(MapToStatEntry);
    }

    public async Task<CampaignDailyStatEntry?> GetAggregatedStatsByCampaignAsync(
        string campaignId, 
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT 
                @CampaignId as campaign_id,
                SUM(sent) as sent,
                SUM(opened) as opened,
                SUM(clicked) as clicked,
                SUM(replied) as replied,
                SUM(positive_replies) as positive_replies,
                SUM(bounced) as bounced
            FROM campaign_daily_stat_entries
            WHERE campaign_id = @CampaignId
              AND stat_date >= @StartDate
              AND stat_date <= @EndDate";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new 
        { 
            CampaignId = campaignId, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });

        if (result == null) return null;

        return new CampaignDailyStatEntry
        {
            Id = Guid.NewGuid().ToString(),
            CampaignId = campaignId,
            StatDate = startDate, // Representative date
            Sent = (int)(result.sent ?? 0),
            Opened = (int)(result.opened ?? 0),
            Clicked = (int)(result.clicked ?? 0),
            Replied = (int)(result.replied ?? 0),
            PositiveReplies = (int)(result.positive_replies ?? 0),
            Bounced = (int)(result.bounced ?? 0)
        };
    }

    public async Task<CampaignDailyStatEntry> GetOrCreateStatEntryAsync(
        string adminUuid,
        string campaignId, 
        int campaignIdInt,
        DateTime statDate)
    {
        const string selectSql = @"
            SELECT id, admin_uuid, campaign_id, campaign_id_int, stat_date, 
                   sent, opened, clicked, replied, positive_replies, bounced, 
                   created_at, updated_at
            FROM campaign_daily_stat_entries 
            WHERE campaign_id = @CampaignId AND stat_date = @StatDate";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(selectSql, new 
        { 
            CampaignId = campaignId, 
            StatDate = statDate.Date 
        });

        if (result != null)
        {
            return MapToStatEntry(result);
        }

        // Create new entry
        var newEntry = new CampaignDailyStatEntry
        {
            AdminUuid = adminUuid,
            CampaignId = campaignId,
            CampaignIdInt = campaignIdInt,
            StatDate = statDate.Date
        };

        const string insertSql = @"
            INSERT INTO campaign_daily_stat_entries 
            (id, admin_uuid, campaign_id, campaign_id_int, stat_date, sent, opened, 
             clicked, replied, positive_replies, bounced, created_at, updated_at)
            VALUES 
            (@Id, @AdminUuid, @CampaignId, @CampaignIdInt, @StatDate, @Sent, @Opened, 
             @Clicked, @Replied, @PositiveReplies, @Bounced, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(insertSql, newEntry);
        return newEntry;
    }

    public async Task UpdateStatsAsync(
        string campaignId, 
        DateTime statDate, 
        int sent, 
        int opened, 
        int clicked, 
        int replied, 
        int positiveReplies, 
        int bounced)
    {
        const string sql = @"
            UPDATE campaign_daily_stat_entries 
            SET sent = @Sent, opened = @Opened, clicked = @Clicked, 
                replied = @Replied, positive_replies = @PositiveReplies, 
                bounced = @Bounced, updated_at = @UpdatedAt
            WHERE campaign_id = @CampaignId AND stat_date = @StatDate";

        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new 
        { 
            CampaignId = campaignId, 
            StatDate = statDate.Date,
            Sent = sent,
            Opened = opened,
            Clicked = clicked,
            Replied = replied,
            PositiveReplies = positiveReplies,
            Bounced = bounced,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task UpsertStatsAsync(
        string adminUuid,
        string campaignId, 
        int campaignIdInt,
        DateTime statDate, 
        int sent, 
        int opened, 
        int clicked, 
        int replied, 
        int positiveReplies, 
        int bounced)
    {
        const string sql = @"
            INSERT INTO campaign_daily_stat_entries 
            (id, admin_uuid, campaign_id, campaign_id_int, stat_date, sent, opened, 
             clicked, replied, positive_replies, bounced, created_at, updated_at)
            VALUES 
            (@Id, @AdminUuid, @CampaignId, @CampaignIdInt, @StatDate, @Sent, @Opened, 
             @Clicked, @Replied, @PositiveReplies, @Bounced, @CreatedAt, @UpdatedAt)
            ON CONFLICT (campaign_id, stat_date) 
            DO UPDATE SET
                sent = @Sent,
                opened = @Opened,
                clicked = @Clicked,
                replied = @Replied,
                positive_replies = @PositiveReplies,
                bounced = @Bounced,
                updated_at = @UpdatedAt";

        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new 
        { 
            Id = Guid.NewGuid().ToString(),
            AdminUuid = adminUuid,
            CampaignId = campaignId, 
            CampaignIdInt = campaignIdInt,
            StatDate = statDate.Date,
            Sent = sent,
            Opened = opened,
            Clicked = clicked,
            Replied = replied,
            PositiveReplies = positiveReplies,
            Bounced = bounced,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<IEnumerable<CampaignDailyStatEntry>> GetByAdminAndDateRangeAsync(
        string adminUuid, 
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT id, admin_uuid, campaign_id, campaign_id_int, stat_date, 
                   sent, opened, clicked, replied, positive_replies, bounced, 
                   created_at, updated_at
            FROM campaign_daily_stat_entries 
            WHERE admin_uuid = @AdminUuid 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            ORDER BY stat_date DESC, campaign_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            AdminUuid = adminUuid, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });
        
        return results.Select(MapToStatEntry);
    }

    /// <summary>
    /// Get aggregated daily stats for dashboard performance trends
    /// Optimized for large datasets by aggregating in the database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetAggregatedDailyStatsAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT 
                stat_date as date,
                SUM(COALESCE(sent, 0)) as totalsent,
                SUM(COALESCE(opened, 0)) as totalopened,
                SUM(COALESCE(replied, 0)) as totalreplied,
                SUM(COALESCE(positive_replies, 0)) as totalpositivereplies,
                SUM(COALESCE(bounced, 0)) as totalbounced,
                COUNT(*) as recordcount
            FROM campaign_daily_stat_entries 
            WHERE stat_date IS NOT NULL
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            GROUP BY stat_date
            ORDER BY stat_date ASC";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryAsync<dynamic>(sql, new 
        { 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        }, commandTimeout: 30);
    }

    public async Task<IEnumerable<dynamic>> GetAggregatedDailyStatsByCampaignsAsync(
        DateTime startDate, 
        DateTime endDate,
        List<string> campaignIds)
    {
        if (!campaignIds.Any())
        {
            return Enumerable.Empty<dynamic>();
        }

        const string sql = @"
            SELECT 
                stat_date as date,
                SUM(COALESCE(sent, 0)) as totalsent,
                SUM(COALESCE(opened, 0)) as totalopened,
                SUM(COALESCE(replied, 0)) as totalreplied,
                SUM(COALESCE(positive_replies, 0)) as totalpositivereplies,
                SUM(COALESCE(bounced, 0)) as totalbounced,
                COUNT(*) as recordcount
            FROM campaign_daily_stat_entries 
            WHERE stat_date IS NOT NULL
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
              AND campaign_id = ANY(@CampaignIds)
            GROUP BY stat_date
            ORDER BY stat_date ASC";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryAsync<dynamic>(sql, new 
        { 
            StartDate = startDate.Date, 
            EndDate = endDate.Date,
            CampaignIds = campaignIds.ToArray()
        }, commandTimeout: 30);
    }

    public async Task<bool> DeleteByCampaignIdAsync(string campaignId)
    {
        const string sql = "DELETE FROM campaign_daily_stat_entries WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { CampaignId = campaignId });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdIntAsync(int campaignIdInt)
    {
        const string sql = @"
            SELECT id, admin_uuid, campaign_id, campaign_id_int, stat_date, 
                   sent, opened, clicked, replied, positive_replies, bounced, 
                   created_at, updated_at
            FROM campaign_daily_stat_entries 
            WHERE campaign_id_int = @CampaignIdInt 
            ORDER BY stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new { CampaignIdInt = campaignIdInt });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<(int TotalSent, int TotalReplied, int TotalPositive, DateTime? LastReplyDate, DateTime? LastPositiveDate)> GetAggregatedTotalsForCampaignsAsync(List<string> campaignIds)
    {
        if (!campaignIds.Any())
        {
            return (0, 0, 0, null, null);
        }

        const string sql = @"
            SELECT 
                COALESCE(SUM(sent), 0) as total_sent,
                COALESCE(SUM(replied), 0) as total_replied,
                COALESCE(SUM(positive_replies), 0) as total_positive,
                MAX(CASE WHEN replied > 0 THEN stat_date ELSE NULL END) as last_reply_date,
                MAX(CASE WHEN positive_replies > 0 THEN stat_date ELSE NULL END) as last_positive_date
            FROM campaign_daily_stat_entries
            WHERE campaign_id = ANY(@CampaignIds)";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { CampaignIds = campaignIds.ToArray() });
        
        return (
            (int)(result?.total_sent ?? 0),
            (int)(result?.total_replied ?? 0), 
            (int)(result?.total_positive ?? 0),
            result?.last_reply_date as DateTime?,
            result?.last_positive_date as DateTime?
        );
    }

    private static CampaignDailyStatEntry MapToStatEntry(dynamic row)
    {
        return new CampaignDailyStatEntry
        {
            Id = row.id,
            AdminUuid = row.admin_uuid,
            CampaignId = row.campaign_id,
            CampaignIdInt = row.campaign_id_int,
            StatDate = row.stat_date,
            Sent = Convert.ToInt32(row.sent),
            Opened = Convert.ToInt32(row.opened),
            Clicked = Convert.ToInt32(row.clicked),
            Replied = Convert.ToInt32(row.replied),
            PositiveReplies = Convert.ToInt32(row.positive_replies),
            Bounced = Convert.ToInt32(row.bounced),
            CreatedAt = row.created_at,
            UpdatedAt = row.updated_at
        };
    }
}