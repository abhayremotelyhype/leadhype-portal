using LeadHype.Api.Core.Models;

namespace LeadHype.Api;

public class LeadIdResponse
{
    public string Id { get; set; }
    public string Status { get; set; }
    public Lead Lead { get; set; }
    
    public List<KeyValuePair<string,string>> CustomFields { get; set; } = new();

}