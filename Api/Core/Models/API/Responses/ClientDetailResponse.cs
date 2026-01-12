using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client detail
    /// </summary>
    public class ClientDetailResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Client details with enriched metadata
        /// </summary>
        public ClientListItem Data { get; set; } = new();
    }
}