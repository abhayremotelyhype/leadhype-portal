using System.ComponentModel;
using Newtonsoft.Json;

namespace LeadHype.Api;

public class ConfigureAccountRequest : AdminAccountCore
{
    public string EmailAccount { get; set; }
    
    public GeneralSettings GeneralSettings { get; set; }
    
    public WarmupSettings WarmupSettings { get; set; }
}