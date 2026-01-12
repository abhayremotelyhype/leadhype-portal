using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LeadHype.Api.Core.Models.API.Requests
{
    public class LeadInput
    {
        [Required]
        [DefaultValue("Cristiano")]
        public string FirstName { get; set; } = "Cristiano";

        [Required]
        [DefaultValue("Ronaldo")]
        public string LastName { get; set; } = "Ronaldo";

        [Required]
        [EmailAddress]
        [DefaultValue("cristiano@mufc.com")]
        public string Email { get; set; } = "cristiano@mufc.com";

        [DefaultValue("0239392029")]
        public string? PhoneNumber { get; set; }

        [DefaultValue("Manchester United")]
        public string? CompanyName { get; set; }

        [DefaultValue("mufc.com")]
        public string? Website { get; set; }

        [DefaultValue("London")]
        public string? Location { get; set; }

        /// <summary>
        /// Custom fields for the lead. Example: {"Title": "Regional Manager", "First_Line": "Loved your recent post about remote work on Linkedin"}
        /// </summary>
        /// <example>{"Title": "Regional Manager", "First_Line": "Loved your recent post about remote work on Linkedin"}</example>
        public Dictionary<string, object>? CustomFields { get; set; }

        [DefaultValue("http://www.linkedin.com/in/cristianoronaldo")]
        public string? LinkedinProfile { get; set; }

        [DefaultValue("mufc.com")]
        public string? CompanyUrl { get; set; }
    }

    public class LeadUploadSettings
    {
        public bool IgnoreGlobalBlockList { get; set; } = false;

        public bool IgnoreUnsubscribeList { get; set; } = false;

        public bool IgnoreCommunityBounceList { get; set; } = false;

        public bool IgnoreDuplicateLeadsInOtherCampaign { get; set; } = false;

        public bool ReturnLeadIds { get; set; } = false;
    }
}