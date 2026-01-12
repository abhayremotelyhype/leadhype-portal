using System.ComponentModel;
using Newtonsoft.Json;

namespace LeadHype.Api;

public class WarmupSettings
{
    [JsonProperty("maxEmailPerDay"), DefaultValue(14)]
    public int? MaxEmailPerDay { get; set; }

    [JsonProperty("isRampupEnabled"), DefaultValue(false)]
    public bool? IsRampupEnabled { get; set; }

    [JsonProperty("rampupValue"), DefaultValue(10)]
    public int? RampupValue { get; set; }

    [JsonProperty("warmupMinCount"), DefaultValue(12)]
    public int? WarmupMinCount { get; set; }

    [JsonProperty("warmupMaxCount"), DefaultValue(15)]
    public int? WarmupMaxCount { get; set; }

    [JsonProperty("replyRate")]
    public int? ReplyRate { get; set; }

    [JsonProperty("dailyReplyLimit")]
    public int? DailyReplyLimit { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("autoAdjustWarmup")]
    public bool? AutoAdjustWarmup { get; set; }

    [JsonProperty("useCustomDomain")]
    public bool? UseCustomDomain { get; set; }

    [JsonProperty("sendWarmupsOnlyOnWeekdays")]
    public bool? SendWarmupsOnlyOnWeekdays { get; set; }

    [JsonProperty("warmupKeyId")]
    public string? WarmupKeyId { get; set; }

    // [JsonProperty("emailAccountId")]
    // public string? EmailAccountId { get; set; }
}