using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for campaign email accounts
    /// </summary>
    public class CampaignEmailAccountsResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// List of email accounts assigned to this campaign
        /// </summary>
        public List<CampaignEmailAccountData> Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Email accounts retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Email account data for campaign assignments
    /// </summary>
    public class CampaignEmailAccountData
    {
        /// <summary>
        /// Email account ID
        /// </summary>
        /// <example>1001</example>
        public long Id { get; set; }
        
        /// <summary>
        /// Email address
        /// </summary>
        /// <example>sender@company.com</example>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name for the email account
        /// </summary>
        /// <example>Jane Sender</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the email account
        /// </summary>
        /// <example>Active</example>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Client ID this email account belongs to
        /// </summary>
        /// <example>client-001</example>
        public string? ClientId { get; set; }
        
        /// <summary>
        /// Client name this email account belongs to
        /// </summary>
        /// <example>Acme Corporation</example>
        public string? ClientName { get; set; }
        
        /// <summary>
        /// Whether this account is currently sending campaign emails
        /// </summary>
        /// <example>true</example>
        public bool? IsSendingActualEmails { get; set; }
        
        /// <summary>
        /// Number of campaigns this account is assigned to
        /// </summary>
        /// <example>3</example>
        public int CampaignCount { get; set; }
        
        /// <summary>
        /// Number of active campaigns this account is assigned to
        /// </summary>
        /// <example>2</example>
        public int ActiveCampaignCount { get; set; }
        
        /// <summary>
        /// Total emails sent from this account
        /// </summary>
        /// <example>1250</example>
        public int Sent { get; set; }
        
        /// <summary>
        /// Total emails opened from this account
        /// </summary>
        /// <example>420</example>
        public int Opened { get; set; }
        
        /// <summary>
        /// Total replies received by this account
        /// </summary>
        /// <example>85</example>
        public int Replied { get; set; }
        
        /// <summary>
        /// Total emails bounced from this account
        /// </summary>
        /// <example>15</example>
        public int Bounced { get; set; }
        
        /// <summary>
        /// When the account was created
        /// </summary>
        /// <example>2024-01-10T08:00:00.000Z</example>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the account was last updated
        /// </summary>
        /// <example>2024-01-25T14:30:00.000Z</example>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response model for campaign lead history
    /// </summary>
    public class CampaignLeadHistoryResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Campaign lead history data with pagination
        /// </summary>
        public CampaignLeadHistoryData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Lead history retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Campaign lead history data with pagination information
    /// </summary>
    public class CampaignLeadHistoryData
    {
        /// <summary>
        /// Basic campaign information
        /// </summary>
        public CampaignLeadHistoryInfo Campaign { get; set; } = new();
        
        /// <summary>
        /// List of leads with conversation data and message counts
        /// </summary>
        public List<LeadHistoryEntry> Leads { get; set; } = new();
        
        /// <summary>
        /// Total number of leads in this campaign
        /// </summary>
        /// <example>150</example>
        public int TotalLeads { get; set; }
        
        /// <summary>
        /// Total number of messages across all leads
        /// </summary>
        /// <example>6750</example>
        public int TotalMessages { get; set; }
        
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        /// <example>1</example>
        public int CurrentPage { get; set; }
        
        /// <summary>
        /// Number of leads per page
        /// </summary>
        /// <example>20</example>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Total number of pages available
        /// </summary>
        /// <example>8</example>
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Basic campaign information for lead history
    /// </summary>
    public class CampaignLeadHistoryInfo
    {
        /// <summary>
        /// Internal campaign identifier
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// External campaign ID
        /// </summary>
        /// <example>12345</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Campaign name
        /// </summary>
        /// <example>Q4 Product Launch Campaign</example>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual lead entry with conversation data and message count
    /// </summary>
    public class LeadHistoryEntry
    {
        /// <summary>
        /// Internal lead identifier
        /// </summary>
        /// <example>lead-abc123</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// External Smartlead lead ID
        /// </summary>
        /// <example>sl_lead_xyz789</example>
        public string SmartleadLeadId { get; set; } = string.Empty;
        
        /// <summary>
        /// Number of email messages exchanged with this lead
        /// </summary>
        /// <example>5</example>
        public int MessageCount { get; set; }
        
        /// <summary>
        /// Full conversation data stored as JSON string containing lead details
        /// </summary>
        /// <example>{"firstName":"John","lastName":"Doe","email":"john.doe@company.com","companyName":"Acme Corp"}</example>
        public string? ConversationData { get; set; }
    }

    /// <summary>
    /// Response model for campaign templates
    /// </summary>
    public class CampaignTemplatesResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Campaign templates data with email sequence information
        /// </summary>
        public CampaignTemplatesData Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Templates retrieved successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code (null for successful responses)
        /// </summary>
        /// <example>null</example>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Campaign templates data with sequence information
    /// </summary>
    public class CampaignTemplatesData
    {
        /// <summary>
        /// Basic campaign information
        /// </summary>
        public CampaignTemplatesInfo Campaign { get; set; } = new();
        
        /// <summary>
        /// List of email templates in sequence order
        /// </summary>
        public List<EmailTemplateEntry> Templates { get; set; } = new();
        
        /// <summary>
        /// Total number of templates in this campaign
        /// </summary>
        /// <example>3</example>
        public int TotalTemplates { get; set; }
    }

    /// <summary>
    /// Basic campaign information for templates
    /// </summary>
    public class CampaignTemplatesInfo
    {
        /// <summary>
        /// Internal campaign identifier
        /// </summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// External campaign ID
        /// </summary>
        /// <example>12345</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Campaign name
        /// </summary>
        /// <example>Q4 Product Launch Campaign</example>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email template variant (for A/B testing)
    /// </summary>
    public class EmailTemplateVariantEntry
    {
        /// <summary>
        /// Variant ID from Smartlead
        /// </summary>
        /// <example>3143163</example>
        public long Id { get; set; }

        /// <summary>
        /// Variant label (A, B, C, etc.)
        /// </summary>
        /// <example>A</example>
        public string VariantLabel { get; set; } = string.Empty;

        /// <summary>
        /// Email subject line for this variant
        /// </summary>
        /// <example>Introduction - Partnership Opportunity with {{CompanyName}}</example>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body content for this variant
        /// </summary>
        /// <example>Hi {{FirstName}},\n\nI hope this email finds you well...</example>
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual email template entry
    /// </summary>
    public class EmailTemplateEntry
    {
        /// <summary>
        /// Email subject line with personalization variables
        /// </summary>
        /// <example>Introduction - Partnership Opportunity with {{CompanyName}}</example>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body content with personalization variables
        /// </summary>
        /// <example>Hi {{FirstName}},\n\nI hope this email finds you well. I wanted to reach out regarding a potential partnership opportunity between {{CompanyName}} and our company...</example>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Sequence number indicating the order of this email (1 for first email, 2 for follow-up, etc.)
        /// </summary>
        /// <example>1</example>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// A/B test variants for this sequence (null if no variants)
        /// </summary>
        public List<EmailTemplateVariantEntry>? Variants { get; set; }
    }

    /// <summary>
    /// Response model for campaign sequence configuration
    /// </summary>
    public class CampaignSequenceConfigResponse
    {
        /// <summary>
        /// Indicates if the configuration was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Campaign sequence configured successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Campaign ID that was configured
        /// </summary>
        /// <example>12345</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Configuration timestamp
        /// </summary>
        /// <example>2024-01-25T10:30:45.123Z</example>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for campaign lead upload
    /// </summary>
    public class CampaignLeadUploadResponse
    {
        /// <summary>
        /// Indicates if the upload was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Leads uploaded successfully</example>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Campaign ID where leads were uploaded
        /// </summary>
        /// <example>12345</example>
        public int CampaignId { get; set; }
        
        /// <summary>
        /// Number of leads successfully uploaded
        /// </summary>
        /// <example>25</example>
        public int UploadedCount { get; set; }
        
        /// <summary>
        /// List of generated lead IDs
        /// </summary>
        /// <example>["lead-001", "lead-002", "lead-003"]</example>
        public List<string> LeadIds { get; set; } = new();
        
        /// <summary>
        /// Upload timestamp
        /// </summary>
        /// <example>2024-01-25T10:30:45.123Z</example>
        public DateTime Timestamp { get; set; }
    }
}