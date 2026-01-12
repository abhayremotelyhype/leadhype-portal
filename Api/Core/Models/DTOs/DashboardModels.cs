namespace LeadHype.Api.Core.Models.Frontend
{
    public class DashboardOverview
    {
        public OverviewStats Stats { get; set; } = new();
        public List<CampaignPerformanceMetric> TopCampaigns { get; set; } = new();
        public List<ClientPerformanceMetric> TopClients { get; set; } = new();
        public List<TimeSeriesDataPoint> PerformanceTrend { get; set; } = new();
        public EmailAccountSummary EmailAccountSummary { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class OverviewStats
    {
        // Counts
        public int TotalEmailAccounts { get; set; }
        public int TotalCampaigns { get; set; }
        public int TotalClients { get; set; }
        public int TotalUsers { get; set; }
        
        // Campaign Performance (All Time)
        public int TotalEmailsSent { get; set; }
        public int TotalEmailsOpened { get; set; }
        public int TotalEmailsReplied { get; set; }
        public int TotalEmailsBounced { get; set; }
        public int TotalEmailsClicked { get; set; }
        
        // Calculated Rates
        public double OpenRate { get; set; }
        public double ReplyRate { get; set; }
        public double BounceRate { get; set; }
        public double ClickRate { get; set; }
        
        // Period Comparisons (vs previous period)
        public double TotalEmailsSentChange { get; set; }
        public double OpenRateChange { get; set; }
        public double ReplyRateChange { get; set; }
        public double BounceRateChange { get; set; }
        
        // Recent Activity (Last 7 days)
        public int RecentEmailsSent { get; set; }
        public int RecentEmailsOpened { get; set; }
        public int RecentEmailsReplied { get; set; }
        
        // Active Campaigns
        public int ActiveCampaigns { get; set; }
        public int PausedCampaigns { get; set; }
        public int CompletedCampaigns { get; set; }
        
        // Positive Replies
        public int TotalPositiveReplies { get; set; }
        public double PositiveReplyRate { get; set; }
        public double PositiveReplyRateChange { get; set; }
    }

    public class CampaignPerformanceMetric
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalSent { get; set; }
        public int TotalOpened { get; set; }
        public int TotalReplied { get; set; }
        public int TotalBounced { get; set; }
        public int TotalPositiveReplies { get; set; }
        public double OpenRate { get; set; }
        public double ReplyRate { get; set; }
        public double BounceRate { get; set; }
        public double PositiveReplyRate { get; set; }
        public DateTime LastActivity { get; set; }
        public int DaysActive { get; set; }
        public double CompositeScore { get; set; }
    }

    public class ClientPerformanceMetric
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CampaignCount { get; set; }
        public int EmailAccountCount { get; set; }
        public int TotalSent { get; set; }
        public int TotalOpened { get; set; }
        public int TotalReplied { get; set; }
        public double OpenRate { get; set; }
        public double ReplyRate { get; set; }
        public DateTime LastActivity { get; set; }
        public string Color { get; set; } = "#3B82F6";
    }

    public class TimeSeriesDataPoint
    {
        public DateTime Date { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsOpened { get; set; }
        public int EmailsReplied { get; set; }
        public int EmailsBounced { get; set; }
        public double OpenRate { get; set; }
        public double ReplyRate { get; set; }
    }

    public class EmailAccountSummary
    {
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int WarmingUpAccounts { get; set; }
        public int WarmedUpAccounts { get; set; }
        public int PausedAccounts { get; set; }
        public int IssueAccounts { get; set; }
        
        
        // By Provider
        public Dictionary<string, int> AccountsByProvider { get; set; } = new();
        public Dictionary<string, AccountStatusCount> AccountsByStatus { get; set; } = new();
    }

    public class AccountStatusCount
    {
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class RecentActivity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty; // "campaign", "email_account", "client"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    // Request models for filtering
    public class DashboardFilterRequest
    {
        public List<string> ClientIds { get; set; } = new();
        public List<string> CampaignIds { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Period { get; set; } = "30"; // "7", "30", "90", "365", "all"
        public bool AllCampaigns { get; set; } = false; // Admin-only: view all campaigns system-wide
    }

    public class CampaignAnalyticsRequest
    {
        public List<string> CampaignIds { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string GroupBy { get; set; } = "day"; // "hour", "day", "week", "month"
    }

    // User Statistics Models
    public class UserStatsRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? RoleFilter { get; set; } // "All", "Admin", "User"
        public string? StatusFilter { get; set; } // "All", "Active", "Inactive"
        public string? SearchQuery { get; set; }
        public string? SortBy { get; set; } = "CreatedAt"; // "Username", "Email", "CreatedAt", "LastLoginAt", "Role"
        public bool SortDescending { get; set; } = true;
    }

    public class UserStatsResponse
    {
        public List<UserStatsItem> Users { get; set; } = new();
        public UserStatsSummary Summary { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class UserStatsItem
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int AssignedClientCount { get; set; }
        public List<string> AssignedClientNames { get; set; } = new();
        public bool HasApiKey { get; set; }
        public DateTime? ApiKeyCreatedAt { get; set; }
    }

    public class UserStatsSummary
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int RegularUsers { get; set; }
        public int UsersWithApiKeys { get; set; }
        public int UsersLoggedInLast30Days { get; set; }
        public int RecentlyCreatedUsers { get; set; }
    }
}