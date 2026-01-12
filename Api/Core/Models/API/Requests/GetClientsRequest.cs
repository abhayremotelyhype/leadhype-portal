using System.ComponentModel;

namespace LeadHype.Api;

public class GetClientsRequest : AdminAccountCore
{
    [DefaultValue(30)]
    public int? Limit { get; set; } = 30;
}