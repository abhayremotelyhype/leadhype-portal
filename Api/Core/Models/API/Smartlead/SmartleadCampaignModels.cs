using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models.API.Smartlead
{
    public class SmartleadCreateCampaignRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("client_id")]
        public int? ClientId { get; set; }
    }

    public class SmartleadCreateCampaignResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("client_id")]
        public int? ClientId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class SmartleadUpdateCampaignSettingsRequest
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        
        [JsonProperty("track_settings")]
        public List<string> TrackSettings { get; set; } = new List<string>();
        
        [JsonProperty("stop_lead_settings")]
        public string StopLeadSettings { get; set; } = string.Empty;
        
        [JsonProperty("unsubscribe_text")]
        public string UnsubscribeText { get; set; } = string.Empty;
        
        [JsonProperty("send_as_plain_text")]
        public bool SendAsPlainText { get; set; } = false;
        
        [JsonProperty("force_plain_text")]
        public bool ForcePlainText { get; set; } = false;
        
        [JsonProperty("enable_ai_esp_matching")]
        public bool EnableAiEspMatching { get; set; } = true;
        
        [JsonProperty("follow_up_percentage")]
        public int FollowUpPercentage { get; set; } = 80;
        
        [JsonProperty("client_id")]
        public int? ClientId { get; set; }
        
        [JsonProperty("add_unsubscribe_tag")]
        public bool AddUnsubscribeTag { get; set; } = true;
        
        [JsonProperty("auto_pause_domain_leads_on_reply")]
        public bool AutoPauseDomainLeadsOnReply { get; set; } = true;
        
        [JsonProperty("ignore_ss_mailbox_sending_limit")]
        public bool IgnoreSsMailboxSendingLimit { get; set; } = false;
        
        [JsonProperty("bounce_autopause_threshold")]
        public string BounceAutopauseThreshold { get; set; } = "5";
        
        [JsonProperty("out_of_office_detection_settings")]
        public SmartleadOutOfOfficeSettings OutOfOfficeDetectionSettings { get; set; } = new SmartleadOutOfOfficeSettings();
        
        [JsonProperty("ai_categorisation_options")]
        public List<int> AiCategorisationOptions { get; set; } = new List<int>();
    }

    public class SmartleadOutOfOfficeSettings
    {
        [JsonProperty("ignoreOOOasReply")]
        public bool IgnoreOOOasReply { get; set; } = false;
        
        [JsonProperty("autoReactivateOOO")]
        public bool AutoReactivateOOO { get; set; } = false;
        
        [JsonProperty("reactivateOOOwithDelay")]
        public int? ReactivateOOOwithDelay { get; set; }
        
        [JsonProperty("autoCategorizeOOO")]
        public bool AutoCategorizeOOO { get; set; } = false;
    }

    public class SmartleadUpdateCampaignSettingsResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class SmartleadScheduleCampaignRequest
    {
        [JsonProperty("timezone")]
        public string Timezone { get; set; } = string.Empty;
        
        [JsonProperty("days_of_the_week")]
        public List<int> DaysOfTheWeek { get; set; } = new List<int>();
        
        [JsonProperty("start_hour")]
        public string StartHour { get; set; } = string.Empty;
        
        [JsonProperty("end_hour")]
        public string EndHour { get; set; } = string.Empty;
        
        [JsonProperty("min_time_btw_emails")]
        public int MinTimeBtwEmails { get; set; }
        
        [JsonProperty("max_new_leads_per_day")]
        public int MaxNewLeadsPerDay { get; set; }
        
        [JsonProperty("schedule_start_time")]
        public DateTime? ScheduleStartTime { get; set; }
    }

    public class SmartleadScheduleCampaignResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class SmartleadSequencesRequest
    {
        [JsonProperty("sequences")]
        public List<SmartleadSequence> Sequences { get; set; } = new List<SmartleadSequence>();
    }

    public class SmartleadSequence
    {
        [JsonProperty("id")]
        public int? Id { get; set; }
        
        [JsonProperty("seq_number")]
        public int SeqNumber { get; set; }
        
        [JsonProperty("seq_type")]
        public string SeqType { get; set; } = "EMAIL";
        
        [JsonProperty("seq_delay_details")]
        public SmartleadSeqDelayDetails SeqDelayDetails { get; set; } = new SmartleadSeqDelayDetails();
        
        [JsonProperty("variant_distribution_type")]
        public string? VariantDistributionType { get; set; }
        
        [JsonProperty("lead_distribution_percentage")]
        public int? LeadDistributionPercentage { get; set; }
        
        [JsonProperty("winning_metric_property")]
        public string? WinningMetricProperty { get; set; }
        
        [JsonProperty("seq_variants")]
        public List<SmartleadSeqVariant>? SeqVariants { get; set; }
        
        // For simple follow-ups without variants
        [JsonProperty("subject")]
        public string? Subject { get; set; }
        
        [JsonProperty("email_body")]
        public string? EmailBody { get; set; }
    }

    public class SmartleadSeqDelayDetails
    {
        [JsonProperty("delay_in_days")]
        public int DelayInDays { get; set; } = 1;
    }

    public class SmartleadSeqVariant
    {
        [JsonProperty("id")]
        public int? Id { get; set; }
        
        [JsonProperty("subject")]
        public string Subject { get; set; } = string.Empty;
        
        [JsonProperty("email_body")]
        public string EmailBody { get; set; } = string.Empty;
        
        [JsonProperty("variant_label")]
        public string VariantLabel { get; set; } = string.Empty;
        
        [JsonProperty("variant_distribution_percentage")]
        public int VariantDistributionPercentage { get; set; } = 0;
    }

    public class SmartleadSequencesResponse
    {
        [JsonProperty("ok")]
        public bool Success { get; set; }
        
        [JsonProperty("data")]
        public string Message { get; set; } = string.Empty;
    }

    public class SmartleadUploadLeadsResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string? Message { get; set; }
        
        [JsonProperty("uploaded_count")]
        public int UploadedCount { get; set; }
        
        [JsonProperty("lead_ids")]
        public Dictionary<string, string>? LeadIds { get; set; }
    }

    public class SmartleadInboxRepliesResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        
        [JsonProperty("data")]
        public List<SmartleadInboxReply>? Data { get; set; }
    }

    public class SmartleadInboxReply
    {
        [JsonProperty("lead_category_id")]
        public int LeadCategoryId { get; set; }
        
        [JsonProperty("last_sent_time")]
        public DateTime? LastSentTime { get; set; }
        
        [JsonProperty("last_reply_time")]
        public DateTime? LastReplyTime { get; set; }
        
        [JsonProperty("has_new_unread_email")]
        public bool HasNewUnreadEmail { get; set; }
        
        [JsonProperty("email_account_id")]
        public int EmailAccountId { get; set; }
        
        [JsonProperty("revenue")]
        public string Revenue { get; set; } = "0.00";
        
        [JsonProperty("is_pushed_to_sub_sequence")]
        public bool IsPushedToSubSequence { get; set; }
        
        [JsonProperty("lead_first_name")]
        public string? LeadFirstName { get; set; }
        
        [JsonProperty("lead_last_name")]
        public string? LeadLastName { get; set; }
        
        [JsonProperty("lead_email")]
        public string? LeadEmail { get; set; }
        
        [JsonProperty("email_lead_id")]
        public string? EmailLeadId { get; set; }
        
        [JsonProperty("email_lead_map_id")]
        public string? EmailLeadMapId { get; set; }
        
        [JsonProperty("lead_status")]
        public string? LeadStatus { get; set; }
        
        [JsonProperty("current_sequence_number")]
        public int CurrentSequenceNumber { get; set; }
        
        [JsonProperty("sub_sequence_id")]
        public object? SubSequenceId { get; set; }
        
        [JsonProperty("old_replaced_lead_data")]
        public object? OldReplacedLeadData { get; set; }
        
        [JsonProperty("lead_next_timestamp_to_send")]
        public DateTime? LeadNextTimestampToSend { get; set; }
        
        [JsonProperty("email_campaign_seq_id")]
        public object? EmailCampaignSeqId { get; set; }
        
        [JsonProperty("is_important")]
        public bool IsImportant { get; set; }
        
        [JsonProperty("is_archived")]
        public bool IsArchived { get; set; }
        
        [JsonProperty("is_snoozed")]
        public bool IsSnoozed { get; set; }
        
        [JsonProperty("team_member_id")]
        public object? TeamMemberId { get; set; }
        
        [JsonProperty("is_ooo_automated_push_lead")]
        public object? IsOooAutomatedPushLead { get; set; }
        
        [JsonProperty("email_campaign_id")]
        public int EmailCampaignId { get; set; }
        
        [JsonProperty("email_campaign_name")]
        public string? EmailCampaignName { get; set; }
        
        [JsonProperty("client_id")]
        public int ClientId { get; set; }
        
        [JsonProperty("belongs_to_sub_sequence")]
        public bool BelongsToSubSequence { get; set; }
        
        [JsonProperty("campaign_sending_schedule")]
        public SmartleadCampaignSendingSchedule? CampaignSendingSchedule { get; set; }
        
        [JsonProperty("email_history")]
        public List<SmartleadEmailHistory>? EmailHistory { get; set; }
    }

    public class SmartleadCampaignSendingSchedule
    {
        [JsonProperty("tz")]
        public string? Timezone { get; set; }
        
        [JsonProperty("days")]
        public List<int>? Days { get; set; }
        
        [JsonProperty("endHour")]
        public string? EndHour { get; set; }
        
        [JsonProperty("startHour")]
        public string? StartHour { get; set; }
    }

    public class SmartleadEmailHistory
    {
        [JsonProperty("stats_id")]
        public string? StatsId { get; set; }
        
        [JsonProperty("from")]
        public string? From { get; set; }
        
        [JsonProperty("to")]
        public string? To { get; set; }
        
        [JsonProperty("type")]
        public string? Type { get; set; }
        
        [JsonProperty("message_id")]
        public string? MessageId { get; set; }
        
        [JsonProperty("time")]
        public DateTime? Time { get; set; }
        
        [JsonProperty("email_body")]
        public string? EmailBody { get; set; }
        
        [JsonProperty("subject")]
        public string? Subject { get; set; }
        
        [JsonProperty("email_seq_number")]
        public string? EmailSeqNumber { get; set; }
        
        [JsonProperty("open_count")]
        public int OpenCount { get; set; }
        
        [JsonProperty("click_count")]
        public int ClickCount { get; set; }
        
        [JsonProperty("click_details")]
        public object? ClickDetails { get; set; }
        
        [JsonProperty("cc")]
        public List<string>? Cc { get; set; }
    }
}