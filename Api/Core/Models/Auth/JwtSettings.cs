namespace LeadHype.Api.Core.Models.Auth
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = "SmartleadAPI";
        public string Audience { get; set; } = "SmartleadClient";
        public int ExpirationInMinutes { get; set; } = 60; // 1 hour - more reasonable for user experience
        public int RefreshTokenExpirationInDays { get; set; } = 7; // 7 days
    }
}