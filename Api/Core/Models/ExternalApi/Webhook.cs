using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models
{
    /// <summary>
    /// Webhook configuration model
    /// </summary>
    public class Webhook
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;
        
        
        /// <summary>
        /// Custom headers to send with webhook requests
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// List of event types this webhook should listen for (empty means all events)
        /// </summary>
        public List<string> Events { get; set; } = new List<string>();
        
        public bool IsActive { get; set; } = true;
        public int RetryCount { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
        public DateTime? LastTriggeredAt { get; set; }
        public int FailureCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Webhook delivery log model
    /// </summary>
    public class WebhookDelivery
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string WebhookId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorMessage { get; set; }
        public int AttemptCount { get; set; } = 1;
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


    /// <summary>
    /// Request models for webhook management
    /// </summary>
    public class CreateWebhookRequest
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;
        
        
        public Dictionary<string, string>? Headers { get; set; }
        public List<string>? Events { get; set; }
        public int RetryCount { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class UpdateWebhookRequest
    {
        [StringLength(255)]
        public string? Name { get; set; }
        
        [Url]
        public string? Url { get; set; }
        
        public Dictionary<string, string>? Headers { get; set; }
        public List<string>? Events { get; set; }
        public int? RetryCount { get; set; }
        public int? TimeoutSeconds { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Response models for webhook APIs
    /// </summary>
    public class WebhookResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public List<string> Events { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public int RetryCount { get; set; }
        public int TimeoutSeconds { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public int FailureCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WebhookDeliveryResponse
    {
        public string Id { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorMessage { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }
}