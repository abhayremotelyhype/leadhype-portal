using Newtonsoft.Json;

namespace LeadHype.Api;

public class SmartleadWarmupSettings
{
    [JsonProperty("maxEmailPerDay")]
    public int? MaxEmailPerDay { get; set; }

    [JsonProperty("isRampupEnabled")]
    public bool? IsRampupEnabled { get; set; }

    [JsonProperty("rampupValue")]
    public int? RampupValue { get; set; }

    [JsonProperty("warmupMinCount")]
    public int? WarmupMinCount { get; set; }

    [JsonProperty("warmupMaxCount")]
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
    
    [JsonProperty("emailAccountId")]
    public string? EmailAccountId { get; set; }

}