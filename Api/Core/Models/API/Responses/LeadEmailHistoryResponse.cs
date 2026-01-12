using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for lead email history
    /// </summary>
    public class LeadEmailHistoryResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Lead email history data
        /// </summary>
        public LeadEmailHistoryData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Lead email history retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Lead email history data
    /// </summary>
    public class LeadEmailHistoryData
    {
        /// <summary>
        /// Lead identifier
        /// </summary>
        /// <example>lead-abc123</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Complete email conversation history for the lead
        /// </summary>
        public List<EmailHistoryEntry> EmailHistory { get; set; } = new();
    }

    /// <summary>
    /// Individual email entry in the conversation history
    /// </summary>
    public class EmailHistoryEntry
    {
        /// <summary>
        /// Email subject line
        /// </summary>
        /// <example>Introduction - Partnership Opportunity</example>
        public string Subject { get; set; } = string.Empty;
        
        /// <summary>
        /// Email body content
        /// </summary>
        /// <example>Hi John,\n\nI hope this email finds you well. I wanted to reach out regarding a potential partnership opportunity...</example>
        public string Body { get; set; } = string.Empty;
        
        /// <summary>
        /// Sequence number in the email campaign (1 for first email, 2 for follow-up, etc.)
        /// </summary>
        /// <example>1</example>
        public int SequenceNumber { get; set; }
        
        /// <summary>
        /// Type of email - SENT (outbound) or REPLY (inbound response)
        /// </summary>
        /// <example>SENT</example>
        public string Type { get; set; } = "SENT";
        
        /// <summary>
        /// When the email was sent (actual send time from campaign)
        /// </summary>
        /// <example>2024-01-15T09:30:00.000Z</example>
        public DateTime? Time { get; set; }
        
        /// <summary>
        /// When the email record was created in our system
        /// </summary>
        /// <example>2024-01-15T09:30:00.000Z</example>
        public DateTime? CreatedAt { get; set; }
    }
}