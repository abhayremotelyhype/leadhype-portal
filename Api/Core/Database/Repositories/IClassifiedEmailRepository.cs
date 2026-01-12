using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IClassifiedEmailRepository
{
    Task<ClassifiedEmailDbModel> CreateAsync(ClassifiedEmailDbModel classifiedEmail);
    Task<ClassifiedEmailDbModel> UpsertAsync(ClassifiedEmailDbModel classifiedEmail);
    Task<bool> UpdateAsync(ClassifiedEmailDbModel classifiedEmail);
    Task<ClassifiedEmailDbModel?> GetByMessageIdAsync(string messageId);
    Task<bool> ExistsByMessageIdAsync(string messageId);
    Task<bool> ExistsByEmailBodyHashAsync(string emailBodyHash);
    Task<List<ClassifiedEmailDbModel>> GetByCampaignIdAsync(int campaignId);
    Task<List<ClassifiedEmailDbModel>> GetByEmailAccountIdAsync(int emailAccountId);
    Task<List<ClassifiedEmailDbModel>> GetByLeadEmailAsync(string leadEmail);
    Task<int> GetTotalClassifiedCountAsync();
    Task<int> GetPositiveRepliesCountByEmailAccountAsync(int emailAccountId);
    Task<Dictionary<int, int>> GetPositiveRepliesCountForEmailAccountsAsync(List<int> emailAccountIds);
    Task<List<ClassifiedEmailDbModel>> GetRecentClassificationsAsync(int limit = 100);
}