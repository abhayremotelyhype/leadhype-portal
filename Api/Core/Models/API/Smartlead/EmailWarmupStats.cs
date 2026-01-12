using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class EmailWarmupStats
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("sent_count")]
    public string SentCount { get; set; }

    [JsonProperty("spam_count")]
    public string SpamCount { get; set; }

    [JsonProperty("warmup_email_received_count")]
    public string WarmupEmailReceivedCount { get; set; }

    [JsonProperty("inbox_count")]
    public string InboxCount { get; set; }

    [JsonProperty("stats_by_date")]
    public List<WarmupStatsByDate>? StatsByDate { get; set; }
}

public class WarmupStatsByDate
{
    [JsonProperty("date")]
    public string Date { get; set; }

    [JsonProperty("sent_count")]
    public int SentCount { get; set; }

    [JsonProperty("reply_count")]
    public int ReplyCount { get; set; }

    [JsonProperty("save_from_spam_count")]
    public int SaveFromSpamCount { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }
}
