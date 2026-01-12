using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for email accounts summary
    /// </summary>
    public class EmailAccountSummaryResponse
    {
        /// <summary>
        /// Total number of email accounts
        /// </summary>
        /// <example>25</example>
        public int TotalAccounts { get; set; }
        
        /// <summary>
        /// Breakdown of accounts by their current status
        /// </summary>
        public Dictionary<string, int> AccountsByStatus { get; set; } = new();
    }
}