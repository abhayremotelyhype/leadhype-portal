using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client campaigns
    /// </summary>
    public class ClientCampaignsResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// List of client campaigns
        /// </summary>
        public List<object> Data { get; set; } = new();
        
        /// <summary>
        /// Pagination information
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();
    }
}