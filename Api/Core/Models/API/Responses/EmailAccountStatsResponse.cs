using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for email account statistics with daily breakdown
    /// </summary>
    public class EmailAccountStatsResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Email account statistics data with daily breakdown
        /// </summary>
        public EmailAccountStatsData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Email account statistics retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Email account statistics data with comprehensive metrics
    /// </summary>
    public class EmailAccountStatsData
    {
        /// <summary>
        /// Basic email account information
        /// </summary>
        public EmailAccountStatsInfo EmailAccount { get; set; } = new();
        
        /// <summary>
        /// Time range for the statistics
        /// </summary>
        public EmailAccountStatsTimeRange TimeRange { get; set; } = new();
        
        /// <summary>
        /// Summary statistics for the entire period
        /// </summary>
        public EmailAccountStatsSummary Summary { get; set; } = new();
        
        /// <summary>
        /// Daily breakdown of email statistics
        /// </summary>
        public List<EmailAccountDailyStats> DailyStats { get; set; } = new();
    }

    /// <summary>
    /// Basic email account information for statistics
    /// </summary>
    public class EmailAccountStatsInfo
    {
        /// <summary>
        /// Email account ID
        /// </summary>
        /// <example>1001</example>
        public long Id { get; set; }
        
        /// <summary>
        /// Email address
        /// </summary>
        /// <example>sender@company.com</example>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name for the email account
        /// </summary>
        /// <example>Jane Sender</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the email account
        /// </summary>
        /// <example>Active</example>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Time range for email account statistics
    /// </summary>
    public class EmailAccountStatsTimeRange
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
    /// Summary statistics for the email account over the entire period
    /// </summary>
    public class EmailAccountStatsSummary
    {
        /// <summary>
        /// Total emails sent from this account
        /// </summary>
        /// <example>2500</example>
        public int TotalSent { get; set; }
        
        /// <summary>
        /// Total emails opened
        /// </summary>
        /// <example>950</example>
        public int TotalOpened { get; set; }
        
        /// <summary>
        /// Total replies received
        /// </summary>
        /// <example>120</example>
        public int TotalReplied { get; set; }
        
        /// <summary>
        /// Total emails bounced
        /// </summary>
        /// <example>35</example>
        public int TotalBounced { get; set; }
        
        /// <summary>
        /// Overall open rate percentage
        /// </summary>
        /// <example>38.00</example>
        public decimal OpenRate { get; set; }
        
        /// <summary>
        /// Overall reply rate percentage
        /// </summary>
        /// <example>4.80</example>
        public decimal ReplyRate { get; set; }
        
        /// <summary>
        /// Overall bounce rate percentage
        /// </summary>
        /// <example>1.40</example>
        public decimal BounceRate { get; set; }
    }

    /// <summary>
    /// Daily statistics breakdown for email account
    /// </summary>
    public class EmailAccountDailyStats
    {
        /// <summary>
        /// Date in YYYY-MM-DD format
        /// </summary>
        /// <example>2024-01-15</example>
        public string Date { get; set; } = string.Empty;
        
        /// <summary>
        /// Day of the week
        /// </summary>
        /// <example>Monday</example>
        public string DayOfWeek { get; set; } = string.Empty;
        
        /// <summary>
        /// Emails sent on this day
        /// </summary>
        /// <example>85</example>
        public int Sent { get; set; }
        
        /// <summary>
        /// Emails opened on this day
        /// </summary>
        /// <example>32</example>
        public int Opened { get; set; }
        
        /// <summary>
        /// Replies received on this day
        /// </summary>
        /// <example>4</example>
        public int Replied { get; set; }
        
        /// <summary>
        /// Emails bounced on this day
        /// </summary>
        /// <example>1</example>
        public int Bounced { get; set; }
        
        /// <summary>
        /// Open rate for this day
        /// </summary>
        /// <example>37.65</example>
        public decimal OpenRate { get; set; }
        
        /// <summary>
        /// Reply rate for this day
        /// </summary>
        /// <example>4.71</example>
        public decimal ReplyRate { get; set; }
        
        /// <summary>
        /// Bounce rate for this day
        /// </summary>
        /// <example>1.18</example>
        public decimal BounceRate { get; set; }
    }
}