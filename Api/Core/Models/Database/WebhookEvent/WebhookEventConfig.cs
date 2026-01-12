using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.Database.WebhookEvent
{
    /// <summary>
    /// Configuration for webhook events that monitor campaign performance
    /// </summary>
    public class WebhookEventConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AdminUuid { get; set; } = string.Empty;
        public string WebhookId { get; set; } = string.Empty;
        
        [Required]
        public string EventType { get; set; } = string.Empty; // e.g., "reply_rate_drop"
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Event configuration parameters (JSON)
        /// For reply_rate_drop: { "thresholdPercent": 5.0, "monitoringPeriodDays": 7 }
        /// </summary>
        public string ConfigParameters { get; set; } = "{}";
        
        /// <summary>
        /// Target scope configuration (JSON)
        /// { "type": "clients", "ids": ["client1", "client2"] } or
        /// { "type": "campaigns", "ids": ["campaign1", "campaign2"] } or
        /// { "type": "users", "ids": ["user1", "user2"] }
        /// </summary>
        public string TargetScope { get; set; } = "{}";
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastCheckedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
    }
    
    /// <summary>
    /// Log of webhook event triggers
    /// </summary>
    public class WebhookEventTrigger
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventConfigId { get; set; } = string.Empty;
        public string WebhookId { get; set; } = string.Empty;
        
        /// <summary>
        /// Campaign that triggered the event
        /// </summary>
        public string CampaignId { get; set; } = string.Empty;
        public string CampaignName { get; set; } = string.Empty;
        
        /// <summary>
        /// Trigger details (JSON)
        /// Contains current metrics, affected email accounts, etc.
        /// </summary>
        public string TriggerData { get; set; } = "{}";
        
        /// <summary>
        /// Webhook delivery attempt details
        /// </summary>
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess { get; set; } = false;
        public int AttemptCount { get; set; } = 1;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
    }
    
    /// <summary>
    /// Request/Response models for API
    /// </summary>
    public class CreateWebhookEventConfigRequest
    {
        [Required]
        public string WebhookId { get; set; } = string.Empty;
        
        [Required]
        public string EventType { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> ConfigParameters { get; set; } = new();
        public TargetScopeConfig TargetScope { get; set; } = new();
    }
    
    public class UpdateWebhookEventConfigRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object>? ConfigParameters { get; set; }
        public TargetScopeConfig? TargetScope { get; set; }
        public bool? IsActive { get; set; }
    }
    
    public class TargetScopeConfig
    {
        [Required]
        public string Type { get; set; } = string.Empty; // "clients", "campaigns", or "users"
        
        [Required]
        public List<string> Ids { get; set; } = new();
    }
    
    public class WebhookEventConfigResponse
    {
        public string Id { get; set; } = string.Empty;
        public string WebhookId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> ConfigParameters { get; set; } = new();
        public TargetScopeConfig TargetScope { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
    }
    
    /// <summary>
    /// Webhook payload for reply rate drop events
    /// </summary>
    public class ReplyRateDropWebhookPayload
    {
        public string EventType { get; set; } = "reply_rate_drop";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventConfigId { get; set; } = string.Empty;
        public string EventConfigName { get; set; } = string.Empty;
        
        public CampaignMetrics Campaign { get; set; } = new();
        public List<EmailAccountImpact> AffectedEmailAccounts { get; set; } = new();
        public ThresholdDetails Threshold { get; set; } = new();
        
        public class CampaignMetrics
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public double CurrentReplyRate { get; set; }
            public double PreviousReplyRate { get; set; }
            public double ReplyRateDrop { get; set; }
            public int TotalSent7Days { get; set; }
            public int TotalReplied7Days { get; set; }
        }
        
        public class EmailAccountImpact
        {
            public long EmailAccountId { get; set; }
            public string EmailAddress { get; set; } = string.Empty;
            public double ReplyRate7Days { get; set; }
            public int Sent7Days { get; set; }
            public int Replied7Days { get; set; }
            public string ImpactLevel { get; set; } = string.Empty; // "High", "Medium", "Low"
        }
        
        public class ThresholdDetails
        {
            public double ThresholdPercent { get; set; }
            public int MonitoringPeriodDays { get; set; }
            public int MinimumEmailsSent { get; set; }
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
        }
    }
    
    /// <summary>
    /// Webhook payload for high bounce rate events
    /// </summary>
    public class BounceRateHighWebhookPayload
    {
        public string EventType { get; set; } = "bounce_rate_high";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventConfigId { get; set; } = string.Empty;
        public string EventConfigName { get; set; } = string.Empty;
        
        public CampaignMetrics Campaign { get; set; } = new();
        public List<EmailAccountImpact> AffectedEmailAccounts { get; set; } = new();
        public ThresholdDetails Threshold { get; set; } = new();
        
        public class CampaignMetrics
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public double CurrentBounceRate { get; set; }
            public double PreviousBounceRate { get; set; }
            public double BounceRateIncrease { get; set; }
            public int TotalSent7Days { get; set; }
            public int TotalBounced7Days { get; set; }
        }
        
        public class EmailAccountImpact
        {
            public long EmailAccountId { get; set; }
            public string EmailAddress { get; set; } = string.Empty;
            public double BounceRate7Days { get; set; }
            public int Sent7Days { get; set; }
            public int Bounced7Days { get; set; }
            public string ImpactLevel { get; set; } = string.Empty; // "High", "Medium", "Low"
        }
        
        public class ThresholdDetails
        {
            public double ThresholdPercent { get; set; }
            public int MonitoringPeriodDays { get; set; }
            public int MinimumEmailsSent { get; set; }
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
        }
    }

    /// <summary>
    /// Webhook payload for no positive reply for X days events
    /// </summary>
    public class NoPositiveReplyForXDaysWebhookPayload
    {
        public string EventType { get; set; } = "no_positive_reply_for_x_days";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventConfigId { get; set; } = string.Empty;
        public string EventConfigName { get; set; } = string.Empty;
        
        public List<CampaignNoReplyInfo> AffectedCampaigns { get; set; } = new();
        public NoReplyThresholdDetails Threshold { get; set; } = new();
        
        public class CampaignNoReplyInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public DateTime? LastPositiveReplyDate { get; set; }
            public int DaysSinceLastPositiveReply { get; set; }
            public int TotalSentInPeriod { get; set; }
            public int TotalRepliesInPeriod { get; set; }
            public int PositiveRepliesInPeriod { get; set; }
            public List<EmailAccountNoReplyInfo> EmailAccounts { get; set; } = new();
        }
        
        public class EmailAccountNoReplyInfo
        {
            public long EmailAccountId { get; set; }
            public string EmailAddress { get; set; } = string.Empty;
            public DateTime? LastPositiveReplyDate { get; set; }
            public int DaysSinceLastPositiveReply { get; set; }
            public int SentInPeriod { get; set; }
            public int RepliesInPeriod { get; set; }
            public int PositiveRepliesInPeriod { get; set; }
        }
        
        public class NoReplyThresholdDetails
        {
            public int DaysSinceLastReply { get; set; }
            public DateTime CheckDate { get; set; }
            public DateTime ThresholdDate { get; set; }
        }
    }

    /// <summary>
    /// Webhook payload for no reply for X days events
    /// </summary>
    public class NoReplyForXDaysWebhookPayload
    {
        public string EventType { get; set; } = "no_reply_for_x_days";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventConfigId { get; set; } = string.Empty;
        public string EventConfigName { get; set; } = string.Empty;
        
        public List<CampaignNoReplyInfo> AffectedCampaigns { get; set; } = new();
        public NoReplyThresholdDetails Threshold { get; set; } = new();
        
        public class CampaignNoReplyInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public DateTime? LastReplyDate { get; set; }
            public int DaysSinceLastReply { get; set; }
            public int TotalSentInPeriod { get; set; }
            public int TotalRepliesInPeriod { get; set; }
            public List<EmailAccountNoReplyInfo> EmailAccounts { get; set; } = new();
        }
        
        public class EmailAccountNoReplyInfo
        {
            public long EmailAccountId { get; set; }
            public string EmailAddress { get; set; } = string.Empty;
            public DateTime? LastReplyDate { get; set; }
            public int DaysSinceLastReply { get; set; }
            public int SentInPeriod { get; set; }
            public int RepliesInPeriod { get; set; }
        }
        
        public class NoReplyThresholdDetails
        {
            public int DaysSinceLastReply { get; set; }
            public DateTime CheckDate { get; set; }
            public DateTime ThresholdDate { get; set; }
        }
    }
}