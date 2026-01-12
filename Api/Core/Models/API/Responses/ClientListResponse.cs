using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client list with pagination
    /// </summary>
    public class ClientListResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// List of clients with enriched metadata
        /// </summary>
        public List<ClientListItem> Data { get; set; } = new();
        
        /// <summary>
        /// Pagination information
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Individual client item in the list with enriched metadata
    /// </summary>
    public class ClientListItem
    {
        /// <summary>
        /// Unique client identifier
        /// </summary>
        /// <example>client-001</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Client name
        /// </summary>
        /// <example>Acme Corporation</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Client email address
        /// </summary>
        /// <example>contact@acme.com</example>
        public string? Email { get; set; }
        
        /// <summary>
        /// Client company name
        /// </summary>
        /// <example>Acme Corp</example>
        public string? Company { get; set; }
        
        /// <summary>
        /// Client status
        /// </summary>
        /// <example>active</example>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Client notes
        /// </summary>
        /// <example>Premium client with multiple campaigns</example>
        public string? Notes { get; set; }
        
        /// <summary>
        /// When the client was created
        /// </summary>
        /// <example>2024-01-15T08:30:00.000Z</example>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the client was last updated
        /// </summary>
        /// <example>2024-01-20T14:22:00.000Z</example>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Total number of campaigns owned by this client
        /// </summary>
        /// <example>12</example>
        public int CampaignCount { get; set; }
        
        /// <summary>
        /// Number of active campaigns owned by this client
        /// </summary>
        /// <example>8</example>
        public int ActiveCampaigns { get; set; }
        
        /// <summary>
        /// Total number of email accounts owned by this client
        /// </summary>
        /// <example>25</example>
        public int EmailAccountCount { get; set; }
    }
}