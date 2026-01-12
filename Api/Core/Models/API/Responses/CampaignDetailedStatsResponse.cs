using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for detailed campaign statistics
    /// </summary>
    public class CampaignDetailedStatsResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Campaign statistics data
        /// </summary>
        public CampaignStatsData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Campaign statistics retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Campaign statistics data
    /// </summary>
    public class CampaignStatsData
    {
        /// <summary>
        /// Campaign information
        /// </summary>
        public CampaignStatsInfo Campaign { get; set; } = new();
        
        /// <summary>
        /// Time range for the statistics
        /// </summary>
        public CampaignStatsTimeRange TimeRange { get; set; } = new();
        
        /// <summary>
        /// Summary statistics for the entire period
        /// </summary>
        public CampaignStatsSummary Summary { get; set; } = new();
        
        /// <summary>
        /// Daily breakdown of statistics
        /// </summary>
        public List<CampaignDailyStats> DailyStats { get; set; } = new();
    }

    /// <summary>
    /// Campaign information
    /// </summary>
    public class CampaignStatsInfo
    {
        /// <summary>
        /// Internal campaign identifier
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// External campaign ID
        /// </summary>
        /// <example>12345</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Campaign name
        /// </summary>
        /// <example>Q4 Product Launch Campaign</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Campaign status
        /// </summary>
        /// <example>Active</example>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Time range for statistics
    /// </summary>
    public class CampaignStatsTimeRange
    {
        /// <summary>
        /// Start date of the statistics period
        /// </summary>
        /// <example>2024-01-01</example>
        public string StartDate { get; set; } = string.Empty;
        
        /// <summary>
        /// End date of the statistics period
        /// </summary>
        /// <example>2024-01-31</example>
        public string EndDate { get; set; } = string.Empty;
        
        /// <summary>
        /// Number of days in the period
        /// </summary>
        /// <example>31</example>
        public int Days { get; set; }
    }

    /// <summary>
    /// Summary statistics for the entire period
    /// </summary>
    public class CampaignStatsSummary
    {
        /// <summary>
        /// Total emails sent
        /// </summary>
        /// <example>8750</example>
        public int TotalSent { get; set; }
        
        /// <summary>
        /// Total emails opened
        /// </summary>
        /// <example>3200</example>
        public int TotalOpened { get; set; }
        
        /// <summary>
        /// Total email clicks
        /// </summary>
        /// <example>680</example>
        public int TotalClicked { get; set; }
        
        /// <summary>
        /// Total replies received
        /// </summary>
        /// <example>240</example>
        public int TotalReplied { get; set; }
        
        /// <summary>
        /// Total positive replies
        /// </summary>
        /// <example>180</example>
        public int TotalPositiveReplies { get; set; }
        
        /// <summary>
        /// Total emails bounced
        /// </summary>
        /// <example>125</example>
        public int TotalBounced { get; set; }
        
        /// <summary>
        /// Open rate percentage
        /// </summary>
        /// <example>36.57</example>
        public decimal OpenRate { get; set; }
        
        /// <summary>
        /// Click rate percentage
        /// </summary>
        /// <example>7.77</example>
        public decimal ClickRate { get; set; }
        
        /// <summary>
        /// Reply rate percentage
        /// </summary>
        /// <example>2.74</example>
        public decimal ReplyRate { get; set; }
        
        /// <summary>
        /// Bounce rate percentage
        /// </summary>
        /// <example>1.43</example>
        public decimal BounceRate { get; set; }
        
        /// <summary>
        /// Positive reply rate percentage (positive replies / total replies)
        /// </summary>
        /// <example>75.00</example>
        public decimal PositiveReplyRate { get; set; }
    }

    /// <summary>
    /// Daily statistics breakdown
    /// </summary>
    public class CampaignDailyStats
    {
        /// <summary>
        /// Date in YYYY-MM-DD format
        /// </summary>
        /// <example>2024-01-01</example>
        public string Date { get; set; } = string.Empty;
        
        /// <summary>
        /// Day of week
        /// </summary>
        /// <example>Monday</example>
        public string DayOfWeek { get; set; } = string.Empty;
        
        /// <summary>
        /// Emails sent on this day
        /// </summary>
        /// <example>150</example>
        public int Sent { get; set; }
        
        /// <summary>
        /// Emails opened on this day
        /// </summary>
        /// <example>65</example>
        public int Opened { get; set; }
        
        /// <summary>
        /// Email clicks on this day
        /// </summary>
        /// <example>12</example>
        public int Clicked { get; set; }
        
        /// <summary>
        /// Replies received on this day
        /// </summary>
        /// <example>4</example>
        public int Replied { get; set; }
        
        /// <summary>
        /// Positive replies on this day
        /// </summary>
        /// <example>3</example>
        public int PositiveReplies { get; set; }
        
        /// <summary>
        /// Emails bounced on this day
        /// </summary>
        /// <example>2</example>
        public int Bounced { get; set; }
        
        /// <summary>
        /// Open rate for this day
        /// </summary>
        /// <example>43.33</example>
        public decimal OpenRate { get; set; }
        
        /// <summary>
        /// Click rate for this day
        /// </summary>
        /// <example>8.00</example>
        public decimal ClickRate { get; set; }
        
        /// <summary>
        /// Reply rate for this day
        /// </summary>
        /// <example>2.67</example>
        public decimal ReplyRate { get; set; }
        
        /// <summary>
        /// Bounce rate for this day
        /// </summary>
        /// <example>1.33</example>
        public decimal BounceRate { get; set; }
    }
}