using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class PositiveReplyDaybyDayStats
{
    [JsonProperty("success")]
    public bool? Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public Data Data { get; set; }
}

public class Data
{
    [JsonProperty("day_wise_stats")]
    public List<DayWiseStat> DayWiseStats { get; set; }
}

public class DayWiseStat
{
    [JsonProperty("date")]
    public string Date { get; set; }

    [JsonProperty("day_name")]
    public string DayName { get; set; }

    [JsonProperty("email_engagement_metrics")]
    public EmailEngagementMetrics EmailEngagementMetrics { get; set; }
}

public class EmailEngagementMetrics
{
    [JsonProperty("positive_replied")]
    public object PositiveReplied { get; set; }
}

