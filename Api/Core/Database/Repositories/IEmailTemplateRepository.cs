using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IEmailTemplateRepository
{
    Task<IEnumerable<EmailTemplateDbModel>> GetByCampaignIdAsync(int campaignId);
    Task<Dictionary<EmailTemplateDbModel, List<EmailTemplateVariantDbModel>>> GetByCampaignIdWithVariantsAsync(int campaignId);
    Task<IEnumerable<EmailTemplateDbModel>> GetByAdminUuidAsync(string adminUuid);
    Task<string> CreateAsync(EmailTemplateDbModel template);
    Task<string> UpsertAsync(EmailTemplateDbModel template);
    Task<bool> UpdateAsync(EmailTemplateDbModel template);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteByCampaignIdAsync(int campaignId);
    Task<int> CountByCampaignIdAsync(int campaignId);

    // Variant methods
    Task<string> UpsertVariantAsync(EmailTemplateVariantDbModel variant);
    Task<bool> DeleteVariantsByTemplateIdAsync(string templateId);
}