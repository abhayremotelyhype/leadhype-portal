using Newtonsoft.Json;

namespace LeadHype.Api.Core.Models;

public class LeadsResponse
{
    [JsonProperty("total_leads")]
    public string TotalLeads { get; set; }

    [JsonProperty("data")]
    public List<LeadDatum> Data { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }

    [JsonProperty("limit")]
    public int Limit { get; set; }

}   

public class UnsubscribedClientIdMap
{
    [JsonProperty("3138")]
    public DateTime? _3138 { get; set; }
}

public class Lead
{
    [JsonProperty("id")]
    public long? Id { get; set; }

    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("phone_number")]
    public string PhoneNumber { get; set; }

    [JsonProperty("company_name")]
    public string CompanyName { get; set; }

    [JsonProperty("website")]
    public string Website { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }

    [JsonProperty("custom_fields")]
    public CustomFields CustomFields { get; set; }

    [JsonProperty("linkedin_profile")]
    public string LinkedinProfile { get; set; }

    [JsonProperty("company_url")]
    public object CompanyUrl { get; set; }

    [JsonProperty("is_unsubscribed")]
    public bool? IsUnsubscribed { get; set; }

    [JsonProperty("unsubscribed_client_id_map")]
    public UnsubscribedClientIdMap UnsubscribedClientIdMap { get; set; }
}

public class LeadDatum
{
    [JsonProperty("campaign_lead_map_id")]
    public string CampaignLeadMapId { get; set; }

    [JsonProperty("lead_category_id")]
    public object LeadCategoryId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("lead")]
    public Lead Lead { get; set; }
}

public class CustomFields
{
    [JsonProperty("StreetAddress")]
    public string StreetAddress { get; set; }

    [JsonProperty("contact.first_name")]
    public string ContactFirstName { get; set; }

    [JsonProperty("contact.print_name")]
    public string ContactPrintName { get; set; }

    [JsonProperty("contact.site_street_address")]
    public string ContactSiteStreetAddress { get; set; }

    [JsonProperty("contact.site_city")]
    public string ContactSiteCity { get; set; }

    [JsonProperty("SiteStreetAddress")]
    public string SiteStreetAddress { get; set; }

    [JsonProperty("City")]
    public string City { get; set; }

    [JsonProperty("PrintName")]
    public string PrintName { get; set; }

    [JsonProperty("Print_Name")]
    public string PrintName2 { get; set; }

    [JsonProperty("contact.property_city")]
    public string ContactPropertyCity { get; set; }

    [JsonProperty("contact.print")]
    public string ContactPrint { get; set; }

    [JsonProperty("site_city")]
    public string SiteCity { get; set; }

    [JsonProperty("contact.city")]
    public string ContactCity { get; set; }
}