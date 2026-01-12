using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Models.UI;

public class CreateCampaignRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ClientId { get; set; } = string.Empty;
    
    public List<string> TagIds { get; set; } = new List<string>();
}

public class UpdateCampaignRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? ClientId { get; set; }
    
    public List<string>? TagIds { get; set; }
}

public class BulkAssignClientRequest
{
    /// <summary>
    /// List of campaign IDs to assign the client to
    /// </summary>
    [Required]
    public List<string> CampaignIds { get; set; } = new List<string>();
    
    /// <summary>
    /// Client ID to assign to the campaigns (null to unassign)
    /// </summary>
    public string? ClientId { get; set; }
}