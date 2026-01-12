using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class CampaignStatsResponse
{
    [JsonProperty("total_stats")]
    public string TotalStats { get; set; }

    [JsonProperty("data")]
    public List<CampaignStat> Stats { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }

    [JsonProperty("limit")]
    public int Limit { get; set; }
}

public class CampaignStat
{
    [JsonProperty("lead_name")]
    public string LeadName { get; set; }

    [JsonProperty("lead_email")]
    public string LeadEmail { get; set; }

    [JsonProperty("lead_category")]
    public object LeadCategory { get; set; }

    [JsonProperty("sequence_number")]
    public int? SequenceNumber { get; set; }

    [JsonProperty("stats_id")]
    public string StatsId { get; set; }

    [JsonProperty("email_campaign_seq_id")]
    public int? EmailCampaignSeqId { get; set; }

    [JsonProperty("seq_variant_id")]
    public int? SeqVariantId { get; set; }

    [JsonProperty("email_subject")]
    public string EmailSubject { get; set; }

    [JsonProperty("email_message")]
    public string EmailMessage { get; set; }

    [JsonProperty("sent_time")]
    public DateTime SentTime { get; set; }

    [JsonProperty("open_time")]
    public DateTime? OpenTime { get; set; }

    [JsonProperty("click_time")]
    public DateTime? ClickTime { get; set; }

    [JsonProperty("reply_time")]
    public DateTime? ReplyTime { get; set; }

    [JsonProperty("open_count")]
    public int? OpenCount { get; set; }

    [JsonProperty("click_count")]
    public int? ClickCount { get; set; }

    [JsonProperty("is_unsubscribed")]
    public bool IsUnsubscribed { get; set; }

    [JsonProperty("is_bounced")]
    public bool IsBounced { get; set; }

    [JsonIgnore] 
    public DateTime FetchedAt { get; set; }
}