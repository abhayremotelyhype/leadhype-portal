using LeadHype.Api.Core.Models.Database.WebhookEvent;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IWebhookEventConfigRepository
{
    /// <summary>
    /// Get all webhook event configs for an admin
    /// </summary>
    Task<IEnumerable<WebhookEventConfig>> GetByAdminAsync(string adminUuid);
    
    /// <summary>
    /// Get webhook event config by ID
    /// </summary>
    Task<WebhookEventConfig?> GetByIdAsync(string id);
    
    /// <summary>
    /// Get all webhook event configs for a specific webhook
    /// </summary>
    Task<IEnumerable<WebhookEventConfig>> GetByWebhookIdAsync(string webhookId);
    
    /// <summary>
    /// Get all active webhook event configs for monitoring
    /// </summary>
    Task<IEnumerable<WebhookEventConfig>> GetActiveConfigsAsync();
    
    /// <summary>
    /// Create a new webhook event config
    /// </summary>
    Task<WebhookEventConfig> CreateAsync(WebhookEventConfig config);
    
    /// <summary>
    /// Update existing webhook event config
    /// </summary>
    Task<bool> UpdateAsync(WebhookEventConfig config);
    
    /// <summary>
    /// Delete webhook event config
    /// </summary>
    Task<bool> DeleteAsync(string id);
    
    /// <summary>
    /// Update last checked timestamp
    /// </summary>
    Task UpdateLastCheckedAsync(string id, DateTime lastCheckedAt);
    
    /// <summary>
    /// Update last triggered timestamp
    /// </summary>
    Task UpdateLastTriggeredAsync(string id, DateTime lastTriggeredAt);
}

public interface IWebhookEventTriggerRepository
{
    /// <summary>
    /// Create a new webhook event trigger log
    /// </summary>
    Task<WebhookEventTrigger> CreateAsync(WebhookEventTrigger trigger);
    
    /// <summary>
    /// Get triggers for a specific event config
    /// </summary>
    Task<IEnumerable<WebhookEventTrigger>> GetByEventConfigIdAsync(string eventConfigId, int limit = 50);
    
    /// <summary>
    /// Get triggers for a specific webhook
    /// </summary>
    Task<IEnumerable<WebhookEventTrigger>> GetByWebhookIdAsync(string webhookId, int limit = 50);
    
    /// <summary>
    /// Update trigger delivery status
    /// </summary>
    Task UpdateDeliveryStatusAsync(string id, int statusCode, string? responseBody, string? errorMessage, bool isSuccess);
    
    /// <summary>
    /// Get recent triggers for dashboard
    /// </summary>
    Task<IEnumerable<WebhookEventTrigger>> GetRecentTriggersAsync(string adminUuid, int limit = 20);
}