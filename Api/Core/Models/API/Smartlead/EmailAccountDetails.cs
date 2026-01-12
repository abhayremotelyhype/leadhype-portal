using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class EmailAccountDetails
{
    [JsonProperty("id")] public int Id { get; set; }

    [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }

    [JsonProperty("user_id")] public int UserId { get; set; }

    [JsonProperty("from_name")] public string FromName { get; set; }

    [JsonProperty("from_email")] public string FromEmail { get; set; }

    [JsonProperty("username")] public string Username { get; set; }

    [JsonProperty("password")] public object Password { get; set; }

    [JsonProperty("smtp_host")] public object SmtpHost { get; set; }

    [JsonProperty("smtp_port")] public object SmtpPort { get; set; }

    [JsonProperty("smtp_port_type")] public object SmtpPortType { get; set; }

    [JsonProperty("message_per_day")] public int MessagePerDay { get; set; }

    [JsonProperty("different_reply_to_address")]
    public string DifferentReplyToAddress { get; set; }

    [JsonProperty("is_different_imap_account")]
    public bool IsDifferentImapAccount { get; set; }

    [JsonProperty("imap_username")] public object ImapUsername { get; set; }

    [JsonProperty("imap_password")] public object ImapPassword { get; set; }

    [JsonProperty("imap_host")] public object ImapHost { get; set; }

    [JsonProperty("imap_port")] public object ImapPort { get; set; }

    [JsonProperty("imap_port_type")] public object ImapPortType { get; set; }

    [JsonProperty("signature")] public object Signature { get; set; }

    [JsonProperty("custom_tracking_domain")]
    public string CustomTrackingDomain { get; set; }

    [JsonProperty("bcc_email")] public object BccEmail { get; set; }

    [JsonProperty("is_smtp_success")] public bool IsSmtpSuccess { get; set; }

    [JsonProperty("is_imap_success")] public bool IsImapSuccess { get; set; }

    [JsonProperty("smtp_failure_error")] public string SmtpFailureError { get; set; }

    [JsonProperty("imap_failure_error")] public object ImapFailureError { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("daily_sent_count")] public int DailySentCount { get; set; }

    [JsonProperty("client_id")] public int? ClientId { get; set; }

    [JsonProperty("campaign_count")] public int CampaignCount { get; set; }

    [JsonProperty("warmup_details")] public WarmupDetails WarmupDetails { get; set; }
}

public class WarmupDetails
{
    [JsonProperty("status")] public string Status { get; set; }

    [JsonProperty("total_sent_count")] public int TotalSentCount { get; set; }

    [JsonProperty("total_spam_count")] public int TotalSpamCount { get; set; }

    [JsonProperty("warmup_reputation")] public string WarmupReputation { get; set; }

    [JsonProperty("warmup_key_id")] public string WarmupKeyId { get; set; }

    [JsonProperty("warmup_created_at")] public DateTime WarmupCreatedAt { get; set; }

    [JsonProperty("reply_rate")] public int ReplyRate { get; set; }

    [JsonProperty("blocked_reason")] public string BlockedReason { get; set; }
}