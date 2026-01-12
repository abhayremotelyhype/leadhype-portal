using System.ComponentModel;

namespace LeadHype.Api;

public class OAuthRequest : AdminAccountCore
{
    public GoogleWorkspaceAccount? WorkspaceAccount { get; set; }
    public ProxyModel? Proxy { get; set; }
    
    [DefaultValue("")]
    public string? CallbackUrl { get; set; }
    
}

public class GoogleWorkspaceAccount
{
    [DefaultValue("")]
    public string? Email { get; set; }
    
    [DefaultValue("")]
    public string? Password { get; set; }
}