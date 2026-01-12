using Dapper;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Models.Database.WebhookEvent;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LeadHype.Api.Core.Database.Repositories;

public class WebhookEventConfigRepository : IWebhookEventConfigRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WebhookEventConfigRepository> _logger;

    public WebhookEventConfigRepository(IConfiguration configuration, ILogger<WebhookEventConfigRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<IEnumerable<WebhookEventConfig>> GetByAdminAsync(string adminUuid)
    {
        const string sql = @"
            SELECT id, admin_uuid, webhook_id, event_type, name, description, 
                   config_parameters, target_scope, is_active, created_at, 
                   updated_at, last_checked_at, last_triggered_at
            FROM webhook_event_configs 
            WHERE admin_uuid = @AdminUuid 
            ORDER BY created_at DESC";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid });
            return results.Select(MapToWebhookEventConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook event configs for admin {AdminUuid}", adminUuid);
            throw;
        }
    }

    public async Task<WebhookEventConfig?> GetByIdAsync(string id)
    {
        const string sql = @"
            SELECT id, admin_uuid, webhook_id, event_type, name, description, 
                   config_parameters, target_scope, is_active, created_at, 
                   updated_at, last_checked_at, last_triggered_at
            FROM webhook_event_configs 
            WHERE id = @Id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return result != null ? MapToWebhookEventConfig(result) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook event config {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<WebhookEventConfig>> GetByWebhookIdAsync(string webhookId)
    {
        const string sql = @"
            SELECT id, admin_uuid, webhook_id, event_type, name, description, 
                   config_parameters, target_scope, is_active, created_at, 
                   updated_at, last_checked_at, last_triggered_at
            FROM webhook_event_configs 
            WHERE webhook_id = @WebhookId 
            ORDER BY created_at DESC";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync(sql, new { WebhookId = webhookId });
            return results.Select(MapToWebhookEventConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook event configs for webhook {WebhookId}", webhookId);
            throw;
        }
    }

    public async Task<IEnumerable<WebhookEventConfig>> GetActiveConfigsAsync()
    {
        const string sql = @"
            SELECT wec.id, wec.admin_uuid, wec.webhook_id, wec.event_type, wec.name, wec.description, 
                   wec.config_parameters, wec.target_scope, wec.is_active, wec.created_at, 
                   wec.updated_at, wec.last_checked_at, wec.last_triggered_at
            FROM webhook_event_configs wec
            INNER JOIN webhooks w ON wec.webhook_id = w.id
            WHERE wec.is_active = true AND w.is_active = true
            ORDER BY wec.created_at DESC";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync(sql);
            return results.Select(MapToWebhookEventConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active webhook event configs");
            throw;
        }
    }

    public async Task<WebhookEventConfig> CreateAsync(WebhookEventConfig config)
    {
        const string sql = @"
            INSERT INTO webhook_event_configs 
            (id, admin_uuid, webhook_id, event_type, name, description, config_parameters, 
             target_scope, is_active, created_at, updated_at)
            VALUES 
            (@Id, @AdminUuid, @WebhookId, @EventType, @Name, @Description, @ConfigParameters::jsonb,
             @TargetScope::jsonb, @IsActive, @CreatedAt, @UpdatedAt)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, config);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook event config {Id}", config.Id);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(WebhookEventConfig config)
    {
        const string sql = @"
            UPDATE webhook_event_configs 
            SET name = @Name,
                description = @Description,
                config_parameters = @ConfigParameters::jsonb,
                target_scope = @TargetScope::jsonb,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        try
        {
            config.UpdatedAt = DateTime.UtcNow;
            using var connection = new NpgsqlConnection(_connectionString);
            var affected = await connection.ExecuteAsync(sql, config);
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook event config {Id}", config.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM webhook_event_configs WHERE id = @Id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook event config {Id}", id);
            throw;
        }
    }

    public async Task UpdateLastCheckedAsync(string id, DateTime lastCheckedAt)
    {
        const string sql = @"
            UPDATE webhook_event_configs 
            SET last_checked_at = @LastCheckedAt
            WHERE id = @Id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { Id = id, LastCheckedAt = lastCheckedAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last checked time for webhook event config {Id}", id);
            throw;
        }
    }

    public async Task UpdateLastTriggeredAsync(string id, DateTime lastTriggeredAt)
    {
        const string sql = @"
            UPDATE webhook_event_configs 
            SET last_triggered_at = @LastTriggeredAt
            WHERE id = @Id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { Id = id, LastTriggeredAt = lastTriggeredAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last triggered time for webhook event config {Id}", id);
            throw;
        }
    }

    private static WebhookEventConfig MapToWebhookEventConfig(dynamic row)
    {
        return new WebhookEventConfig
        {
            Id = row.id,
            AdminUuid = row.admin_uuid,
            WebhookId = row.webhook_id,
            EventType = row.event_type,
            Name = row.name,
            Description = row.description ?? string.Empty,
            ConfigParameters = row.config_parameters ?? "{}",
            TargetScope = row.target_scope ?? "{}",
            IsActive = row.is_active,
            CreatedAt = row.created_at,
            UpdatedAt = row.updated_at,
            LastCheckedAt = row.last_checked_at,
            LastTriggeredAt = row.last_triggered_at
        };
    }
}

public class WebhookEventTriggerRepository : IWebhookEventTriggerRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WebhookEventTriggerRepository> _logger;

    public WebhookEventTriggerRepository(IConfiguration configuration, ILogger<WebhookEventTriggerRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<WebhookEventTrigger> CreateAsync(WebhookEventTrigger trigger)
    {
        const string sql = @"
            INSERT INTO webhook_event_triggers 
            (id, event_config_id, webhook_id, campaign_id, campaign_name, trigger_data,
             status_code, response_body, error_message, is_success, attempt_count, 
             created_at, delivered_at)
            VALUES 
            (@Id, @EventConfigId, @WebhookId, @CampaignId, @CampaignName, @TriggerData::jsonb,
             @StatusCode, @ResponseBody, @ErrorMessage, @IsSuccess, @AttemptCount,
             @CreatedAt, @DeliveredAt)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, trigger);
            return trigger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook event trigger {Id}", trigger.Id);
            throw;
        }
    }

    public async Task<IEnumerable<WebhookEventTrigger>> GetByEventConfigIdAsync(string eventConfigId, int limit = 50)
    {
        const string sql = @"
            SELECT id, event_config_id, webhook_id, campaign_id, campaign_name, trigger_data,
                   status_code, response_body, error_message, is_success, attempt_count,
                   created_at, delivered_at
            FROM webhook_event_triggers 
            WHERE event_config_id = @EventConfigId 
            ORDER BY created_at DESC
            LIMIT @Limit";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync(sql, new { EventConfigId = eventConfigId, Limit = limit });
            return results.Select(MapToWebhookEventTrigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook event triggers for config {EventConfigId}", eventConfigId);
            throw;
        }
    }

    public async Task<IEnumerable<WebhookEventTrigger>> GetByWebhookIdAsync(string webhookId, int limit = 50)
    {
        const string sql = @"
            SELECT id, event_config_id, webhook_id, campaign_id, campaign_name, trigger_data,
                   status_code, response_body, error_message, is_success, attempt_count,
                   created_at, delivered_at
            FROM webhook_event_triggers 
            WHERE webhook_id = @WebhookId 
            ORDER BY created_at DESC
            LIMIT @Limit";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync(sql, new { WebhookId = webhookId, Limit = limit });
            return results.Select(MapToWebhookEventTrigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook event triggers for webhook {WebhookId}", webhookId);
            throw;
        }
    }

    public async Task UpdateDeliveryStatusAsync(string id, int statusCode, string? responseBody, string? errorMessage, bool isSuccess)
    {
        const string sql = @"
            UPDATE webhook_event_triggers 
            SET status_code = @StatusCode,
                response_body = @ResponseBody,
                error_message = @ErrorMessage,
                is_success = @IsSuccess,
                delivered_at = @DeliveredAt
            WHERE id = @Id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new 
            { 
                Id = id, 
                StatusCode = statusCode, 
                ResponseBody = responseBody, 
                ErrorMessage = errorMessage, 
                IsSuccess = isSuccess,
                DeliveredAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook event trigger delivery status {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<WebhookEventTrigger>> GetRecentTriggersAsync(string adminUuid, int limit = 20)
    {
        const string sql = @"
            SELECT wt.id, wt.event_config_id, wt.webhook_id, wt.campaign_id, wt.campaign_name, 
                   wt.trigger_data, wt.status_code, wt.response_body, wt.error_message, 
                   wt.is_success, wt.attempt_count, wt.created_at, wt.delivered_at
            FROM webhook_event_triggers wt
            JOIN webhook_event_configs wc ON wt.event_config_id = wc.id
            WHERE wc.admin_uuid = @AdminUuid
            ORDER BY wt.created_at DESC
            LIMIT @Limit";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid, Limit = limit });
            return results.Select(MapToWebhookEventTrigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent webhook event triggers for admin {AdminUuid}", adminUuid);
            throw;
        }
    }

    private static WebhookEventTrigger MapToWebhookEventTrigger(dynamic row)
    {
        return new WebhookEventTrigger
        {
            Id = row.id,
            EventConfigId = row.event_config_id,
            WebhookId = row.webhook_id,
            CampaignId = row.campaign_id,
            CampaignName = row.campaign_name,
            TriggerData = row.trigger_data ?? "{}",
            StatusCode = row.status_code,
            ResponseBody = row.response_body,
            ErrorMessage = row.error_message,
            IsSuccess = row.is_success,
            AttemptCount = row.attempt_count,
            CreatedAt = row.created_at,
            DeliveredAt = row.delivered_at
        };
    }
}