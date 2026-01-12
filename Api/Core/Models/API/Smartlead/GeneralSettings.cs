using System.ComponentModel;

namespace LeadHype.Api;

public class GeneralSettings
{
    [DefaultValue(25)]
    public int MessagePerDay { get; set; }
    
    [DefaultValue(7)]
    public int MinimumTimeGapInMinutes { get; set; }
    
    [DefaultValue("cname_record")]
    public string CustomTrackingDomain { get; set; }
}