using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class CampaignSummary
{
    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("user_id")]
    public int? UserId { get; set; }

    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("track_settings")]
    public List<string>? TrackSettings { get; set; }

    [JsonProperty("scheduler_cron_value")]
    public SchedulerCronValue? SchedulerCronValue { get; set; }

    [JsonProperty("min_time_btwn_emails")]
    public int? MinTimeBtwnEmails { get; set; }

    [JsonProperty("max_leads_per_day")]
    public int? MaxLeadsPerDay { get; set; }

    [JsonProperty("stop_lead_settings")]
    public string? StopLeadSettings { get; set; }

    [JsonProperty("enable_ai_esp_matching")]
    public bool? EnableAiEspMatching { get; set; }

    [JsonProperty("send_as_plain_text")]
    public bool? SendAsPlainText { get; set; }

    [JsonProperty("follow_up_percentage")]
    public int? FollowUpPercentage { get; set; }

    [JsonProperty("unsubscribe_text")]
    public string? UnsubscribeText { get; set; }

    [JsonProperty("parent_campaign_id")]
    public object? ParentCampaignId { get; set; }

    [JsonProperty("client_id")]
    public int? ClientId { get; set; }

}

public class SchedulerCronValue
{
    [JsonProperty("tz")]
    public string? Tz { get; set; }

    [JsonProperty("days")]
    public List<int?>? Days { get; set; }

    [JsonProperty("endHour")]
    public string? EndHour { get; set; }

    [JsonProperty("startHour")]
    public string? StartHour { get; set; }
}