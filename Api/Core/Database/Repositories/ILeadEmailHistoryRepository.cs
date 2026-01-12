using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface ILeadEmailHistoryRepository
{
    Task<LeadEmailHistoryDbModel> CreateAsync(LeadEmailHistoryDbModel leadEmailHistory);
    Task<bool> UpdateAsync(LeadEmailHistoryDbModel leadEmailHistory);
    Task<List<LeadEmailHistoryDbModel>> GetByCampaignIdAsync(int campaignId);
    Task<List<LeadEmailHistoryDbModel>> GetByLeadIdAsync(string leadId);
    Task<List<LeadEmailHistoryDbModel>> GetByCampaignAndLeadIdAsync(int campaignId, string leadId);
    Task<Dictionary<string, int>> GetMessageCountsByCampaignIdAsync(int campaignId);
    Task<int> GetTotalMessageCountByCampaignIdAsync(int campaignId);
    Task<bool> DeleteByCampaignIdAsync(int campaignId);
    Task<bool> DeleteByLeadIdAsync(string leadId);
    Task<bool> DeleteByCampaignAndLeadIdAsync(int campaignId, string leadId);
    Task<DateTime?> GetLastContactedDateForCampaignsAsync(List<int> campaignIds);
}