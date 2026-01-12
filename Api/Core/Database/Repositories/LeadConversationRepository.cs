using LeadHype.Api.Models;
using Dapper;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class LeadConversationRepository : ILeadConversationRepository
{
    private readonly IDbConnectionService _connectionService;

    public LeadConversationRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<LeadConversationDbModel>> GetByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT
                id,
                admin_uuid,
                campaign_id,
                lead_email,
                lead_first_name,
                lead_last_name,
                status,
                conversation_data,
                created_at,
                updated_at
            FROM lead_conversations
            WHERE campaign_id = @CampaignId
            ORDER BY updated_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { CampaignId = campaignId });

        return results.Select(MapToConversation);
    }

    public async Task<IEnumerable<LeadConversationDbModel>> GetByCampaignIdAsync(int campaignId, bool withRepliesOnly)
    {
        if (!withRepliesOnly)
        {
            return await GetByCampaignIdAsync(campaignId);
        }

        const string sql = @"
            SELECT DISTINCT
                lc.id,
                lc.admin_uuid,
                lc.campaign_id,
                lc.lead_email,
                lc.lead_first_name,
                lc.lead_last_name,
                lc.status,
                lc.conversation_data,
                lc.created_at,
                lc.updated_at
            FROM lead_conversations lc
            INNER JOIN lead_email_history leh ON lc.campaign_id = leh.campaign_id
                AND lc.lead_email = leh.lead_email
            WHERE lc.campaign_id = @CampaignId
                AND leh.type = 'REPLY'
            ORDER BY lc.updated_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { CampaignId = campaignId });

        return results.Select(MapToConversation);
    }

    public async Task<IEnumerable<LeadConversationDbModel>> GetByAdminUuidAsync(string adminUuid)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                lead_email,
                lead_first_name,
                lead_last_name,
                status,
                conversation_data,
                created_at,
                updated_at
            FROM lead_conversations
            WHERE admin_uuid = @AdminUuid
            ORDER BY campaign_id, updated_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid });
        
        return results.Select(MapToConversation);
    }

    public async Task<LeadConversationDbModel?> GetByLeadEmailAsync(int campaignId, string leadEmail)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                lead_email,
                lead_first_name,
                lead_last_name,
                status,
                conversation_data,
                created_at,
                updated_at
            FROM lead_conversations
            WHERE campaign_id = @CampaignId AND lead_email = @LeadEmail";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { CampaignId = campaignId, LeadEmail = leadEmail });
        
        return result != null ? MapToConversation(result) : null;
    }

    public async Task<string> CreateAsync(LeadConversationDbModel conversation)
    {
        const string sql = @"
            INSERT INTO lead_conversations (
                id,
                admin_uuid,
                campaign_id,
                lead_email,
                lead_first_name,
                lead_last_name,
                status,
                conversation_data,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @AdminUuid,
                @CampaignId,
                @LeadEmail,
                @LeadFirstName,
                @LeadLastName,
                @Status,
                @ConversationData,
                @CreatedAt,
                @UpdatedAt
            )
            RETURNING id";

        conversation.Id = conversation.Id ?? Guid.NewGuid().ToString();
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, conversation);
    }

    public async Task<bool> UpdateAsync(LeadConversationDbModel conversation)
    {
        const string sql = @"
            UPDATE lead_conversations SET
                admin_uuid = @AdminUuid,
                campaign_id = @CampaignId,
                lead_email = @LeadEmail,
                lead_first_name = @LeadFirstName,
                lead_last_name = @LeadLastName,
                status = @Status,
                conversation_data = @ConversationData,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        conversation.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, conversation);
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM lead_conversations WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByCampaignIdAsync(int campaignId)
    {
        const string sql = "DELETE FROM lead_conversations WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { CampaignId = campaignId });
        return rowsAffected > 0;
    }

    public async Task<int> CountByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM lead_conversations 
            WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql, new { CampaignId = campaignId });
    }

    private static LeadConversationDbModel MapToConversation(dynamic result)
    {
        return new LeadConversationDbModel
        {
            Id = result.id,
            AdminUuid = result.admin_uuid,
            CampaignId = result.campaign_id,
            LeadEmail = result.lead_email ?? string.Empty,
            LeadFirstName = result.lead_first_name ?? string.Empty,
            LeadLastName = result.lead_last_name ?? string.Empty,
            Status = result.status ?? string.Empty,
            ConversationData = result.conversation_data ?? string.Empty,
            CreatedAt = result.created_at,
            UpdatedAt = result.updated_at
        };
    }
}