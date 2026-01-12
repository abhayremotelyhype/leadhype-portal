using Dapper;
using LeadHype.Api.Models;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class LeadEmailHistoryRepository : ILeadEmailHistoryRepository
{
    private readonly IDbConnectionService _connectionService;

    public LeadEmailHistoryRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<LeadEmailHistoryDbModel> CreateAsync(LeadEmailHistoryDbModel leadEmailHistory)
    {
        const string sql = @"
            INSERT INTO lead_email_history 
            (id, admin_uuid, campaign_id, lead_id, lead_email, subject, body, sequence_number, type, time, created_at, updated_at, 
             classification_result, classified_at, is_classified)
            VALUES 
            (@Id, @AdminUuid, @CampaignId, @LeadId, @LeadEmail, @Subject, @Body, @SequenceNumber, @Type, @Time, @CreatedAt, @UpdatedAt,
             @ClassificationResult, @ClassifiedAt, @IsClassified)
            RETURNING *";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QuerySingleAsync<LeadEmailHistoryDbModel>(sql, leadEmailHistory);
        return result;
    }

    public async Task<bool> UpdateAsync(LeadEmailHistoryDbModel leadEmailHistory)
    {
        const string sql = @"
            UPDATE lead_email_history 
            SET admin_uuid = @AdminUuid, campaign_id = @CampaignId, lead_id = @LeadId, lead_email = @LeadEmail, 
                subject = @Subject, body = @Body, sequence_number = @SequenceNumber, type = @Type, time = @Time, updated_at = @UpdatedAt,
                classification_result = @ClassificationResult, classified_at = @ClassifiedAt, is_classified = @IsClassified
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, leadEmailHistory);
        return rowsAffected > 0;
    }

    public async Task<List<LeadEmailHistoryDbModel>> GetByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT * FROM lead_email_history 
            WHERE campaign_id = @CampaignId 
            ORDER BY sequence_number, COALESCE(time, created_at)";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<LeadEmailHistoryDbModel>(sql, new { CampaignId = campaignId });
        return results.ToList();
    }

    public async Task<List<LeadEmailHistoryDbModel>> GetByLeadIdAsync(string leadId)
    {
        const string sql = @"
            SELECT * FROM lead_email_history 
            WHERE lead_id = @LeadId 
            ORDER BY sequence_number, COALESCE(time, created_at)";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<LeadEmailHistoryDbModel>(sql, new { LeadId = leadId });
        return results.ToList();
    }

    public async Task<List<LeadEmailHistoryDbModel>> GetByCampaignAndLeadIdAsync(int campaignId, string leadId)
    {
        const string sql = @"
            SELECT * FROM lead_email_history 
            WHERE campaign_id = @CampaignId AND lead_id = @LeadId 
            ORDER BY sequence_number, COALESCE(time, created_at)";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<LeadEmailHistoryDbModel>(sql, new { CampaignId = campaignId, LeadId = leadId });
        return results.ToList();
    }

    public async Task<Dictionary<string, int>> GetMessageCountsByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT lead_id, COUNT(*) as message_count 
            FROM lead_email_history 
            WHERE campaign_id = @CampaignId 
            GROUP BY lead_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql, new { CampaignId = campaignId });
        
        return results.ToDictionary(
            row => (string)row.lead_id, 
            row => (int)row.message_count
        );
    }

    public async Task<int> GetTotalMessageCountByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM lead_email_history 
            WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QuerySingleAsync<int>(sql, new { CampaignId = campaignId });
        return result;
    }

    public async Task<bool> DeleteByCampaignIdAsync(int campaignId)
    {
        const string sql = "DELETE FROM lead_email_history WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { CampaignId = campaignId });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByLeadIdAsync(string leadId)
    {
        const string sql = "DELETE FROM lead_email_history WHERE lead_id = @LeadId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { LeadId = leadId });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByCampaignAndLeadIdAsync(int campaignId, string leadId)
    {
        const string sql = "DELETE FROM lead_email_history WHERE campaign_id = @CampaignId AND lead_id = @LeadId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { CampaignId = campaignId, LeadId = leadId });
        return rowsAffected > 0;
    }

    public async Task<DateTime?> GetLastContactedDateForCampaignsAsync(List<int> campaignIds)
    {
        if (campaignIds == null || !campaignIds.Any())
            return null;

        // Efficient query to get the most recent email activity (sent or received) across all campaigns
        const string sql = @"
            SELECT MAX(time) as last_contacted
            FROM lead_email_history
            WHERE campaign_id = ANY(@CampaignIds)
            AND time IS NOT NULL";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { CampaignIds = campaignIds.ToArray() });
        return result;
    }
}