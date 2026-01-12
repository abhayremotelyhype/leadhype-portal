using LeadHype.Api.Models;
using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly IDbConnectionService _connectionService;

    public CampaignRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<CampaignDetailsDbModel>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                name,
                client_id,
                status,
                total_positive_replies,
                total_leads,
                total_sent,
                total_opened,
                total_replied,
                total_bounced,
                total_clicked,
                email_ids::text as email_ids_json,
                tags::text as tags_json,
                created_at,
                updated_at,
                last_updated_at,
                notes
            FROM campaigns
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql);
        
        return results.Select(MapToCampaign);
    }

    public async Task<CampaignDetailsDbModel?> GetByIdAsync(string id)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                name,
                client_id,
                status,
                total_positive_replies,
                total_leads,
                total_sent,
                total_opened,
                total_replied,
                total_bounced,
                total_clicked,
                email_ids::text as email_ids_json,
                tags::text as tags_json,
                created_at,
                updated_at,
                last_updated_at,
                notes
            FROM campaigns
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
        
        return result != null ? MapToCampaign(result) : null;
    }

    public async Task<IEnumerable<CampaignDetailsDbModel>> GetByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                name,
                client_id,
                status,
                total_positive_replies,
                total_leads,
                total_sent,
                total_opened,
                total_replied,
                total_bounced,
                total_clicked,
                email_ids::text as email_ids_json,
                tags::text as tags_json,
                created_at,
                updated_at,
                last_updated_at,
                notes
            FROM campaigns
            WHERE campaign_id = @CampaignId
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { CampaignId = campaignId });
        
        return results.Select(MapToCampaign);
    }

    public async Task<IEnumerable<CampaignDetailsDbModel>> GetByAdminUuidAsync(string adminUuid)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                name,
                client_id,
                status,
                total_positive_replies,
                total_leads,
                total_sent,
                total_opened,
                total_replied,
                total_bounced,
                total_clicked,
                email_ids::text as email_ids_json,
                tags::text as tags_json,
                created_at,
                updated_at,
                last_updated_at,
                notes
            FROM campaigns
            WHERE admin_uuid = @AdminUuid
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid });
        
        return results.Select(MapToCampaign);
    }

    public async Task<IEnumerable<CampaignDetailsDbModel>> GetByClientIdAsync(string clientId)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                name,
                client_id,
                status,
                total_positive_replies,
                total_leads,
                total_sent,
                total_opened,
                total_replied,
                total_bounced,
                total_clicked,
                email_ids::text as email_ids_json,
                tags::text as tags_json,
                created_at,
                updated_at,
                last_updated_at,
                notes
            FROM campaigns
            WHERE client_id = @ClientId
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { ClientId = clientId });
        
        return results.Select(MapToCampaign);
    }

    public async Task<string> CreateAsync(CampaignDetailsDbModel campaign)
    {
        const string sql = @"
            INSERT INTO campaigns (
                id,
                admin_uuid,
                campaign_id,
                name,
                client_id,
                status,
                total_positive_replies,
                total_leads,
                total_sent,
                total_opened,
                total_replied,
                total_bounced,
                total_clicked,
                email_ids,
                tags,
                created_at,
                updated_at,
                last_updated_at
            )
            VALUES (
                @Id,
                @AdminUuid,
                @CampaignId,
                @Name,
                @ClientId,
                @Status,
                @TotalPositiveReplies,
                @TotalLeads,
                @TotalSent,
                @TotalOpened,
                @TotalReplied,
                @TotalBounced,
                @TotalClicked,
                @EmailIdsJson::jsonb,
                @TagsJson::jsonb,
                @CreatedAt,
                @UpdatedAt,
                @LastUpdatedAt
            )
            RETURNING id";

        campaign.Id = campaign.Id ?? Guid.NewGuid().ToString();
        campaign.CreatedAt ??= DateTime.UtcNow;
        campaign.UpdatedAt ??= DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, new
        {
            campaign.Id,
            campaign.AdminUuid,
            campaign.CampaignId,
            campaign.Name,
            campaign.ClientId,
            campaign.Status,
            campaign.TotalPositiveReplies,
            campaign.TotalLeads,
            campaign.TotalSent,
            campaign.TotalOpened,
            campaign.TotalReplied,
            campaign.TotalBounced,
            campaign.TotalClicked,
            EmailIdsJson = JsonConvert.SerializeObject(campaign.EmailIds),
            TagsJson = JsonConvert.SerializeObject(campaign.Tags),
            campaign.CreatedAt,
            campaign.UpdatedAt,
            campaign.LastUpdatedAt
        });
    }

    public async Task<bool> UpdateAsync(CampaignDetailsDbModel campaign)
    {
        const string sql = @"
            UPDATE campaigns SET
                admin_uuid = @AdminUuid,
                campaign_id = @CampaignId,
                name = @Name,
                client_id = @ClientId,
                status = @Status,
                total_positive_replies = @TotalPositiveReplies,
                total_leads = @TotalLeads,
                total_sent = @TotalSent,
                total_opened = @TotalOpened,
                total_replied = @TotalReplied,
                total_bounced = @TotalBounced,
                total_clicked = @TotalClicked,
                email_ids = @EmailIdsJson::jsonb,
                tags = @TagsJson::jsonb,
                updated_at = @UpdatedAt,
                last_updated_at = @LastUpdatedAt,
                notes = @Notes
            WHERE id = @Id";

        campaign.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            campaign.Id,
            campaign.AdminUuid,
            campaign.CampaignId,
            campaign.Name,
            campaign.ClientId,
            campaign.Status,
            campaign.TotalPositiveReplies,
            campaign.TotalLeads,
            campaign.TotalSent,
            campaign.TotalOpened,
            campaign.TotalReplied,
            campaign.TotalBounced,
            campaign.TotalClicked,
            EmailIdsJson = JsonConvert.SerializeObject(campaign.EmailIds),
            TagsJson = JsonConvert.SerializeObject(campaign.Tags),
            campaign.UpdatedAt,
            campaign.LastUpdatedAt,
            campaign.Notes
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM campaigns WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<int> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM campaigns";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql);
    }

    public async Task<int> CountByStatusAsync(string status)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM campaigns 
            WHERE status = @Status";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql, new { Status = status });
    }

    private static CampaignDetailsDbModel MapToCampaign(dynamic result)
    {
        var emailIds = new List<long>();
        if (!string.IsNullOrEmpty(result.email_ids_json))
        {
            try
            {
                emailIds = JsonConvert.DeserializeObject<List<long>>(result.email_ids_json) ?? new List<long>();
            }
            catch
            {
                emailIds = new List<long>();
            }
        }

        var tags = new List<string>();
        if (!string.IsNullOrEmpty(result.tags_json))
        {
            try
            {
                tags = JsonConvert.DeserializeObject<List<string>>(result.tags_json) ?? new List<string>();
            }
            catch
            {
                tags = new List<string>();
            }
        }

        return new CampaignDetailsDbModel
        {
            Id = result.id,
            AdminUuid = result.admin_uuid,
            CampaignId = result.campaign_id,
            Name = result.name,
            ClientId = result.client_id,
            Status = result.status,
            TotalPositiveReplies = result.total_positive_replies,
            TotalLeads = result.total_leads,
            TotalSent = result.total_sent,
            TotalOpened = result.total_opened,
            TotalReplied = result.total_replied,
            TotalBounced = result.total_bounced,
            TotalClicked = result.total_clicked,
            EmailIds = emailIds,
            Tags = tags,
            CreatedAt = result.created_at,
            UpdatedAt = result.updated_at,
            LastUpdatedAt = result.last_updated_at,
            Notes = result.notes
        };
    }
}