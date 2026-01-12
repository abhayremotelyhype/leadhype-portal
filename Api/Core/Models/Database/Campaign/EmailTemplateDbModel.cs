using LeadHype.Api.Core.Models;

namespace LeadHype.Api.Models;

public class EmailTemplateVariantDbModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TemplateId { get; set; } = string.Empty;
    public long SmartleadVariantId { get; set; }
    public string VariantLabel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EmailTemplateDbModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AdminUuid { get; set; }
    public int CampaignId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}