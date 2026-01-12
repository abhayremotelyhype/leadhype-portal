using LeadHype.Api.Core.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IEmailAccountStatsDateRepository
{
    Task<EmailAccountStatsDate?> GetByDateAsync(string adminUuid, DateTime dateTime);
    Task<IEnumerable<EmailAccountStatsDate>> GetByAdminUuidAsync(string adminUuid);
    Task CreateAsync(EmailAccountStatsDate statsDate);
    Task<bool> ExistsAsync(string adminUuid, DateTime dateTime);
}