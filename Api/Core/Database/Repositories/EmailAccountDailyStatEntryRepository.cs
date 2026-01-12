using LeadHype.Api.Core.Database.Models;
using Dapper;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class EmailAccountDailyStatEntryRepository : IEmailAccountDailyStatEntryRepository
{
    private readonly IDbConnectionService _connectionService;

    public EmailAccountDailyStatEntryRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<EmailAccountDailyStatEntry>> GetByEmailAccountIdAsync(long emailAccountId)
    {
        const string sql = @"
            SELECT id, admin_uuid, email_account_id, stat_date, 
                   sent, opened, replied, bounced, 
                   created_at, updated_at
            FROM email_account_daily_stat_entries 
            WHERE email_account_id = @EmailAccountId 
            ORDER BY stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new { EmailAccountId = emailAccountId });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<IEnumerable<EmailAccountDailyStatEntry>> GetByEmailAccountIdAndDateRangeAsync(
        long emailAccountId, 
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT id, admin_uuid, email_account_id, stat_date, 
                   sent, opened, replied, bounced, 
                   created_at, updated_at
            FROM email_account_daily_stat_entries 
            WHERE email_account_id = @EmailAccountId 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            ORDER BY stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            EmailAccountId = emailAccountId, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<IEnumerable<EmailAccountDailyStatEntry>> GetByEmailAccountIdsAndDateRangeAsync(
        List<long> emailAccountIds, 
        DateTime startDate, 
        DateTime endDate)
    {
        if (!emailAccountIds.Any()) return Enumerable.Empty<EmailAccountDailyStatEntry>();

        const string sql = @"
            SELECT id, admin_uuid, email_account_id, stat_date, 
                   sent, opened, replied, bounced, 
                   created_at, updated_at
            FROM email_account_daily_stat_entries 
            WHERE email_account_id = ANY(@EmailAccountIds) 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            ORDER BY email_account_id, stat_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            EmailAccountIds = emailAccountIds.ToArray(), 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<EmailAccountDailyStatEntry?> GetAggregatedStatsByEmailAccountAsync(
        long emailAccountId, 
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT 
                @EmailAccountId as email_account_id,
                SUM(sent) as sent,
                SUM(opened) as opened,
                SUM(replied) as replied,
                SUM(bounced) as bounced
            FROM email_account_daily_stat_entries 
            WHERE email_account_id = @EmailAccountId 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new 
        { 
            EmailAccountId = emailAccountId, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });

        if (result == null) return null;

        return new EmailAccountDailyStatEntry
        {
            Id = Guid.NewGuid().ToString(),
            EmailAccountId = emailAccountId,
            StatDate = startDate, // Representative date
            Sent = (int)(result.sent ?? 0),
            Opened = (int)(result.opened ?? 0),
            Replied = (int)(result.replied ?? 0),
            Bounced = (int)(result.bounced ?? 0)
        };
    }

    public async Task<EmailAccountDailyStatEntry> GetOrCreateStatEntryAsync(
        string adminUuid,
        long emailAccountId,
        DateTime statDate)
    {
        const string selectSql = @"
            SELECT id, admin_uuid, email_account_id, stat_date, 
                   sent, opened, replied, bounced, 
                   created_at, updated_at
            FROM email_account_daily_stat_entries 
            WHERE email_account_id = @EmailAccountId AND stat_date = @StatDate";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(selectSql, new 
        { 
            EmailAccountId = emailAccountId, 
            StatDate = statDate.Date 
        });

        if (result != null)
        {
            return MapToStatEntry(result);
        }

        // Create new entry
        var newEntry = new EmailAccountDailyStatEntry
        {
            AdminUuid = adminUuid,
            EmailAccountId = emailAccountId,
            StatDate = statDate.Date
        };

        const string insertSql = @"
            INSERT INTO email_account_daily_stat_entries 
            (id, admin_uuid, email_account_id, stat_date, sent, opened, 
             replied, bounced, created_at, updated_at)
            VALUES 
            (@Id, @AdminUuid, @EmailAccountId, @StatDate, @Sent, @Opened, 
             @Replied, @Bounced, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(insertSql, newEntry);
        return newEntry;
    }

    public async Task UpdateStatsAsync(
        long emailAccountId, 
        DateTime statDate, 
        int sent, 
        int opened, 
        int replied, 
        int bounced)
    {
        const string sql = @"
            UPDATE email_account_daily_stat_entries 
            SET sent = @Sent, opened = @Opened, 
                replied = @Replied, 
                bounced = @Bounced, updated_at = @UpdatedAt
            WHERE email_account_id = @EmailAccountId AND stat_date = @StatDate";

        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new 
        { 
            EmailAccountId = emailAccountId, 
            StatDate = statDate.Date,
            Sent = sent,
            Opened = opened,
            Replied = replied,
            Bounced = bounced,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task UpsertStatsAsync(
        string adminUuid,
        long emailAccountId,
        DateTime statDate, 
        int sent, 
        int opened, 
        int replied, 
        int bounced)
    {
        const string sql = @"
            INSERT INTO email_account_daily_stat_entries
            (id, admin_uuid, email_account_id, stat_date, sent, opened,
             replied, bounced, created_at, updated_at)
            VALUES
            (@Id, @AdminUuid, @EmailAccountId, @StatDate, @Sent, @Opened,
             @Replied, @Bounced, @CreatedAt, @UpdatedAt)
            ON CONFLICT (email_account_id, stat_date)
            DO UPDATE SET
                sent = @Sent,
                opened = @Opened,
                replied = @Replied,
                bounced = @Bounced,
                updated_at = @UpdatedAt";

        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new 
        { 
            Id = Guid.NewGuid().ToString(),
            AdminUuid = adminUuid,
            EmailAccountId = emailAccountId,
            StatDate = statDate.Date,
            Sent = sent,
            Opened = opened,
            Replied = replied,
            Bounced = bounced,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<IEnumerable<EmailAccountDailyStatEntry>> GetByAdminAndDateRangeAsync(
        string adminUuid, 
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT id, admin_uuid, email_account_id, stat_date, 
                   sent, opened, replied, bounced, 
                   created_at, updated_at
            FROM email_account_daily_stat_entries 
            WHERE admin_uuid = @AdminUuid 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            ORDER BY stat_date DESC, email_account_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            AdminUuid = adminUuid, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });
        
        return results.Select(MapToStatEntry);
    }

    public async Task<bool> DeleteByEmailAccountIdAsync(long emailAccountId)
    {
        const string sql = "DELETE FROM email_account_daily_stat_entries WHERE email_account_id = @EmailAccountId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { EmailAccountId = emailAccountId });
        return rowsAffected > 0;
    }

    public async Task<Dictionary<string, int>> GetAggregatedStatsByDateAsync(
        string adminUuid,
        DateTime startDate, 
        DateTime endDate)
    {
        const string sql = @"
            SELECT 
                stat_date,
                SUM(sent) as total_sent,
                SUM(opened) as total_opened,
                SUM(replied) as total_replied,
                SUM(bounced) as total_bounced
            FROM email_account_daily_stat_entries 
            WHERE admin_uuid = @AdminUuid 
              AND stat_date >= @StartDate 
              AND stat_date <= @EndDate 
            GROUP BY stat_date
            ORDER BY stat_date";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new 
        { 
            AdminUuid = adminUuid, 
            StartDate = startDate.Date, 
            EndDate = endDate.Date 
        });

        var sentStats = new Dictionary<string, int>();
        foreach (var row in results)
        {
            var dateKey = ((DateTime)row.stat_date).ToString("yyyy-MM-dd");
            sentStats[dateKey] = row.total_sent ?? 0;
        }

        return sentStats;
    }

    private static EmailAccountDailyStatEntry MapToStatEntry(dynamic row)
    {
        return new EmailAccountDailyStatEntry
        {
            Id = row.id,
            AdminUuid = row.admin_uuid,
            EmailAccountId = row.email_account_id,
            StatDate = row.stat_date,
            Sent = row.sent,
            Opened = row.opened,
            Replied = row.replied,
            Bounced = row.bounced,
            CreatedAt = row.created_at,
            UpdatedAt = row.updated_at
        };
    }
}