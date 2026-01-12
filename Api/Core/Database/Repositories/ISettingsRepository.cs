using LeadHype.Api.Models;

namespace LeadHype.Api.Core.Database.Repositories;

public interface ISettingsRepository
{
    Task<IEnumerable<Setting>> GetAllAsync();
    Task<Setting?> GetByIdAsync(string id);
    Task<Setting?> GetByKeyAsync(string key);
    Task<string> CreateAsync(Setting setting);
    Task<bool> UpdateAsync(Setting setting);
    Task<bool> UpdateByKeyAsync(string key, string value);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteByKeyAsync(string key);
}