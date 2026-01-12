namespace LeadHype.Api.Core.Models.Frontend
{
    public class EmailAccountSummaryDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientColor { get; set; }

        // Warmup stats
        public int WarmupSent { get; set; }
        public int WarmupReplied { get; set; }
        public int WarmupSavedFromSpam { get; set; }
        
        // Email statistics (totals only, no daily breakdown)
        public int Sent { get; set; }
        public int Opened { get; set; }
        public int Clicked { get; set; }
        public int Replied { get; set; }
        public int PositiveReplies { get; set; } // Count of positive classified replies from RevReply API
        public int Unsubscribed { get; set; }
        public int Bounced { get; set; }

        // Tags support
        public List<string> Tags { get; set; } = new();
        
        // Campaign association count
        public int CampaignCount { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}