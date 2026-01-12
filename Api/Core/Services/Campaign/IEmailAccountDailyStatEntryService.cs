using LeadHype.Api.Core.Database.Models;

namespace LeadHype.Api.Services;

/// <summary>
/// Improved service using relational model instead of Dictionary approach
/// </summary>
public interface IEmailAccountDailyStatEntryService
{
    /// <summary>
    /// Get daily sent stats for an email account as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetSentEmailsAsync(long emailAccountId, int days);
    
    /// <summary>
    /// Get daily opened stats for an email account as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetOpenedEmailsAsync(long emailAccountId, int days);
    
    
    /// <summary>
    /// Get daily replied stats for an email account as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetRepliedEmailsAsync(long emailAccountId, int days);
    
    /// <summary>
    /// Get daily bounced stats for an email account as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetBouncedEmailsAsync(long emailAccountId, int days);
    
    
    /// <summary>
    /// Get aggregated sent stats for all accounts as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetAllAccountsSentEmailsAsync(string adminUuid, int days);
    
    /// <summary>
    /// Get aggregated opened stats for all accounts as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetAllAccountsOpenedEmailsAsync(string adminUuid, int days);
    
    /// <summary>
    /// Get aggregated replied stats for all accounts as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetAllAccountsRepliedEmailsAsync(string adminUuid, int days);
    
    /// <summary>
    /// Get aggregated bounced stats for all accounts as date->count dictionary
    /// </summary>
    Task<Dictionary<string, int>> GetAllAccountsBouncedEmailsAsync(string adminUuid, int days);
    
    /// <summary>
    /// Get all accounts' sent stats grouped by account as account_id->date->count dictionary
    /// </summary>
    Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsSentEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get all accounts' opened stats grouped by account as account_id->date->count dictionary
    /// </summary>
    Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsOpenedEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get all accounts' replied stats grouped by account as account_id->date->count dictionary
    /// </summary>
    Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsRepliedEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get all accounts' bounced stats grouped by account as account_id->date->count dictionary
    /// </summary>
    Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsBouncedEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Update daily stats for an email account
    /// </summary>
    Task UpdateDailyStatsAsync(long emailAccountId, string adminUuid, DateTime statDate, int sent, int opened, int replied, int bounced);
    
    /// <summary>
    /// Get raw stat entries for an email account within date range
    /// </summary>
    Task<IEnumerable<EmailAccountDailyStatEntry>> GetStatEntriesAsync(long emailAccountId, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get aggregated stats for an email account within date range
    /// </summary>
    Task<EmailAccountDailyStatEntry?> GetAggregatedStatsAsync(long emailAccountId, DateTime startDate, DateTime endDate);
    
    // ============ OPTIMIZED SQL AGGREGATION METHODS ============
    // These methods use direct SQL SUM() queries for better performance
    
    /// <summary>
    /// Get total sent count for an email account within the last N days (optimized SQL)
    /// </summary>
    Task<int> GetTotalSentAsync(long emailAccountId, int days);
    
    /// <summary>
    /// Get total opened count for an email account within the last N days (optimized SQL)
    /// </summary>
    Task<int> GetTotalOpenedAsync(long emailAccountId, int days);
    
    
    /// <summary>
    /// Get total replied count for an email account within the last N days (optimized SQL)
    /// </summary>
    Task<int> GetTotalRepliedAsync(long emailAccountId, int days);
    
    
    /// <summary>
    /// Get total bounced count for an email account within the last N days (optimized SQL)
    /// </summary>
    Task<int> GetTotalBouncedAsync(long emailAccountId, int days);
    
    /// <summary>
    /// Get all totals for an email account within the last N days (single SQL query)
    /// </summary>
    Task<EmailAccountDailyStatEntry?> GetTotalStatsAsync(long emailAccountId, int days);
    
    /// <summary>
    /// Get all totals for an email account within a date range (single SQL query)
    /// </summary>
    Task<EmailAccountDailyStatEntry?> GetTotalStatsAsync(long emailAccountId, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get all totals for multiple email accounts within the last N days (batch optimized)
    /// </summary>
    Task<Dictionary<long, EmailAccountDailyStatEntry>> GetBatchTotalStatsAsync(List<long> emailAccountIds, int days);
    
    /// <summary>
    /// Get all totals for multiple email accounts within a date range (batch optimized)
    /// </summary>
    Task<Dictionary<long, EmailAccountDailyStatEntry>> GetBatchTotalStatsAsync(List<long> emailAccountIds, DateTime startDate, DateTime endDate);
}