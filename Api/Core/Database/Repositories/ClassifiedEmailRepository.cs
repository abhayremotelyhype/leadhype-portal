using Dapper;
using LeadHype.Api.Models;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class ClassifiedEmailRepository : IClassifiedEmailRepository
{
    private readonly IDbConnectionService _connectionService;

    public ClassifiedEmailRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<ClassifiedEmailDbModel> CreateAsync(ClassifiedEmailDbModel classifiedEmail)
    {
        const string sql = @"
            INSERT INTO classified_emails
            (id, admin_uuid, campaign_id, email_account_id, message_id, lead_email, email_type, email_time, email_body_hash,
             classification_result, classified_at, created_at, updated_at)
            VALUES
            (@Id, @AdminUuid, @CampaignId, @EmailAccountId, @MessageId, @LeadEmail, @EmailType, @EmailTime, @EmailBodyHash,
             @ClassificationResult, @ClassifiedAt, @CreatedAt, @UpdatedAt)
            RETURNING *";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QuerySingleAsync<ClassifiedEmailDbModel>(sql, classifiedEmail);
        return result;
    }

    public async Task<ClassifiedEmailDbModel> UpsertAsync(ClassifiedEmailDbModel classifiedEmail)
    {
        const string sql = @"
            INSERT INTO classified_emails
            (id, admin_uuid, campaign_id, email_account_id, message_id, lead_email, email_type, email_time, email_body_hash,
             classification_result, classified_at, created_at, updated_at)
            VALUES
            (@Id, @AdminUuid, @CampaignId, @EmailAccountId, @MessageId, @LeadEmail, @EmailType, @EmailTime, @EmailBodyHash,
             @ClassificationResult, @ClassifiedAt, @CreatedAt, @UpdatedAt)
            ON CONFLICT (message_id)
            DO UPDATE SET
                classification_result = EXCLUDED.classification_result,
                classified_at = EXCLUDED.classified_at,
                updated_at = EXCLUDED.updated_at
            RETURNING *";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QuerySingleAsync<ClassifiedEmailDbModel>(sql, classifiedEmail);
        return result;
    }

    public async Task<bool> UpdateAsync(ClassifiedEmailDbModel classifiedEmail)
    {
        const string sql = @"
            UPDATE classified_emails 
            SET admin_uuid = @AdminUuid, campaign_id = @CampaignId, email_account_id = @EmailAccountId, message_id = @MessageId, 
                lead_email = @LeadEmail, email_type = @EmailType, email_time = @EmailTime,
                email_body_hash = @EmailBodyHash, classification_result = @ClassificationResult, 
                classified_at = @ClassifiedAt, updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, classifiedEmail);
        return rowsAffected > 0;
    }

    public async Task<ClassifiedEmailDbModel?> GetByMessageIdAsync(string messageId)
    {
        const string sql = @"
            SELECT * FROM classified_emails 
            WHERE message_id = @MessageId 
            LIMIT 1";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<ClassifiedEmailDbModel>(sql, new { MessageId = messageId });
        return result;
    }

    public async Task<bool> ExistsByMessageIdAsync(string messageId)
    {
        const string sql = @"
            SELECT COUNT(1) FROM classified_emails 
            WHERE message_id = @MessageId";

        using var connection = await _connectionService.GetConnectionAsync();
        var count = await connection.QuerySingleAsync<int>(sql, new { MessageId = messageId });
        return count > 0;
    }

    public async Task<bool> ExistsByEmailBodyHashAsync(string emailBodyHash)
    {
        const string sql = @"
            SELECT COUNT(1) FROM classified_emails 
            WHERE email_body_hash = @EmailBodyHash";

        using var connection = await _connectionService.GetConnectionAsync();
        var count = await connection.QuerySingleAsync<int>(sql, new { EmailBodyHash = emailBodyHash });
        return count > 0;
    }

    public async Task<List<ClassifiedEmailDbModel>> GetByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT * FROM classified_emails 
            WHERE campaign_id = @CampaignId 
            ORDER BY email_time DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryAsync<ClassifiedEmailDbModel>(sql, new { CampaignId = campaignId });
        return result.ToList();
    }

    public async Task<List<ClassifiedEmailDbModel>> GetByEmailAccountIdAsync(int emailAccountId)
    {
        const string sql = @"
            SELECT * FROM classified_emails 
            WHERE email_account_id = @EmailAccountId 
            ORDER BY email_time DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryAsync<ClassifiedEmailDbModel>(sql, new { EmailAccountId = emailAccountId });
        return result.ToList();
    }

    public async Task<List<ClassifiedEmailDbModel>> GetByLeadEmailAsync(string leadEmail)
    {
        const string sql = @"
            SELECT * FROM classified_emails 
            WHERE lead_email = @LeadEmail 
            ORDER BY email_time DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryAsync<ClassifiedEmailDbModel>(sql, new { LeadEmail = leadEmail });
        return result.ToList();
    }

    public async Task<int> GetTotalClassifiedCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM classified_emails";

        using var connection = await _connectionService.GetConnectionAsync();
        var count = await connection.QuerySingleAsync<int>(sql);
        return count;
    }

    public async Task<int> GetPositiveRepliesCountByEmailAccountAsync(int emailAccountId)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM classified_emails 
            WHERE email_account_id = @EmailAccountId 
            AND UPPER(classification_result) IN ('POSITIVE', 'INTERESTED', 'YES')";

        using var connection = await _connectionService.GetConnectionAsync();
        var count = await connection.QuerySingleAsync<int>(sql, new { EmailAccountId = emailAccountId });
        return count;
    }

    public async Task<Dictionary<int, int>> GetPositiveRepliesCountForEmailAccountsAsync(List<int> emailAccountIds)
    {
        if (!emailAccountIds.Any())
            return new Dictionary<int, int>();

        const string sql = @"
            SELECT email_account_id, COUNT(*) as count
            FROM classified_emails 
            WHERE email_account_id = ANY(@EmailAccountIds)
            AND UPPER(classification_result) IN ('POSITIVE', 'INTERESTED', 'YES')
            GROUP BY email_account_id";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { EmailAccountIds = emailAccountIds.ToArray() });
        
        return results.ToDictionary(
            r => (int)r.email_account_id, 
            r => (int)r.count
        );
    }

    public async Task<List<ClassifiedEmailDbModel>> GetRecentClassificationsAsync(int limit = 100)
    {
        const string sql = @"
            SELECT * FROM classified_emails 
            ORDER BY classified_at DESC 
            LIMIT @Limit";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryAsync<ClassifiedEmailDbModel>(sql, new { Limit = limit });
        return result.ToList();
    }
}