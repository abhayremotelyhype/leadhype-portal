using LeadHype.Api.Core.Models;

namespace LeadHype.Api.Core.Database.Models;

public class EmailAccountDbModel
{
    /// <summary>
    /// Admin account
    /// </summary>
    public string AdminUuid { get; set; }
    
    /// <summary>
    /// Email Id of account
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Email address of the account
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Name of the email account
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Status of the email account
    /// </summary>
    public string Status { get; set; }

    // Client assignment
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientColor { get; set; }

    //Warmup stats
    public int WarmupSent { get; set; }
    public int WarmupReplied { get; set; }
    public int WarmupSpamCount { get; set; }
    public int WarmupSavedFromSpam { get; set; }
    
    // Email statistics
    public int Sent { get; set; } = 0;
    public int Opened { get; set; } = 0;
    public int Replied { get; set; } = 0;
    public int Bounced { get; set; } = 0;
    
    // Tags support
    public List<string> Tags { get; set; } = new List<string>();
    
    // Campaign association count
    public int CampaignCount { get; set; } = 0;
    public int ActiveCampaignCount { get; set; } = 0;
    
    // Email sending status
    // true = sending actual campaign emails
    // false = only sending warmup emails
    // null = not sending any emails (inactive)
    public bool? IsSendingActualEmails { get; set; } = null;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    
    // API data fetch timestamps
    public DateTime? WarmupUpdateDateTime { get; set; }
    
    // Notes
    public string? Notes { get; set; }

}