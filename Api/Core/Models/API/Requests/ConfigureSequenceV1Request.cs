using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Requests
{
    public class ConfigureSequenceV1Request
    {
        [Required]
        public List<SequenceStep> Steps { get; set; } = new List<SequenceStep>();
    }

    public class SequenceStep
    {
        public int? Id { get; set; }
        
        [Required]
        public int StepNumber { get; set; }
        
        public string StepType { get; set; } = "EMAIL";
        
        public SequenceDelay DelaySettings { get; set; } = new SequenceDelay();
        
        public string TestingMode { get; set; } = "MANUAL_EQUAL";
        
        public int SampleSize { get; set; } = 40;
        
        public string WinningCriteria { get; set; } = "OPEN_RATE";
        
        public List<MessageVariant> MessageVariants { get; set; } = new List<MessageVariant>();
        
        // For simple follow-ups without variants
        public string? Subject { get; set; }
        
        public string? Content { get; set; }
    }

    public class SequenceDelay
    {
        [Required]
        public int DelayDays { get; set; } = 1;
    }

    public class MessageVariant
    {
        public int? Id { get; set; }
        
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public string Label { get; set; } = string.Empty;
        
        public int DistributionPercentage { get; set; } = 0;
    }
}