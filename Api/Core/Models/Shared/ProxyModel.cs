using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LeadHype.Api;

public class ProxyModel
{
    [DefaultValue("")]
    public string? Host { get; set; }
    
    [DefaultValue(0)]
    public int? Port { get; set; }
    
    [JsonIgnore]
    public bool HasProxy =>
        !string.IsNullOrEmpty(Host) && 
        Port.HasValue;

    [DefaultValue("")]
    public string? Username { get; set; }

    [DefaultValue("")]
    public string? Password { get; set; }
    
    [DefaultValue("https")]
    public string Protocol { get; set; } = "https";

}
public enum ProxyProtocol
{
    Http,
    Socks5
}