using LeadHype.Api.Models;
using Dapper;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly IDbConnectionService _connectionService;

    public EmailTemplateRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<EmailTemplateDbModel>> GetByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                subject,
                body,
                sequence_number,
                created_at,
                updated_at
            FROM email_templates
            WHERE campaign_id = @CampaignId
            ORDER BY sequence_number";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { CampaignId = campaignId });
        
        return results.Select(MapToTemplate);
    }

    public async Task<IEnumerable<EmailTemplateDbModel>> GetByAdminUuidAsync(string adminUuid)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                campaign_id,
                subject,
                body,
                sequence_number,
                created_at,
                updated_at
            FROM email_templates
            WHERE admin_uuid = @AdminUuid
            ORDER BY campaign_id, sequence_number";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid });
        
        return results.Select(MapToTemplate);
    }

    public async Task<string> CreateAsync(EmailTemplateDbModel template)
    {
        const string sql = @"
            INSERT INTO email_templates (
                id,
                admin_uuid,
                campaign_id,
                subject,
                body,
                sequence_number,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @AdminUuid,
                @CampaignId,
                @Subject,
                @Body,
                @SequenceNumber,
                @CreatedAt,
                @UpdatedAt
            )
            RETURNING id";

        template.Id = template.Id ?? Guid.NewGuid().ToString();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, template);
    }

    public async Task<string> UpsertAsync(EmailTemplateDbModel template)
    {
        const string sql = @"
            INSERT INTO email_templates (
                id,
                admin_uuid,
                campaign_id,
                subject,
                body,
                sequence_number,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @AdminUuid,
                @CampaignId,
                @Subject,
                @Body,
                @SequenceNumber,
                @CreatedAt,
                @UpdatedAt
            )
            ON CONFLICT (campaign_id, sequence_number)
            DO UPDATE SET
                subject = EXCLUDED.subject,
                body = EXCLUDED.body,
                updated_at = EXCLUDED.updated_at
            RETURNING id";

        template.Id = template.Id ?? Guid.NewGuid().ToString();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, template);
    }

    public async Task<bool> UpdateAsync(EmailTemplateDbModel template)
    {
        const string sql = @"
            UPDATE email_templates SET
                admin_uuid = @AdminUuid,
                campaign_id = @CampaignId,
                subject = @Subject,
                body = @Body,
                sequence_number = @SequenceNumber,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        template.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, template);
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM email_templates WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByCampaignIdAsync(int campaignId)
    {
        const string sql = "DELETE FROM email_templates WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { CampaignId = campaignId });
        return rowsAffected > 0;
    }

    public async Task<int> CountByCampaignIdAsync(int campaignId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM email_templates
            WHERE campaign_id = @CampaignId";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql, new { CampaignId = campaignId });
    }

    public async Task<Dictionary<EmailTemplateDbModel, List<EmailTemplateVariantDbModel>>> GetByCampaignIdWithVariantsAsync(int campaignId)
    {
        const string sql = @"
            SELECT
                t.id,
                t.admin_uuid,
                t.campaign_id,
                t.subject,
                t.body,
                t.sequence_number,
                t.created_at,
                t.updated_at,
                v.id as variant_id,
                v.template_id as variant_template_id,
                v.smartlead_variant_id,
                v.variant_label,
                v.subject as variant_subject,
                v.body as variant_body,
                v.created_at as variant_created_at,
                v.updated_at as variant_updated_at
            FROM email_templates t
            LEFT JOIN email_template_variants v ON t.id = v.template_id
            WHERE t.campaign_id = @CampaignId
            ORDER BY t.sequence_number, v.variant_label";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { CampaignId = campaignId });

        var templateDict = new Dictionary<EmailTemplateDbModel, List<EmailTemplateVariantDbModel>>();
        EmailTemplateDbModel? currentTemplate = null;

        foreach (var result in results)
        {
            // Check if this is a new template
            string templateId = result.id;
            if (currentTemplate == null || currentTemplate.Id != templateId)
            {
                currentTemplate = MapToTemplate(result);
                templateDict[currentTemplate] = new List<EmailTemplateVariantDbModel>();
            }

            // Add variant if it exists
            if (result.variant_id != null)
            {
                var variant = new EmailTemplateVariantDbModel
                {
                    Id = result.variant_id,
                    TemplateId = result.variant_template_id,
                    SmartleadVariantId = result.smartlead_variant_id,
                    VariantLabel = result.variant_label ?? string.Empty,
                    Subject = result.variant_subject ?? string.Empty,
                    Body = result.variant_body ?? string.Empty,
                    CreatedAt = result.variant_created_at,
                    UpdatedAt = result.variant_updated_at
                };
                templateDict[currentTemplate].Add(variant);
            }
        }

        return templateDict;
    }

    public async Task<string> UpsertVariantAsync(EmailTemplateVariantDbModel variant)
    {
        const string sql = @"
            INSERT INTO email_template_variants (
                id,
                template_id,
                smartlead_variant_id,
                variant_label,
                subject,
                body,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @TemplateId,
                @SmartleadVariantId,
                @VariantLabel,
                @Subject,
                @Body,
                @CreatedAt,
                @UpdatedAt
            )
            ON CONFLICT (template_id, variant_label)
            DO UPDATE SET
                smartlead_variant_id = EXCLUDED.smartlead_variant_id,
                subject = EXCLUDED.subject,
                body = EXCLUDED.body,
                updated_at = EXCLUDED.updated_at
            RETURNING id";

        variant.Id = variant.Id ?? Guid.NewGuid().ToString();
        variant.CreatedAt = DateTime.UtcNow;
        variant.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, variant);
    }

    public async Task<bool> DeleteVariantsByTemplateIdAsync(string templateId)
    {
        const string sql = "DELETE FROM email_template_variants WHERE template_id = @TemplateId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { TemplateId = templateId });
        return rowsAffected > 0;
    }

    private static EmailTemplateDbModel MapToTemplate(dynamic result)
    {
        return new EmailTemplateDbModel
        {
            Id = result.id,
            AdminUuid = result.admin_uuid,
            CampaignId = result.campaign_id,
            Subject = result.subject ?? string.Empty,
            Body = result.body ?? string.Empty,
            SequenceNumber = result.sequence_number,
            CreatedAt = result.created_at,
            UpdatedAt = result.updated_at
        };
    }
}