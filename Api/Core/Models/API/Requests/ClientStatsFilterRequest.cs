using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Requests
{
    /// <summary>
    /// Request model for filtering client statistics using POST to avoid header size limitations
    /// </summary>
    public class ClientStatsFilterRequest
    {
        /// <summary>
        /// List of client IDs to filter statistics by
        /// </summary>
        public string[]? ClientIds { get; set; }
        
        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// Field to sort by
        /// </summary>
        public string SortBy { get; set; } = "name";
        
        /// <summary>
        /// Whether to sort in descending order
        /// </summary>
        public bool SortDescending { get; set; } = false;
        
        /// <summary>
        /// Filter statistics from this date
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Filter statistics to this date
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Filter clients by status (active, inactive)
        /// </summary>
        public string? ClientStatus { get; set; }
        
        /// <summary>
        /// Filter clients by specific user ID (admin only)
        /// </summary>
        public string? FilterByUserId { get; set; }
    }
}