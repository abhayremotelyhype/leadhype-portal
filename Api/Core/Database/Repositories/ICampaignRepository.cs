using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface ICampaignRepository
{
    Task<IEnumerable<CampaignDetailsDbModel>> GetAllAsync();
    Task<CampaignDetailsDbModel?> GetByIdAsync(string id);
    Task<IEnumerable<CampaignDetailsDbModel>> GetByCampaignIdAsync(int campaignId);
    Task<IEnumerable<CampaignDetailsDbModel>> GetByAdminUuidAsync(string adminUuid);
    Task<IEnumerable<CampaignDetailsDbModel>> GetByClientIdAsync(string clientId);
    Task<string> CreateAsync(CampaignDetailsDbModel campaign);
    Task<bool> UpdateAsync(CampaignDetailsDbModel campaign);
    Task<bool> DeleteAsync(string id);
    Task<int> CountAsync();
    Task<int> CountByStatusAsync(string status);
}