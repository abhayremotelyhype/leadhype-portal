using System.ComponentModel;

namespace LeadHype.Api;

public class DeleteAccountByEmail : AdminAccountCore
{
    [DefaultValue("")]
    public string? Email { get; set; }
}