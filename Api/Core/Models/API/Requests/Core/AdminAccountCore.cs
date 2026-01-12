using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LeadHype.Api;

public class AdminAccountCore
{
    [DefaultValue(""), JsonPropertyName("email")]
    public string AdminEmail { get; set; }
 
    [DefaultValue(""), JsonPropertyName("password")]
    public string AdminPassword { get; set; }
    
    [DefaultValue(""), JsonPropertyName("apiKey")]
    public string ApiKey { get; set; }
}