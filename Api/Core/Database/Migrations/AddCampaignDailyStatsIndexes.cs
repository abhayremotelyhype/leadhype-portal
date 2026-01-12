using Dapper;
using Npgsql;

namespace LeadHype.Api.Core.Database.Migrations;

public class AddCampaignDailyStatsIndexes
{
    private readonly string _connectionString;
    
    public AddCampaignDailyStatsIndexes(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task ApplyIndexesAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var indexes = new[]
        {
            // Composite index for the most common query pattern
            @"CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_campaign_date 
              ON campaign_daily_stat_entries(campaign_id, stat_date DESC)",
            
            // Index on stat_date for time-range queries
            @"CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_date 
              ON campaign_daily_stat_entries(stat_date DESC)",
            
            // Index on admin_uuid for filtering
            @"CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_admin 
              ON campaign_daily_stat_entries(admin_uuid)",
            
            // Composite index for admin with date
            @"CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_admin_date 
              ON campaign_daily_stat_entries(admin_uuid, stat_date DESC)",
            
            // Covering index to avoid table lookups for common queries
            @"CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_covering 
              ON campaign_daily_stat_entries(campaign_id, stat_date DESC) 
              INCLUDE (sent, opened, clicked, replied, positive_replies, bounced)"
        };
        
        foreach (var indexSql in indexes)
        {
            try
            {
                await connection.ExecuteAsync(indexSql);
                Console.WriteLine($"✓ Index created or already exists");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error creating index: {ex.Message}");
            }
        }
        
        // Update table statistics
        await connection.ExecuteAsync("ANALYZE campaign_daily_stat_entries");
        
        // Get table size and row count
        var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT 
                pg_size_pretty(pg_total_relation_size('campaign_daily_stat_entries')) AS table_size,
                COUNT(*) as row_count
            FROM campaign_daily_stat_entries");
        
        Console.WriteLine($"\nTable Statistics:");
        Console.WriteLine($"- Table Size: {stats?.table_size ?? "Unknown"}");
        Console.WriteLine($"- Row Count: {stats?.row_count ?? 0:N0}");
        
        // Check query performance
        var explainResult = await connection.QueryAsync<string>(@"
            EXPLAIN (FORMAT JSON, ANALYZE, BUFFERS) 
            SELECT * FROM campaign_daily_stat_entries 
            WHERE campaign_id = '00000000-0000-0000-0000-000000000000' 
            AND stat_date BETWEEN CURRENT_DATE - INTERVAL '30 days' AND CURRENT_DATE");
        
        Console.WriteLine("\nQuery plan available for performance analysis");
    }
}