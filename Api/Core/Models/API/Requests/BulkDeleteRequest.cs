using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Models.UI;

public class BulkDeleteRequest
{
    [Required]
    public List<string> CampaignIds { get; set; } = new();
}