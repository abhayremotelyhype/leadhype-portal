using System.ComponentModel;

namespace LeadHype.Api.Core.Models
{
    /// <summary>
    /// Enumeration of available webhook event types
    /// </summary>
    public enum WebhookEventType
    {
        /// <summary>
        /// Triggered when campaign reply rate drops below specified threshold
        /// </summary>
        [Description("Reply Rate Drop")]
        ReplyRateDrop,

        /// <summary>
        /// Triggered when campaign bounce rate exceeds specified threshold
        /// </summary>
        [Description("High Bounce Rate")]
        BounceRateHigh,

        /// <summary>
        /// Triggered when a new campaign is created by an admin user
        /// </summary>
        [Description("Campaign Created")]
        CampaignCreated,

        /// <summary>
        /// Triggered when there is no positive reply for X days
        /// </summary>
        [Description("No Positive Reply for X Days")]
        NoPositiveReplyForXDays,

        /// <summary>
        /// Triggered when there is no reply (positive or negative) for X days
        /// </summary>
        [Description("No Reply for X Days")]
        NoReplyForXDays
    }

    /// <summary>
    /// Extension methods for WebhookEventType enum
    /// </summary>
    public static class WebhookEventTypeExtensions
    {
        /// <summary>
        /// Convert enum to string representation used in webhook events
        /// </summary>
        public static string ToEventString(this WebhookEventType eventType)
        {
            return eventType switch
            {
                WebhookEventType.ReplyRateDrop => "reply_rate_drop",
                WebhookEventType.BounceRateHigh => "bounce_rate_high",
                WebhookEventType.CampaignCreated => "campaign.created",
                WebhookEventType.NoPositiveReplyForXDays => "no_positive_reply_for_x_days",
                WebhookEventType.NoReplyForXDays => "no_reply_for_x_days",
                _ => throw new ArgumentException($"Unknown webhook event type: {eventType}")
            };
        }

        /// <summary>
        /// Convert string representation to enum
        /// </summary>
        public static WebhookEventType FromEventString(string eventString)
        {
            return eventString switch
            {
                "reply_rate_drop" => WebhookEventType.ReplyRateDrop,
                "bounce_rate_high" => WebhookEventType.BounceRateHigh,
                "campaign.created" => WebhookEventType.CampaignCreated,
                "no_positive_reply_for_x_days" => WebhookEventType.NoPositiveReplyForXDays,
                "no_reply_for_x_days" => WebhookEventType.NoReplyForXDays,
                _ => throw new ArgumentException($"Unknown webhook event string: {eventString}")
            };
        }

        /// <summary>
        /// Get all available event types with their metadata
        /// </summary>
        public static IEnumerable<WebhookEventTypeInfo> GetAllEventTypes()
        {
            return new[]
            {
                new WebhookEventTypeInfo
                {
                    Type = WebhookEventType.ReplyRateDrop.ToEventString(),
                    Name = "Reply Rate Drop",
                    Description = "Triggers when campaign reply rate drops below specified threshold over monitoring period",
                    RequiredParameters = new[]
                    {
                        new WebhookEventParameter { Name = "thresholdPercent", Type = "number", Description = "Minimum reply rate drop percentage to trigger (0-100)" },
                        new WebhookEventParameter { Name = "monitoringPeriodDays", Type = "number", Description = "Number of days to monitor (1-30)" }
                    }
                },
                new WebhookEventTypeInfo
                {
                    Type = WebhookEventType.BounceRateHigh.ToEventString(),
                    Name = "High Bounce Rate",
                    Description = "Triggers when campaign bounce rate exceeds specified threshold over monitoring period",
                    RequiredParameters = new[]
                    {
                        new WebhookEventParameter { Name = "thresholdPercent", Type = "number", Description = "Maximum bounce rate percentage to allow (0-100)" },
                        new WebhookEventParameter { Name = "monitoringPeriodDays", Type = "number", Description = "Number of days to monitor (1-30)" }
                    }
                },
                new WebhookEventTypeInfo
                {
                    Type = WebhookEventType.CampaignCreated.ToEventString(),
                    Name = "Campaign Created",
                    Description = "Triggers when a new campaign is created by an admin user",
                    RequiredParameters = Array.Empty<WebhookEventParameter>()
                },
                new WebhookEventTypeInfo
                {
                    Type = WebhookEventType.NoReplyForXDays.ToEventString(),
                    Name = "No Reply for X Days",
                    Description = "Triggers when there is no reply (positive or negative) received for X days for the selected clients or campaigns",
                    RequiredParameters = new[]
                    {
                        new WebhookEventParameter { Name = "daysSinceLastReply", Type = "number", Description = "Number of days since last reply (1-365)" }
                    }
                },
                new WebhookEventTypeInfo
                {
                    Type = WebhookEventType.NoPositiveReplyForXDays.ToEventString(),
                    Name = "No Positive Reply for X Days",
                    Description = "Triggers when there is no positive reply received for X days for the selected clients or campaigns",
                    RequiredParameters = new[]
                    {
                        new WebhookEventParameter { Name = "daysSinceLastReply", Type = "number", Description = "Number of days since last positive reply (1-365)" }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Webhook event type information for API responses
    /// </summary>
    public class WebhookEventTypeInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WebhookEventParameter[] RequiredParameters { get; set; } = Array.Empty<WebhookEventParameter>();
    }

    /// <summary>
    /// Webhook event parameter information
    /// </summary>
    public class WebhookEventParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}