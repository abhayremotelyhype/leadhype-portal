using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Requests
{
    public class CreateCampaignV1Request
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public List<string> Tracking { get; set; } = new List<string>();
        
        public string LeadStopCondition { get; set; } = "ON_EMAIL_REPLY";
        
        public string UnsubscribeMessage { get; set; } = string.Empty;
        
        public bool PlainTextOnly { get; set; } = false;
        
        public bool ForcePlainFormat { get; set; } = false;
        
        public bool SmartProviderMatching { get; set; } = true;
        
        public int FollowupRatio { get; set; } = 80;
        
        public string? CustomerRef { get; set; }
        
        public bool IncludeUnsubscribeMarker { get; set; } = true;
        
        public bool PauseDomainOnReply { get; set; } = true;
        
        public bool BypassSharedLimit { get; set; } = false;
        
        public string BounceLimit { get; set; } = "5";
        
        public OutOfOfficeRules OutOfOfficeRules { get; set; } = new OutOfOfficeRules();
        
        public List<int> AiSortingOptions { get; set; } = new List<int>();
        
        // Callback URL for campaign creation notification (required)
        [Required]
        public string CallbackUrl { get; set; } = string.Empty;
        
        // Timing configuration (optional)
        public TimingConfiguration? TimingSettings { get; set; }
    }

    public class TimingConfiguration
    {
        public string Region { get; set; } = "America/Los_Angeles";
        
        public List<int> ActiveDays { get; set; } = new List<int>();
        
        public string StartTime { get; set; } = "09:00";
        
        public string EndTime { get; set; } = "18:00";
        
        public int IntervalMinutes { get; set; } = 10;
        
        public int DailyLimit { get; set; } = 20;
        
        public DateTime? ActivationTime { get; set; }
    }

    public class OutOfOfficeRules
    {
        public bool CountOOOasReply { get; set; } = false;
        
        public bool AutoResumeOOO { get; set; } = false;
        
        public int? ResumeOOOafterDelay { get; set; }
        
        public bool TagOOOautomatically { get; set; } = false;
    }
}