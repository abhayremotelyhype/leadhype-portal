using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Client statistics response with engagement metrics and timing data
    /// </summary>
    public class ClientStatsResponse
    {
        /// <summary>
        /// Client information
        /// </summary>
        public ClientInfo Client { get; set; } = new();

        /// <summary>
        /// Email engagement statistics
        /// </summary>
        public EngagementStats Stats { get; set; } = new();

        /// <summary>
        /// Reply timing information
        /// </summary>
        public ReplyTiming Timing { get; set; } = new();
    }

    public class ClientInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string Color { get; set; } = "#3B82F6";
        public string Status { get; set; } = "active";
        
        /// <summary>
        /// Number of campaigns assigned to this client
        /// </summary>
        public int CampaignCount { get; set; }
        
        /// <summary>
        /// Number of active campaigns assigned to this client
        /// </summary>
        public int ActiveCampaignCount { get; set; }
        
        /// <summary>
        /// Number of email accounts assigned to this client's campaigns
        /// </summary>
        public int EmailAccountCount { get; set; }
    }

    public class EngagementStats
    {
        /// <summary>
        /// Total emails sent to this client's campaigns
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// Total replies received (positive + negative)
        /// </summary>
        public int TotalReplies { get; set; }

        /// <summary>
        /// Total positive replies received
        /// </summary>
        public int PositiveReplies { get; set; }

        /// <summary>
        /// Number of emails sent per reply (any type)
        /// </summary>
        public double EmailsPerReply { get; set; }

        /// <summary>
        /// Number of emails sent per positive reply
        /// </summary>
        public double EmailsPerPositiveReply { get; set; }

        /// <summary>
        /// Number of replies needed to get one positive reply
        /// </summary>
        public double RepliesPerPositiveReply { get; set; }

        /// <summary>
        /// Positive reply percentage based on total replies
        /// </summary>
        public double PositiveReplyPercentage { get; set; }

        /// <summary>
        /// Overall reply rate percentage (replies / sent * 100)
        /// </summary>
        public double ReplyRate { get; set; }
    }

    public class ReplyTiming
    {
        /// <summary>
        /// When the last reply was received (any type)
        /// </summary>
        public DateTime? LastReplyAt { get; set; }

        /// <summary>
        /// When the last positive reply was received
        /// </summary>
        public DateTime? LastPositiveReplyAt { get; set; }

        /// <summary>
        /// When the last contact occurred (any email sent or received)
        /// </summary>
        public DateTime? LastContactedAt { get; set; }

        /// <summary>
        /// Relative time string for last reply (e.g., "3 days ago")
        /// </summary>
        public string? LastReplyRelative { get; set; }

        /// <summary>
        /// Relative time string for last positive reply (e.g., "2 weeks ago")
        /// </summary>
        public string? LastPositiveReplyRelative { get; set; }

        /// <summary>
        /// Relative time string for last contact (e.g., "5 hours ago")
        /// </summary>
        public string? LastContactedRelative { get; set; }
    }

    /// <summary>
    /// Collection response for multiple client statistics
    /// </summary>
    public class ClientStatsCollectionResponse
    {
        public List<ClientStatsResponse> Clients { get; set; } = new();
        public int TotalCount { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public PaginationInfo Pagination { get; set; } = new();
        
        /// <summary>
        /// Aggregated statistics for all clients (not just current page)
        /// </summary>
        public EngagementStats? AggregatedStats { get; set; }
        
        /// <summary>
        /// Aggregated timing information for all clients
        /// </summary>
        public ReplyTiming? AggregatedTiming { get; set; }
        
        /// <summary>
        /// Client status summary
        /// </summary>
        public ClientStatusSummary? ClientStatusSummary { get; set; }
    }

    /// <summary>
    /// Summary of client statuses
    /// </summary>
    public class ClientStatusSummary
    {
        /// <summary>
        /// Number of active clients
        /// </summary>
        public int ActiveClients { get; set; }
        
        /// <summary>
        /// Number of inactive clients
        /// </summary>
        public int InactiveClients { get; set; }
        
        /// <summary>
        /// Total number of clients
        /// </summary>
        public int TotalClients { get; set; }
        
        /// <summary>
        /// Total number of campaigns across all clients
        /// </summary>
        public int TotalCampaigns { get; set; }
        
        /// <summary>
        /// Total number of active campaigns across all clients
        /// </summary>
        public int ActiveCampaigns { get; set; }
    }

    /// <summary>
    /// Pagination metadata
    /// </summary>
    public class PaginationInfo
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}