using System.Text.Json.Serialization;

namespace LeadHype.Api;

public class TaskModel
{
    public int Id { get; set; }
    
    [JsonIgnore]
    public bool IsCompleted { get; set; }
    
    [JsonIgnore]
    public bool IsSuccess { get; set; }

    public string Status
    {
        get
        {
            return IsCompleted switch
            {
                false => "In Progress",
                true when !IsSuccess => "Failed",
                _ => "Success"
            };
        }
    }

    public bool CallbackUrlCalled { get; set; }
    public string? Message { get; set; }
}