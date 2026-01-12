using System.ComponentModel;

namespace LeadHype.Api;

public class DeleteAccountById 
{
    public string AdminEmail { get; set; }
    
    [DefaultValue(0)]
    public long? Id { get; set; }
}