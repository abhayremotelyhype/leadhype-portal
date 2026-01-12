namespace LeadHype.Api.Core.Models.API.Requests
{
    /// <summary>
    /// Request model for filtering email accounts by campaign IDs using POST to avoid header size limitations
    /// </summary>
    public class EmailAccountFilterRequest
    {
        /// <summary>
        /// List of campaign IDs to filter email accounts by
        /// </summary>
        public List<string> CampaignIds { get; set; } = new List<string>();
    }
}