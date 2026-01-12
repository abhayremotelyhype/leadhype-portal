using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface ILeadConversationRepository
{
    Task<IEnumerable<LeadConversationDbModel>> GetByCampaignIdAsync(int campaignId);
    Task<IEnumerable<LeadConversationDbModel>> GetByCampaignIdAsync(int campaignId, bool withRepliesOnly);
    Task<IEnumerable<LeadConversationDbModel>> GetByAdminUuidAsync(string adminUuid);
    Task<LeadConversationDbModel?> GetByLeadEmailAsync(int campaignId, string leadEmail);
    Task<string> CreateAsync(LeadConversationDbModel conversation);
    Task<bool> UpdateAsync(LeadConversationDbModel conversation);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteByCampaignIdAsync(int campaignId);
    Task<int> CountByCampaignIdAsync(int campaignId);
}