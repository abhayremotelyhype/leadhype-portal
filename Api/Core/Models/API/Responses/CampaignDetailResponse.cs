using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for campaign detail
    /// </summary>
    public class CampaignDetailResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Campaign details with complete information
        /// </summary>
        public CampaignDetailData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Campaign retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Campaign detail data
    /// </summary>
    public class CampaignDetailData
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
        /// When the campaign was created
        /// </summary>
        /// <example>2024-01-15T08:30:00.000Z</example>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the campaign was last updated
        /// </summary>
        /// <example>2024-01-20T14:22:00.000Z</example>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Total number of leads in campaign
        /// </summary>
        /// <example>1500</example>
        public int? TotalLeads { get; set; }
        
        /// <summary>
        /// Total emails sent
        /// </summary>
        /// <example>8750</example>
        public int? TotalSent { get; set; }
        
        /// <summary>
        /// Total emails opened
        /// </summary>
        /// <example>3200</example>
        public int? TotalOpened { get; set; }
        
        /// <summary>
        /// Total replies received
        /// </summary>
        /// <example>240</example>
        public int? TotalReplied { get; set; }
        
        /// <summary>
        /// Total emails bounced
        /// </summary>
        /// <example>125</example>
        public int? TotalBounced { get; set; }
        
        /// <summary>
        /// Total email clicks
        /// </summary>
        /// <example>680</example>
        public int? TotalClicked { get; set; }
        
        /// <summary>
        /// Total positive replies received
        /// </summary>
        /// <example>180</example>
        public int? TotalPositiveReplies { get; set; }
        
        /// <summary>
        /// List of email account IDs assigned to this campaign
        /// </summary>
        /// <example>[1001, 1002, 1003]</example>
        public List<int>? EmailIds { get; set; }
    }
}