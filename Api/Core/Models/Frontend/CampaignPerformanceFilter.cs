using System;
using System.Collections.Generic;

namespace LeadHype.Api.Core.Models.Frontend
{
    public class CampaignPerformanceFilter
    {
        // Time range filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Period { get; set; } // "7", "30", "90", "6m", "1y", "all"
        public int? TimeRangeDays { get; set; } // Like campaigns endpoint - overrides other time filters
        
        // Volume filters
        public int MinimumSent { get; set; } = 0;
        public int? MaximumSent { get; set; }
        public int? MinimumReplies { get; set; }
        
        // Performance thresholds
        public double? MinimumReplyRate { get; set; }
        public double? MaximumReplyRate { get; set; }
        public double? MinimumOpenRate { get; set; }
        public double? MaximumBounceRate { get; set; }
        
        // Campaign filters
        public List<string>? CampaignIds { get; set; }
        public List<string>? ClientIds { get; set; }
        public List<string>? Statuses { get; set; } // "ACTIVE", "PAUSED", "COMPLETED", "STOPPED"
        
        // Sorting options
        public string SortBy { get; set; } = "ReplyRate"; // ReplyRate, OpenRate, TotalSent, TotalReplied, LastActivity, CompositeScore
        public bool SortDescending { get; set; } = true;
        
        // Pagination
        public int Limit { get; set; } = 10;
        public int Offset { get; set; } = 0;
        
        // Advanced options
        public bool UseCompositeScore { get; set; } = false;
        public bool ExcludeInactive { get; set; } = true;
        public int? MinimumDaysActive { get; set; }
        public int? MaximumDaysActive { get; set; }
    }
    
    public class CampaignPerformanceResponse
    {
        public List<CampaignPerformanceMetric> Campaigns { get; set; } = new();
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
        public CampaignPerformanceStats Stats { get; set; } = new();
    }
    
    public class CampaignPerformanceStats
    {
        public double AverageReplyRate { get; set; }
        public double AverageOpenRate { get; set; }
        public double AverageBounceRate { get; set; }
        public int TotalEmailsSent { get; set; }
        public int TotalReplies { get; set; }
        public int TotalOpens { get; set; }
        public int CampaignsAnalyzed { get; set; }
    }
}