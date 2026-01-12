using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    public class CampaignCreationResponse
    {
        /// <summary>
        /// Indicates if the campaign was created successfully
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Campaign created successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// The ID of the created campaign
        /// </summary>
        /// <example>123456</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Timestamp when the campaign was created
        /// </summary>
        /// <example>2025-01-09T12:34:56.789Z</example>
        public DateTime Timestamp { get; set; }
    }
}