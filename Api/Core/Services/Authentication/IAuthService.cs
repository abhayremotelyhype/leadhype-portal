using LeadHype.Api.Core.Models.Auth;

namespace LeadHype.Api.Services;

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null);
    Task<User?> CreateUserAsync(CreateUserRequest request);
    Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<User?> GetUserByIdAsync(string userId);
    Task<List<User>> GetUsersAsync();
    Task<bool> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<bool> ResetUserPasswordAsync(string userId, string newPassword);
    Task<string> GenerateApiKeyAsync(string userId);
    Task<List<UserSession>> GetUserSessionsAsync(string userId);
    Task<bool> RevokeSessionAsync(string sessionId);
    Task<bool> RevokeAllUserSessionsAsync(string userId);
}