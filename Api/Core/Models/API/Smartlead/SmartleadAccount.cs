using System.Net;
using Newtonsoft.Json;

namespace LeadHype.Api;

public class SmartleadAccount
{
    #region Default Constructor
    public SmartleadAccount()
    {
        Cookies = new List<Cookie>();
    }
    #endregion
    
    #region Properties

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonIgnore]
    public List<Cookie> Cookies { get; set; }
    
    public string CookiesJson { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    
    public string AuthorizationToken 
    { 
        get => AccessToken; 
        set => AccessToken = value; 
    }
    
    [JsonIgnore]
    public SmartleadUserDetails UserDetails { get; set; }
    
    public string UserDetailsJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // public int UserId { get; set; }
    // public int AccountId { get; set; }

    // public string AccountType { get; set; } = string.Empty;
    // public int TotalEmailAccounts { get; set; }
    // public int ActiveEmails { get; set; }
    // public int Campaigns { get; set; }
    // public string Status { get; set; } = string.Empty;
    // public DateTime AddedOn { get; set; }
    // public DateTime UpdatedAt { get; set; }
    
    #endregion
}