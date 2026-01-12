using System.ComponentModel;

namespace LeadHype.Api;

public class ClientDto
{
    public int Id { get; set; }
    
    [DefaultValue("smith@gmail.com")]
    public string Email { get; set; }
    
    [DefaultValue("Smith")]
    public string Name { get; set; }
    
    [DefaultValue("Client")]
    public string Type { get; set; }
    
}