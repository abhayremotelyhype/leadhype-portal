namespace LeadHype.Api;

public class EmailAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string AdminId { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DailyLimit { get; set; }
    public int SentToday { get; set; }
    public string WarmupStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}