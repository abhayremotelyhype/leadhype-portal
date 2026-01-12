using LeadHype.Api.Core.Database.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface ICampaignDailyStatEntryRepository
{
    /// <summary>
    /// Get all stat entries for a campaign
    /// </summary>
    Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdAsync(string campaignId);
    
    /// <summary>
    /// Get stat entries for a campaign within a date range
    /// </summary>
    Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdAndDateRangeAsync(
        string campaignId, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Get stat entries for multiple campaigns within a date range
    /// </summary>
    Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdsAndDateRangeAsync(
        List<string> campaignIds, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Get aggregated stats for a campaign within a date range
    /// </summary>
    Task<CampaignDailyStatEntry?> GetAggregatedStatsByCampaignAsync(
        string campaignId, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Get or create stat entry for a specific campaign and date
    /// </summary>
    Task<CampaignDailyStatEntry> GetOrCreateStatEntryAsync(
        string adminUuid,
        string campaignId, 
        int campaignIdInt,
        DateTime statDate);
    
    /// <summary>
    /// Update stats for a specific campaign and date
    /// </summary>
    Task UpdateStatsAsync(
        string campaignId, 
        DateTime statDate, 
        int sent, 
        int opened, 
        int clicked, 
        int replied, 
        int positiveReplies, 
        int bounced);
    
    /// <summary>
    /// Upsert (insert or update) stats for a specific campaign and date
    /// </summary>
    Task UpsertStatsAsync(
        string adminUuid,
        string campaignId, 
        int campaignIdInt,
        DateTime statDate, 
        int sent, 
        int opened, 
        int clicked, 
        int replied, 
        int positiveReplies, 
        int bounced);
    
    /// <summary>
    /// Get all stat entries for an admin within a date range
    /// </summary>
    Task<IEnumerable<CampaignDailyStatEntry>> GetByAdminAndDateRangeAsync(
        string adminUuid, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Delete stat entries for a campaign
    /// </summary>
    Task<bool> DeleteByCampaignIdAsync(string campaignId);
    
    /// <summary>
    /// Get stat entries by external campaign ID
    /// </summary>
    Task<IEnumerable<CampaignDailyStatEntry>> GetByCampaignIdIntAsync(int campaignIdInt);
    
    /// <summary>
    /// Get aggregated daily stats for dashboard performance trends
    /// Optimized for large datasets by aggregating in the database
    /// </summary>
    Task<IEnumerable<dynamic>> GetAggregatedDailyStatsAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get aggregated daily stats filtered by campaign IDs for user access control
    /// </summary>
    Task<IEnumerable<dynamic>> GetAggregatedDailyStatsByCampaignsAsync(DateTime startDate, DateTime endDate, List<string> campaignIds);
    
    /// <summary>
    /// Get aggregated totals for multiple campaigns - optimized for client statistics
    /// </summary>
    Task<(int TotalSent, int TotalReplied, int TotalPositive, DateTime? LastReplyDate, DateTime? LastPositiveDate)> GetAggregatedTotalsForCampaignsAsync(List<string> campaignIds);
}