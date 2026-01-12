using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Database.Repositories;

namespace LeadHype.Api.Services;

/// <summary>
/// Improved service using relational model instead of Dictionary approach
/// </summary>
public class EmailAccountDailyStatEntryService : IEmailAccountDailyStatEntryService
{
    private readonly IEmailAccountDailyStatEntryRepository _repository;

    public EmailAccountDailyStatEntryService(IEmailAccountDailyStatEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Dictionary<string, int>> GetSentEmailsAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var entries = await _repository.GetByEmailAccountIdAndDateRangeAsync(emailAccountId, startDate, endDate);
        return ConvertToDateDictionary(entries, e => e.Sent);
    }

    public async Task<Dictionary<string, int>> GetOpenedEmailsAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var entries = await _repository.GetByEmailAccountIdAndDateRangeAsync(emailAccountId, startDate, endDate);
        return ConvertToDateDictionary(entries, e => e.Opened);
    }


    public async Task<Dictionary<string, int>> GetRepliedEmailsAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var entries = await _repository.GetByEmailAccountIdAndDateRangeAsync(emailAccountId, startDate, endDate);
        return ConvertToDateDictionary(entries, e => e.Replied);
    }

    public async Task<Dictionary<string, int>> GetBouncedEmailsAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var entries = await _repository.GetByEmailAccountIdAndDateRangeAsync(emailAccountId, startDate, endDate);
        return ConvertToDateDictionary(entries, e => e.Bounced);
    }


    public async Task<Dictionary<string, int>> GetAllAccountsSentEmailsAsync(string adminUuid, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return AggregateByDate(allEntries, e => e.Sent);
    }

    public async Task<Dictionary<string, int>> GetAllAccountsOpenedEmailsAsync(string adminUuid, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return AggregateByDate(allEntries, e => e.Opened);
    }

    public async Task<Dictionary<string, int>> GetAllAccountsRepliedEmailsAsync(string adminUuid, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return AggregateByDate(allEntries, e => e.Replied);
    }

    public async Task<Dictionary<string, int>> GetAllAccountsBouncedEmailsAsync(string adminUuid, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return AggregateByDate(allEntries, e => e.Bounced);
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsSentEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate)
    {
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return GroupByAccountAndDate(allEntries, e => e.Sent);
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsOpenedEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate)
    {
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return GroupByAccountAndDate(allEntries, e => e.Opened);
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsRepliedEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate)
    {
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return GroupByAccountAndDate(allEntries, e => e.Replied);
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetAllAccountsBouncedEmailsAsync(string adminUuid, DateTime startDate, DateTime endDate)
    {
        var allEntries = await _repository.GetByAdminAndDateRangeAsync(adminUuid, startDate, endDate);
        return GroupByAccountAndDate(allEntries, e => e.Bounced);
    }

    public async Task UpdateDailyStatsAsync(long emailAccountId, string adminUuid, DateTime statDate, int sent, int opened, int replied, int bounced)
    {
        await _repository.UpsertStatsAsync(adminUuid, emailAccountId, statDate, sent, opened, replied, bounced);
    }

    public async Task<IEnumerable<EmailAccountDailyStatEntry>> GetStatEntriesAsync(long emailAccountId, DateTime startDate, DateTime endDate)
    {
        return await _repository.GetByEmailAccountIdAndDateRangeAsync(emailAccountId, startDate, endDate);
    }

    public async Task<EmailAccountDailyStatEntry?> GetAggregatedStatsAsync(long emailAccountId, DateTime startDate, DateTime endDate)
    {
        return await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
    }

    // Helper methods to convert relational data back to Dictionary format for backward compatibility

    private static Dictionary<string, int> ConvertToDateDictionary(IEnumerable<EmailAccountDailyStatEntry> entries, Func<EmailAccountDailyStatEntry, int> valueSelector)
    {
        return entries.ToDictionary(
            e => e.StatDate.ToString("yyyy-MM-dd"),
            valueSelector
        );
    }

    private static Dictionary<string, int> AggregateByDate(IEnumerable<EmailAccountDailyStatEntry> entries, Func<EmailAccountDailyStatEntry, int> valueSelector)
    {
        return entries
            .GroupBy(e => e.StatDate.ToString("yyyy-MM-dd"))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(valueSelector)
            );
    }

    private static Dictionary<string, Dictionary<string, int>> GroupByAccountAndDate(IEnumerable<EmailAccountDailyStatEntry> entries, Func<EmailAccountDailyStatEntry, int> valueSelector)
    {
        return entries
            .GroupBy(e => e.EmailAccountId.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    e => e.StatDate.ToString("yyyy-MM-dd"),
                    valueSelector
                )
            );
    }

    // ============ OPTIMIZED SQL AGGREGATION METHODS ============
    // These methods use direct SQL SUM() queries for better performance

    public async Task<int> GetTotalSentAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var result = await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
        return result?.Sent ?? 0;
    }

    public async Task<int> GetTotalOpenedAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var result = await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
        return result?.Opened ?? 0;
    }


    public async Task<int> GetTotalRepliedAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var result = await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
        return result?.Replied ?? 0;
    }


    public async Task<int> GetTotalBouncedAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        var result = await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
        return result?.Bounced ?? 0;
    }

    public async Task<EmailAccountDailyStatEntry?> GetTotalStatsAsync(long emailAccountId, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        return await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
    }

    public async Task<EmailAccountDailyStatEntry?> GetTotalStatsAsync(long emailAccountId, DateTime startDate, DateTime endDate)
    {
        return await _repository.GetAggregatedStatsByEmailAccountAsync(emailAccountId, startDate, endDate);
    }
    
    public async Task<Dictionary<long, EmailAccountDailyStatEntry>> GetBatchTotalStatsAsync(List<long> emailAccountIds, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);
        
        return await GetBatchTotalStatsAsync(emailAccountIds, startDate, endDate);
    }
    
    public async Task<Dictionary<long, EmailAccountDailyStatEntry>> GetBatchTotalStatsAsync(List<long> emailAccountIds, DateTime startDate, DateTime endDate)
    {
        if (!emailAccountIds.Any())
            return new Dictionary<long, EmailAccountDailyStatEntry>();
            
        // Use parallel individual calls for better performance than sequential
        var tasks = emailAccountIds.Select(async id =>
        {
            var stats = await _repository.GetAggregatedStatsByEmailAccountAsync(id, startDate, endDate);
            return new { Id = id, Stats = stats };
        });
        
        var results = await Task.WhenAll(tasks);
        
        return results
            .Where(r => r.Stats != null)
            .ToDictionary(r => r.Id, r => r.Stats!);
    }
}