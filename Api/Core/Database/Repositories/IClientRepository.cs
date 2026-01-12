using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IClientRepository
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(string id);
    Task<string> CreateAsync(Client client);
    Task<bool> UpdateAsync(Client client);
    Task<bool> DeleteAsync(string id);
    Task<int> CountAsync();
}