using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeadHype.Api.Core.Database;
using LeadHype.Api.Core.Models;
using Dapper;
using Newtonsoft.Json;

namespace LeadHype.Api.Core.Database.Repositories
{
    public interface IWebhookRepository
    {
        Task<string> CreateAsync(Webhook webhook);
        Task<Webhook?> GetByIdAsync(string id);
        Task<IEnumerable<Webhook>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Webhook>> GetActiveWebhooksForEventAsync(string eventType);
        Task<bool> UpdateAsync(Webhook webhook);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpdateLastTriggeredAsync(string id, DateTime triggeredAt);
        Task<bool> IncrementFailureCountAsync(string id);
        Task<bool> ResetFailureCountAsync(string id);
        
        // Webhook deliveries
        Task<string> LogDeliveryAsync(WebhookDelivery delivery);
        Task<IEnumerable<WebhookDelivery>> GetDeliveriesAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null);
        Task<(IEnumerable<WebhookDelivery> deliveries, int totalCount)> GetDeliveriesWithCountAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null);
        Task<bool> UpdateDeliveryStatusAsync(string deliveryId, int statusCode, string? responseBody = null, string? errorMessage = null);
    }

    public class WebhookRepository : IWebhookRepository
    {
        private readonly IDbConnectionService _connectionService;

        public WebhookRepository(IDbConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public async Task<string> CreateAsync(Webhook webhook)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                INSERT INTO webhooks (id, user_id, name, url, headers, is_active, retry_count, timeout_seconds, created_at, updated_at)
                VALUES (@Id, @UserId, @Name, @Url, @Headers::jsonb, @IsActive, @RetryCount, @TimeoutSeconds, @CreatedAt, @UpdatedAt)
                RETURNING id";

            var id = await connection.QuerySingleAsync<string>(sql, new
            {
                webhook.Id,
                webhook.UserId,
                webhook.Name,
                webhook.Url,
                Headers = JsonConvert.SerializeObject(webhook.Headers),
                webhook.IsActive,
                webhook.RetryCount,
                webhook.TimeoutSeconds,
                webhook.CreatedAt,
                webhook.UpdatedAt
            });

            return id;
        }

        public async Task<Webhook?> GetByIdAsync(string id)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                SELECT id, user_id, name, url, headers, is_active, retry_count, timeout_seconds,
                       last_triggered_at, failure_count, created_at, updated_at
                FROM webhooks 
                WHERE id = @Id";

            var result = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            
            return result != null ? MapToWebhook(result) : null;
        }

        public async Task<IEnumerable<Webhook>> GetByUserIdAsync(string userId)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                SELECT id, user_id, name, url, headers, is_active, retry_count, timeout_seconds,
                       last_triggered_at, failure_count, created_at, updated_at
                FROM webhooks 
                WHERE user_id = @UserId
                ORDER BY created_at DESC";

            var results = await connection.QueryAsync(sql, new { UserId = userId });
            
            return results.Select(MapToWebhook);
        }

        public async Task<IEnumerable<Webhook>> GetActiveWebhooksForEventAsync(string eventType)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                SELECT id, user_id, name, url, headers, is_active, retry_count, timeout_seconds,
                       last_triggered_at, failure_count, created_at, updated_at
                FROM webhooks 
                WHERE is_active = true";

            var results = await connection.QueryAsync(sql);
            
            return results.Select(MapToWebhook);
        }

        public async Task<bool> UpdateAsync(Webhook webhook)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                UPDATE webhooks 
                SET name = @Name, url = @Url, headers = @Headers::jsonb, 
                    is_active = @IsActive, retry_count = @RetryCount, 
                    timeout_seconds = @TimeoutSeconds, updated_at = @UpdatedAt
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                webhook.Id,
                webhook.Name,
                webhook.Url,
                Headers = JsonConvert.SerializeObject(webhook.Headers),
                webhook.IsActive,
                webhook.RetryCount,
                webhook.TimeoutSeconds,
                UpdatedAt = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = "DELETE FROM webhooks WHERE id = @Id";
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

            return rowsAffected > 0;
        }

        public async Task<bool> UpdateLastTriggeredAsync(string id, DateTime triggeredAt)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                UPDATE webhooks 
                SET last_triggered_at = @TriggeredAt, updated_at = @UpdatedAt
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Id = id,
                TriggeredAt = triggeredAt,
                UpdatedAt = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        public async Task<bool> IncrementFailureCountAsync(string id)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                UPDATE webhooks 
                SET failure_count = failure_count + 1, updated_at = @UpdatedAt
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Id = id,
                UpdatedAt = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        public async Task<bool> ResetFailureCountAsync(string id)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                UPDATE webhooks 
                SET failure_count = 0, updated_at = @UpdatedAt
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Id = id,
                UpdatedAt = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        public async Task<string> LogDeliveryAsync(WebhookDelivery delivery)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                INSERT INTO webhook_deliveries (id, webhook_id, event_type, payload, status_code, response_body, 
                                               error_message, attempt_count, delivered_at, created_at)
                VALUES (@Id, @WebhookId, @EventType, @Payload::jsonb, @StatusCode, @ResponseBody, 
                        @ErrorMessage, @AttemptCount, @DeliveredAt, @CreatedAt)
                RETURNING id";

            var id = await connection.QuerySingleAsync<string>(sql, new
            {
                delivery.Id,
                delivery.WebhookId,
                delivery.EventType,
                Payload = JsonConvert.SerializeObject(delivery.Payload),
                delivery.StatusCode,
                delivery.ResponseBody,
                delivery.ErrorMessage,
                delivery.AttemptCount,
                delivery.DeliveredAt,
                delivery.CreatedAt
            });

            return id;
        }

        public async Task<IEnumerable<WebhookDelivery>> GetDeliveriesAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            var whereClause = "WHERE webhook_id = @WebhookId";
            if (failuresOnly == true)
            {
                whereClause += " AND (status_code IS NULL OR status_code < 200 OR status_code >= 300)";
            }
            else if (failuresOnly == false)
            {
                whereClause += " AND status_code IS NOT NULL AND status_code >= 200 AND status_code < 300";
            }

            var sql = $@"
                SELECT id, webhook_id, event_type, payload, status_code, response_body, 
                       error_message, attempt_count, delivered_at, created_at
                FROM webhook_deliveries 
                {whereClause}
                ORDER BY created_at DESC
                LIMIT @Limit OFFSET @Offset";

            var results = await connection.QueryAsync(sql, new { WebhookId = webhookId, Limit = limit, Offset = offset });
            
            return results.Select(MapToWebhookDelivery);
        }

        public async Task<(IEnumerable<WebhookDelivery> deliveries, int totalCount)> GetDeliveriesWithCountAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            var whereClause = "WHERE webhook_id = @WebhookId";
            if (failuresOnly == true)
            {
                whereClause += " AND (status_code IS NULL OR status_code < 200 OR status_code >= 300)";
            }
            else if (failuresOnly == false)
            {
                whereClause += " AND status_code IS NOT NULL AND status_code >= 200 AND status_code < 300";
            }

            // Get total count
            var countSql = $@"
                SELECT COUNT(*) 
                FROM webhook_deliveries 
                {whereClause}";

            var totalCount = await connection.QuerySingleAsync<int>(countSql, new { WebhookId = webhookId });

            // Get paginated deliveries
            var sql = $@"
                SELECT id, webhook_id, event_type, payload, status_code, response_body, 
                       error_message, attempt_count, delivered_at, created_at
                FROM webhook_deliveries 
                {whereClause}
                ORDER BY created_at DESC
                LIMIT @Limit OFFSET @Offset";

            var results = await connection.QueryAsync(sql, new { WebhookId = webhookId, Limit = limit, Offset = offset });
            var deliveries = results.Select(MapToWebhookDelivery);
            
            return (deliveries, totalCount);
        }

        public async Task<bool> UpdateDeliveryStatusAsync(string deliveryId, int statusCode, string? responseBody = null, string? errorMessage = null)
        {
            using var connection = await _connectionService.GetConnectionAsync();

            const string sql = @"
                UPDATE webhook_deliveries 
                SET status_code = @StatusCode, response_body = @ResponseBody, error_message = @ErrorMessage,
                    delivered_at = @DeliveredAt
                WHERE id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Id = deliveryId,
                StatusCode = statusCode,
                ResponseBody = responseBody,
                ErrorMessage = errorMessage,
                DeliveredAt = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        private static Webhook MapToWebhook(dynamic row)
        {
            return new Webhook
            {
                Id = row.id,
                UserId = row.user_id,
                Name = row.name,
                Url = row.url,
                Headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(row.headers) ?? new Dictionary<string, string>(),
                IsActive = row.is_active,
                RetryCount = row.retry_count,
                TimeoutSeconds = row.timeout_seconds,
                LastTriggeredAt = row.last_triggered_at,
                FailureCount = row.failure_count,
                CreatedAt = row.created_at,
                UpdatedAt = row.updated_at
            };
        }

        private static WebhookDelivery MapToWebhookDelivery(dynamic row)
        {
            return new WebhookDelivery
            {
                Id = row.id,
                WebhookId = row.webhook_id,
                EventType = row.event_type,
                Payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(row.payload) ?? new Dictionary<string, object>(),
                StatusCode = row.status_code,
                ResponseBody = row.response_body,
                ErrorMessage = row.error_message,
                AttemptCount = row.attempt_count,
                DeliveredAt = row.delivered_at,
                CreatedAt = row.created_at
            };
        }
    }
}