using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for campaign list with pagination
    /// </summary>
    public class CampaignListResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// List of campaigns with metrics
        /// </summary>
        public List<CampaignListItem> Data { get; set; } = new();
        
        /// <summary>
        /// Pagination information
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();
        
        /// <summary>
        /// Timestamp when the data was generated
        /// </summary>
        /// <example>2025-01-09T12:34:56.789Z</example>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Individual campaign item in the list
    /// </summary>
    public class CampaignListItem
    {
        /// <summary>
        /// Unique internal campaign identifier
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Campaign ID from external platform
        /// </summary>
        /// <example>12345</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Campaign name
        /// </summary>
        /// <example>Q4 Product Launch Campaign</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Client ID this campaign belongs to
        /// </summary>
        /// <example>client-001</example>
        public string? ClientId { get; set; }
        
        /// <summary>
        /// Client name this campaign belongs to
        /// </summary>
        /// <example>Acme Corporation</example>
        public string? ClientName { get; set; }
        
        /// <summary>
        /// Current campaign status
        /// </summary>
        /// <example>Active</example>
        public string? Status { get; set; }
        
        /// <summary>
        /// Campaign metrics and performance data
        /// </summary>
        public CampaignMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Campaign metrics and performance data
    /// </summary>
    public class CampaignMetrics
    {
        /// <summary>
        /// Total number of leads in campaign
        /// </summary>
        /// <example>1500</example>
        public int TotalLeads { get; set; }
        
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
        /// Total replies received
        /// </summary>
        /// <example>240</example>
        public int TotalReplied { get; set; }
        
        /// <summary>
        /// Total emails bounced
        /// </summary>
        /// <example>125</example>
        public int TotalBounced { get; set; }
        
        /// <summary>
        /// Total email clicks
        /// </summary>
        /// <example>680</example>
        public int TotalClicked { get; set; }
        
        /// <summary>
        /// Total positive replies received
        /// </summary>
        /// <example>180</example>
        public int PositiveReplies { get; set; }
    }

}