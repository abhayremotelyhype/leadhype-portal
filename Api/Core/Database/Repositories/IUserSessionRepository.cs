using LeadHype.Api.Core.Models.Auth;

namespace LeadHype.Api.Core.Database.Repositories;

public interface IUserSessionRepository
{
    Task<IEnumerable<UserSession>> GetAllAsync();
    Task<UserSession?> GetByIdAsync(string id);
    Task<UserSession?> GetByRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<UserSession>> GetByUserIdAsync(string userId);
    Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(string userId);
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync();
    Task<string> CreateAsync(UserSession session);
    Task<bool> UpdateAsync(UserSession session);
    Task<bool> UpdateLastAccessedAsync(string id);
    Task<bool> DeactivateAsync(string id);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteByRefreshTokenAsync(string refreshToken);
    Task<bool> DeleteExpiredSessionsAsync();
    Task<bool> DeleteAllUserSessionsAsync(string userId);
}