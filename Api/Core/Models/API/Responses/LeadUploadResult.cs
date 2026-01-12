namespace LeadHype.Api.Core.Models.API.Responses
{
    public class LeadUploadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int UploadedCount { get; set; }
        public Dictionary<string, string>? LeadIds { get; set; }
    }
}