using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client creation
    /// </summary>
    public class ClientCreationResponse
    {
        /// <summary>
        /// Indicates if the client was created successfully
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// The created client data
        /// </summary>
        public ClientCreationData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Client created successfully</example>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Created client data
    /// </summary>
    public class ClientCreationData
    {
        /// <summary>
        /// Auto-generated unique client identifier
        /// </summary>
        /// <example>client-abc123def456</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Client name
        /// </summary>
        /// <example>TechCorp Solutions</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Client email address
        /// </summary>
        /// <example>contact@techcorp.com</example>
        public string? Email { get; set; }
        
        /// <summary>
        /// Client company name
        /// </summary>
        /// <example>TechCorp Inc</example>
        public string? Company { get; set; }
        
        /// <summary>
        /// Client status (defaults to "active" if not specified)
        /// </summary>
        /// <example>active</example>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Client notes
        /// </summary>
        /// <example>New enterprise client with high volume needs</example>
        public string? Notes { get; set; }
        
        /// <summary>
        /// When the client was created (auto-generated)
        /// </summary>
        /// <example>2024-01-25T10:30:45.123Z</example>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the client was last updated (same as CreatedAt for new clients)
        /// </summary>
        /// <example>2024-01-25T10:30:45.123Z</example>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Campaign count (always 0 for newly created clients)
        /// </summary>
        /// <example>0</example>
        public int CampaignCount { get; set; }
        
        /// <summary>
        /// Active campaign count (always 0 for newly created clients)
        /// </summary>
        /// <example>0</example>
        public int ActiveCampaigns { get; set; }
        
        /// <summary>
        /// Email account count (always 0 for newly created clients)
        /// </summary>
        /// <example>0</example>
        public int EmailAccountCount { get; set; }
    }
}