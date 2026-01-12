using LeadHype.Api.Core.Database.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IEmailAccountRepository
{
    Task<IEnumerable<EmailAccountDbModel>> GetAllAsync();
    Task<EmailAccountDbModel?> GetByIdAsync(long id);
    Task<EmailAccountDbModel?> GetByEmailAsync(string email);
    Task<IEnumerable<EmailAccountDbModel>> GetByAdminUuidAsync(string adminUuid);
    Task<IEnumerable<EmailAccountDbModel>> GetByClientIdAsync(string clientId);
    Task<long> CreateAsync(EmailAccountDbModel emailAccount);
    Task<bool> UpdateAsync(EmailAccountDbModel emailAccount);
    Task<bool> DeleteAsync(long id);
    Task<int> CountAsync();
    Task<int> CountByStatusAsync(string status);
    
    // Paginated queries for performance
    Task<(IEnumerable<EmailAccountDbModel> accounts, int totalCount)> GetPaginatedAsync(
        int page, 
        int pageSize, 
        string? search = null, 
        List<string>? clientIds = null,
        List<long>? emailIds = null,
        string? sortBy = null, 
        bool sortDescending = false,
        int? timeRangeDays = null,
        string? sortMode = null,
        int? minSent = null,
        string? warmupStatus = null,
        int? performanceFilterMinSent = null,
        double? performanceFilterMaxReplyRate = null);
        
    Task<Dictionary<long, int>> GetCampaignCountsAsync(List<long> emailAccountIds);
    
    // Get accounts that need warmup stats update (older than specified minutes or never updated)
    Task<IEnumerable<EmailAccountDbModel>> GetAccountsNeedingWarmupUpdateAsync(string adminUuid, int minutesSinceLastUpdate = 1440);
}