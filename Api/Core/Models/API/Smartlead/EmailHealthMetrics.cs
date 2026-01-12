using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class EmailHealthMetricsResponse
{
    [JsonProperty("success")]
    public bool? Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public EmailHealhtMetricsData Data { get; set; }
 
}

public class EmailHealhtMetricsData
{
    [JsonProperty("email_health_metrics")]
    public List<EmailHealthMetric> EmailHealthMetrics { get; set; }
}

public class EmailHealthMetric
{
    [JsonProperty("from_email")]
    public string FromEmail { get; set; }

    [JsonProperty("sent")]
    public int Sent { get; set; }

    [JsonProperty("opened")]
    public int Opened { get; set; }

    [JsonProperty("replied")]
    public int Replied { get; set; }

    [JsonProperty("positive_replied")]
    public int PositiveReplied { get; set; }

    [JsonProperty("bounced")]
    public int Bounced { get; set; }

    [JsonProperty("unique_lead_count")]
    public int UniqueLeadCount { get; set; }

    [JsonProperty("unique_open_count")]
    public int UniqueOpenCount { get; set; }

    [JsonProperty("open_rate")]
    public string OpenRate { get; set; }

    [JsonProperty("reply_rate")]
    public string ReplyRate { get; set; }

    [JsonProperty("positive_reply_rate")]
    public string PositiveReplyRate { get; set; }

    [JsonProperty("bounce_rate")]
    public string BounceRate { get; set; }

    
    // [JsonProperty("from_email")]
    // public string FromEmail { get; set; }
    //
    // [JsonProperty("sent")]
    // public int Sent { get; set; }
    //
    // [JsonProperty("opened")]
    // public int Opened { get; set; }
    //
    // [JsonProperty("clicked")]
    // public int Clicked { get; set; }
    //
    // [JsonProperty("replied")]
    // public int Replied { get; set; }
    //
    // [JsonProperty("unsubscribed")]
    // public int Unsubscribed { get; set; }
    //
    // [JsonProperty("bounced")]
    // public int Bounced { get; set; }
    //
    
    [JsonIgnore] 
    public DateTime DateTime { get; set; }
}