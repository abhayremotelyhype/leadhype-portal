using LeadHype.Api.Core.Database.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IEmailAccountDailyStatEntryRepository
{
    /// <summary>
    /// Get all stat entries for an email account
    /// </summary>
    Task<IEnumerable<EmailAccountDailyStatEntry>> GetByEmailAccountIdAsync(long emailAccountId);
    
    /// <summary>
    /// Get stat entries for an email account within a date range
    /// </summary>
    Task<IEnumerable<EmailAccountDailyStatEntry>> GetByEmailAccountIdAndDateRangeAsync(
        long emailAccountId, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Get stat entries for multiple email accounts within a date range
    /// </summary>
    Task<IEnumerable<EmailAccountDailyStatEntry>> GetByEmailAccountIdsAndDateRangeAsync(
        List<long> emailAccountIds, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Get aggregated stats for an email account within a date range
    /// </summary>
    Task<EmailAccountDailyStatEntry?> GetAggregatedStatsByEmailAccountAsync(
        long emailAccountId, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Get or create stat entry for a specific email account and date
    /// </summary>
    Task<EmailAccountDailyStatEntry> GetOrCreateStatEntryAsync(
        string adminUuid,
        long emailAccountId,
        DateTime statDate);
    
    /// <summary>
    /// Update stats for a specific email account and date
    /// </summary>
    Task UpdateStatsAsync(
        long emailAccountId, 
        DateTime statDate, 
        int sent, 
        int opened, 
        int replied, 
        int bounced);
    
    /// <summary>
    /// Upsert (insert or update) stats for a specific email account and date
    /// </summary>
    Task UpsertStatsAsync(
        string adminUuid,
        long emailAccountId,
        DateTime statDate, 
        int sent, 
        int opened, 
        int replied, 
        int bounced);
    
    /// <summary>
    /// Get all stat entries for an admin within a date range
    /// </summary>
    Task<IEnumerable<EmailAccountDailyStatEntry>> GetByAdminAndDateRangeAsync(
        string adminUuid, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Delete stat entries for an email account
    /// </summary>
    Task<bool> DeleteByEmailAccountIdAsync(long emailAccountId);
    
    /// <summary>
    /// Get aggregated stats for all accounts within a date range
    /// </summary>
    Task<Dictionary<string, int>> GetAggregatedStatsByDateAsync(
        string adminUuid,
        DateTime startDate, 
        DateTime endDate);
}