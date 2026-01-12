using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Requests
{
    /// <summary>
    /// Request model for filtering campaigns by client IDs using POST to avoid header size limitations
    /// </summary>
    public class CampaignFilterRequest
    {
        /// <summary>
        /// List of client IDs to filter campaigns by
        /// </summary>
        public List<string> ClientIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 2000, ErrorMessage = "PageSize must be between 1 and 2000")]
        public int PageSize { get; set; } = 1000;
    }
}