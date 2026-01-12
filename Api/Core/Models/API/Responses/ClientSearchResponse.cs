using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client search
    /// </summary>
    public class ClientSearchResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// List of matching clients (simplified format)
        /// </summary>
        public List<ClientSearchItem> Data { get; set; } = new();
    }

    /// <summary>
    /// Simplified client item for search results
    /// </summary>
    public class ClientSearchItem
    {
        /// <summary>
        /// Client ID
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
        public string? Status { get; set; }
    }
}