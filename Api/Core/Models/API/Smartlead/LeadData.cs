using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models.API.Smartlead;

public class CampaignLead
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<KeyValuePair<string, string>> CustomFields { get; set; } = new();
}

public class LeadEmailHistory
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string Type { get; set; } = "SENT"; // SENT or REPLY
    public DateTime? Time { get; set; }
}

public class CampaignEmailTemplateVariant
{
    public long Id { get; set; }
    public string VariantLabel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class CampaignEmailTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public List<CampaignEmailTemplateVariant>? Variants { get; set; }
}