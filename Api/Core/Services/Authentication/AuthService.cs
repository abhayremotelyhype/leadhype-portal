using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.Auth;
using Microsoft.IdentityModel.Tokens;

namespace LeadHype.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly IApiKeyService _apiKeyService;

    public AuthService(
        IUserRepository userRepository, 
        IUserSessionRepository userSessionRepository, 
        JwtSettings jwtSettings,
        IApiKeyService apiKeyService)
    {
        _userRepository = userRepository;
        _userSessionRepository = userSessionRepository;
        _jwtSettings = jwtSettings;
        _tokenHandler = new JwtSecurityTokenHandler();
        _apiKeyService = apiKeyService;
    }

    public async Task<AuthResult> AuthenticateAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null)
    {
        // Get user by email
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResult
            {
                Success = false,
                ErrorType = AuthErrorType.InvalidCredentials,
                ErrorMessage = "Invalid email or password"
            };
        }

        if (!user.IsActive)
        {
            return new AuthResult
            {
                Success = false,
                ErrorType = AuthErrorType.AccountInactive,
                ErrorMessage = "Your account has been deactivated. Please contact an administrator."
            };
        }

        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // Create new session
        var session = new UserSession
        {
            UserId = user.Id,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceName = ParseDeviceName(userAgent)
        };
        
        await _userSessionRepository.CreateAsync(session);

        // Update user last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var loginResponse = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            RefreshTokenExpiresAt = session.RefreshTokenExpiryTime,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                AssignedClientIds = user.AssignedClientIds,
                LastLoginAt = user.LastLoginAt,
                ApiKey = user.ApiKey,
                ApiKeyCreatedAt = user.ApiKeyCreatedAt
            }
        };

        return new AuthResult
        {
            Success = true,
            LoginResponse = loginResponse
        };
    }

    public async Task<User?> CreateUserAsync(CreateUserRequest request)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email.ToLower());
        if (existingUser != null) return null;

        // Create new user
        var user = new User
        {
            Email = request.Email.ToLower(),
            Username = request.Username?.ToLower() ?? request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            AssignedClientIds = request.AssignedClientIds ?? new List<string>()
        };

        await _userRepository.CreateAsync(user);
        return user;
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        // Find session by refresh token
        var session = await _userSessionRepository.GetByRefreshTokenAsync(refreshToken);
        if (session == null) return null;

        // Get user
        var user = await _userRepository.GetByIdAsync(session.UserId);
        if (user == null || !user.IsActive) return null;

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        // Update session
        session.RefreshToken = newRefreshToken;
        session.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
        session.LastAccessedAt = DateTime.UtcNow;

        await _userSessionRepository.UpdateAsync(session);

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            RefreshTokenExpiresAt = session.RefreshTokenExpiryTime,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                AssignedClientIds = user.AssignedClientIds,
                LastLoginAt = user.LastLoginAt,
                ApiKey = user.ApiKey,
                ApiKeyCreatedAt = user.ApiKeyCreatedAt
            }
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        return await _userSessionRepository.DeleteByRefreshTokenAsync(refreshToken);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.ToList();
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        // Update user properties
        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email.ToLower();
        if (!string.IsNullOrEmpty(request.Username))
            user.Username = request.Username.ToLower();
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.Role))
            user.Role = request.Role;
        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;
        if (request.AssignedClientIds != null)
            user.AssignedClientIds = request.AssignedClientIds;

        return await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        // Delete all user sessions first
        await _userSessionRepository.DeleteAllUserSessionsAsync(userId);
        
        // Delete user
        return await _userRepository.DeleteAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return false;

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        return await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> ResetUserPasswordAsync(string userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        return await _userRepository.UpdateAsync(user);
    }

    public async Task<string> GenerateApiKeyAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new ArgumentException("User not found", nameof(userId));

        // If user already has an API key, we should revoke the old one first
        if (!string.IsNullOrEmpty(user.ApiKey))
        {
            try
            {
                await _apiKeyService.RevokeApiKeyAsync(user.ApiKey);
            }
            catch
            {
                // If revoking fails, continue with creating new key
                // This handles cases where the old key might not exist in the new system
            }
        }

        // Use the new ApiKeyService to create the API key
        var permissions = new List<string>();
        
        // Assign permissions based on user role
        if (user.Role == UserRoles.Admin)
        {
            permissions.Add(Core.Models.ApiPermissions.AdminAll);
        }
        else
        {
            // Regular users get basic read permissions
            permissions.AddRange(new[]
            {
                Core.Models.ApiPermissions.ReadCampaigns,
                Core.Models.ApiPermissions.ReadEmails,
                Core.Models.ApiPermissions.ReadLeads,
                Core.Models.ApiPermissions.ReadClients
            });
        }
        
        var request = new Core.Models.CreateApiKeyRequest
        {
            Name = "Generated API Key",
            Description = "Generated from settings page",
            Permissions = permissions
        };

        var result = await _apiKeyService.CreateApiKeyAsync(userId, request);
        
        // Update the user record to store the API key for easy access
        user.ApiKey = result.Key;
        user.ApiKeyCreatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        
        return result.Key;
    }


    public async Task<List<UserSession>> GetUserSessionsAsync(string userId)
    {
        var sessions = await _userSessionRepository.GetByUserIdAsync(userId);
        return sessions.ToList();
    }

    public async Task<bool> RevokeSessionAsync(string sessionId)
    {
        return await _userSessionRepository.DeleteAsync(sessionId);
    }

    public async Task<bool> RevokeAllUserSessionsAsync(string userId)
    {
        return await _userSessionRepository.DeleteAllUserSessionsAsync(userId);
    }

    #region Private Helper Methods

    private string GenerateAccessToken(User user)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("IsActive", user.IsActive.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string GenerateSecureApiKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string? ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return null;

        // Check for specific mobile devices first
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            return "iPhone";
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "iPad";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android Device";

        // Check for desktop operating systems
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "Windows PC";
        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
            return "Mac";
        if (userAgent.Contains("X11", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            return "Linux PC";

        // Fallback to browser detection if OS detection fails
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            return "Mobile Device";
        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            return "Tablet";

        return "Unknown Device";
    }

    #endregion
}

// These classes are already defined elsewhere