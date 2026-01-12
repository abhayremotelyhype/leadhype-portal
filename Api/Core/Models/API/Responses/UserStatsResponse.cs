using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// User statistics response with engagement metrics and timing data
    /// </summary>
    public class UserStatsResponse
    {
        /// <summary>
        /// User information
        /// </summary>
        public UserInfo User { get; set; } = new();

        /// <summary>
        /// Email engagement statistics for this user
        /// </summary>
        public UserEngagementStats Stats { get; set; } = new();

        /// <summary>
        /// Reply timing information for this user
        /// </summary>
        public UserReplyTiming Timing { get; set; } = new();
    }

    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        /// <summary>
        /// Number of clients assigned to this user
        /// </summary>
        public int AssignedClientCount { get; set; }
        
        /// <summary>
        /// Number of campaigns this user has access to
        /// </summary>
        public int AccessibleCampaignCount { get; set; }
        
        /// <summary>
        /// Number of active campaigns this user has access to
        /// </summary>
        public int ActiveCampaignCount { get; set; }
        
        /// <summary>
        /// Number of email accounts this user has access to
        /// </summary>
        public int AccessibleEmailAccountCount { get; set; }
        
        /// <summary>
        /// Whether this user has an API key
        /// </summary>
        public bool HasApiKey { get; set; }
        
        /// <summary>
        /// When the API key was created
        /// </summary>
        public DateTime? ApiKeyCreatedAt { get; set; }
    }

    public class UserEngagementStats
    {
        /// <summary>
        /// Total emails sent from campaigns this user has access to
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// Total replies received (positive + negative) from user's campaigns
        /// </summary>
        public int TotalReplies { get; set; }

        /// <summary>
        /// Total positive replies received from user's campaigns
        /// </summary>
        public int PositiveReplies { get; set; }

        /// <summary>
        /// Number of emails sent per reply (any type) in user's campaigns
        /// </summary>
        public double EmailsPerReply { get; set; }

        /// <summary>
        /// Number of emails sent per positive reply in user's campaigns
        /// </summary>
        public double EmailsPerPositiveReply { get; set; }

        /// <summary>
        /// Number of replies needed to get one positive reply in user's campaigns
        /// </summary>
        public double RepliesPerPositiveReply { get; set; }

        /// <summary>
        /// Positive reply percentage based on total replies in user's campaigns
        /// </summary>
        public double PositiveReplyPercentage { get; set; }

        /// <summary>
        /// Overall reply rate percentage (replies / sent * 100) for user's campaigns
        /// </summary>
        public double ReplyRate { get; set; }
    }

    public class UserReplyTiming
    {
        /// <summary>
        /// When the last reply was received from user's campaigns (any type)
        /// </summary>
        public DateTime? LastReplyAt { get; set; }

        /// <summary>
        /// When the last positive reply was received from user's campaigns
        /// </summary>
        public DateTime? LastPositiveReplyAt { get; set; }

        /// <summary>
        /// Relative time string for last reply (e.g., "3 days ago")
        /// </summary>
        public string? LastReplyRelative { get; set; }

        /// <summary>
        /// Relative time string for last positive reply (e.g., "2 weeks ago")
        /// </summary>
        public string? LastPositiveReplyRelative { get; set; }
    }

    /// <summary>
    /// Collection response for multiple user statistics
    /// </summary>
    public class UserStatsCollectionResponse
    {
        public List<UserStatsResponse> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public PaginationInfo Pagination { get; set; } = new();
        
        /// <summary>
        /// Aggregated statistics for all users (not just current page)
        /// </summary>
        public UserEngagementStats? AggregatedStats { get; set; }
        
        /// <summary>
        /// Aggregated timing information for all users
        /// </summary>
        public UserReplyTiming? AggregatedTiming { get; set; }
        
        /// <summary>
        /// User status summary
        /// </summary>
        public UserStatusSummary? UserStatusSummary { get; set; }
    }

    /// <summary>
    /// Summary of user statuses
    /// </summary>
    public class UserStatusSummary
    {
        /// <summary>
        /// Number of active users
        /// </summary>
        public int ActiveUsers { get; set; }
        
        /// <summary>
        /// Number of inactive users
        /// </summary>
        public int InactiveUsers { get; set; }
        
        /// <summary>
        /// Total number of users
        /// </summary>
        public int TotalUsers { get; set; }
        
        /// <summary>
        /// Number of admin users
        /// </summary>
        public int AdminUsers { get; set; }
        
        /// <summary>
        /// Number of regular users
        /// </summary>
        public int RegularUsers { get; set; }
        
        /// <summary>
        /// Number of users with API keys
        /// </summary>
        public int UsersWithApiKeys { get; set; }
        
        /// <summary>
        /// Number of users who logged in within the last 30 days
        /// </summary>
        public int UsersLoggedInLast30Days { get; set; }
    }
}