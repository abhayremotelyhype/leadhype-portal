using LeadHype.Api.Core.Models;
using Dapper;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class EmailAccountStatsDateRepository : IEmailAccountStatsDateRepository
{
    private readonly IDbConnectionService _connectionService;

    public EmailAccountStatsDateRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<EmailAccountStatsDate?> GetByDateAsync(string adminUuid, DateTime dateTime)
    {
        const string sql = @"
            SELECT 
                admin_uuid,
                stats_date
            FROM email_account_stats_dates
            WHERE admin_uuid = @AdminUuid 
                AND stats_date = @StatsDate";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, 
            new { AdminUuid = adminUuid, StatsDate = dateTime.Date });
        
        if (result == null) return null;
        
        return new EmailAccountStatsDate
        {
            AdminUuid = result.admin_uuid,
            LatestDateTime = result.stats_date
        };
    }

    public async Task<IEnumerable<EmailAccountStatsDate>> GetByAdminUuidAsync(string adminUuid)
    {
        const string sql = @"
            SELECT 
                admin_uuid,
                stats_date
            FROM email_account_stats_dates
            WHERE admin_uuid = @AdminUuid
            ORDER BY stats_date DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid });
        
        return results.Select(r => new EmailAccountStatsDate
        {
            AdminUuid = r.admin_uuid,
            LatestDateTime = r.stats_date
        });
    }

    public async Task CreateAsync(EmailAccountStatsDate statsDate)
    {
        const string sql = @"
            INSERT INTO email_account_stats_dates (
                id,
                admin_uuid,
                stats_date
            )
            VALUES (
                @Id,
                @AdminUuid,
                @StatsDate
            )
            ON CONFLICT (admin_uuid, stats_date) DO NOTHING";

        using var connection = await _connectionService.GetConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid().ToString(),
            AdminUuid = statsDate.AdminUuid,
            StatsDate = statsDate.LatestDateTime.Date
        });
    }

    public async Task<bool> ExistsAsync(string adminUuid, DateTime dateTime)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 
                FROM email_account_stats_dates
                WHERE admin_uuid = @AdminUuid 
                    AND stats_date = @StatsDate
            )";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<bool>(sql, 
            new { AdminUuid = adminUuid, StatsDate = dateTime.Date });
    }
}