using System.ComponentModel;

namespace LeadHype.Api;

public class SmartleadUserDetails
{
    [DefaultValue(false)]
    public bool? IsTeamMember { get; set; }

    [DefaultValue(false)]
    public bool? IsEmailVerified { get; set; }

    [DefaultValue(1)]
    public int? Id { get; set; }

    [DefaultValue("Development")]
    public string? Name { get; set; }

    [DefaultValue("smith@gmail.com")]
    public string? Email { get; set; }

    [DefaultValue("dfce622a-fd87-41b7-8173-0e62b94eea50")]
    public string? Uuid { get; set; }

    [DefaultValue("FULL_MEMBER_ACCESS")]
    public string? Role { get; set; }

    [DefaultValue("")]
    public string? ProfilePicUrl { get; set; }

    [DefaultValue(400)]
    public int? UserId { get; set; }

    [DefaultValue("f38e38e8-d7b6-4c00-836d-3437648fe5d9")]
    public string? UserUuid { get; set; }

    [DefaultValue("admin@example.com")]
    public string? UserEmail { get; set; }

    [DefaultValue("username")]
    public string? UserName { get; set; }

}