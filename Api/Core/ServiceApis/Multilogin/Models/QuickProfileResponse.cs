namespace LeadHype.Api.ServiceApis
{
    public class QuickProfileResponse
    {
        public bool IsSuccess => !string.IsNullOrEmpty(Id) && 
            !string.IsNullOrEmpty(Port);
        public string Id { get; set; }
        public string Port { get; set; }
        //public string ErrorCode { get; set; }
    }
}
