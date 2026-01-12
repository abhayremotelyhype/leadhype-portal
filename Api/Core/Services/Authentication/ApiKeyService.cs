using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Services
{
    public interface IApiKeyService
    {
        Task<ApiKeyResponse> CreateApiKeyAsync(string userId, CreateApiKeyRequest request);
        Task<ApiKey?> ValidateApiKeyAsync(string apiKey);
        Task<IEnumerable<ApiKeyListResponse>> GetUserApiKeysAsync(string userId);
        Task<ApiKey?> GetApiKeyAsync(string id);
        Task<bool> UpdateApiKeyAsync(string id, CreateApiKeyRequest request);
        Task<bool> RevokeApiKeyAsync(string id);
        Task<bool> CheckRateLimitAsync(string apiKeyId, int? customLimit = null);
        Task LogApiUsageAsync(string apiKeyId, string endpoint, string method, int statusCode, int responseTimeMs, string? ipAddress = null, string? userAgent = null, string? errorMessage = null);
        Task<IEnumerable<ApiKeyUsage>> GetApiKeyUsageHistoryAsync(string apiKeyId, int limit = 100);
        string GenerateApiKey();
        string HashApiKey(string apiKey);
        bool VerifyApiKey(string apiKey, string hash);
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly ILogger<ApiKeyService> _logger;
        private const string API_KEY_PREFIX = "sk_live_";
        private const string API_KEY_TEST_PREFIX = "sk_test_";

        public ApiKeyService(IApiKeyRepository apiKeyRepository, ILogger<ApiKeyService> logger)
        {
            _apiKeyRepository = apiKeyRepository;
            _logger = logger;
        }

        public async Task<ApiKeyResponse> CreateApiKeyAsync(string userId, CreateApiKeyRequest request)
        {
            // Generate a new API key
            var apiKeyPlain = GenerateApiKey();
            var keyHash = HashApiKey(apiKeyPlain);
            var keyPrefix = apiKeyPlain.Substring(0, Math.Min(apiKeyPlain.Length, 8));

            var apiKey = new ApiKey
            {
                KeyHash = keyHash,
                KeyPrefix = keyPrefix,
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                Permissions = request.Permissions ?? new List<string>(),
                RateLimit = request.RateLimit ?? 10000,
                ExpiresAt = request.ExpiresAt,
                IpWhitelist = request.IpWhitelist ?? new List<string>()
            };

            var id = await _apiKeyRepository.CreateAsync(apiKey);
            apiKey.Id = id;

            _logger.LogInformation($"API key created for user {userId}: {keyPrefix}");

            return new ApiKeyResponse
            {
                Id = id,
                Key = apiKeyPlain, // Only shown once!
                KeyPrefix = keyPrefix,
                Name = apiKey.Name,
                Description = apiKey.Description,
                Permissions = apiKey.Permissions,
                RateLimit = apiKey.RateLimit,
                IsActive = apiKey.IsActive,
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt
            };
        }

        public async Task<ApiKey?> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return null;

            // Hash the provided key
            var keyHash = HashApiKey(apiKey);

            // Look up the key in the database
            var storedKey = await _apiKeyRepository.GetByKeyHashAsync(keyHash);
            
            if (storedKey == null)
            {
                _logger.LogWarning($"Invalid API key attempted: {apiKey.Substring(0, Math.Min(apiKey.Length, 8))}...");
                return null;
            }

            // Check if key is expired
            if (storedKey.ExpiresAt.HasValue && storedKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning($"Expired API key attempted: {storedKey.KeyPrefix}");
                return null;
            }

            // Update last used time
            storedKey.LastUsedAt = DateTime.UtcNow;
            await _apiKeyRepository.UpdateAsync(storedKey);

            _logger.LogInformation($"API key validated: {storedKey.KeyPrefix}");
            return storedKey;
        }

        public async Task<IEnumerable<ApiKeyListResponse>> GetUserApiKeysAsync(string userId)
        {
            var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId);
            
            return apiKeys.Select(key => new ApiKeyListResponse
            {
                Id = key.Id,
                KeyPrefix = key.KeyPrefix,
                Name = key.Name,
                Description = key.Description,
                Permissions = key.Permissions,
                RateLimit = key.RateLimit,
                IsActive = key.IsActive,
                LastUsedAt = key.LastUsedAt,
                ExpiresAt = key.ExpiresAt,
                CreatedAt = key.CreatedAt
            });
        }

        public async Task<ApiKey?> GetApiKeyAsync(string id)
        {
            return await _apiKeyRepository.GetByIdAsync(id);
        }

        public async Task<bool> UpdateApiKeyAsync(string id, CreateApiKeyRequest request)
        {
            var existingKey = await _apiKeyRepository.GetByIdAsync(id);
            if (existingKey == null)
                return false;

            existingKey.Name = request.Name;
            existingKey.Description = request.Description;
            existingKey.Permissions = request.Permissions ?? existingKey.Permissions;
            existingKey.RateLimit = request.RateLimit ?? existingKey.RateLimit;
            existingKey.ExpiresAt = request.ExpiresAt;
            existingKey.IpWhitelist = request.IpWhitelist ?? existingKey.IpWhitelist;

            return await _apiKeyRepository.UpdateAsync(existingKey);
        }

        public async Task<bool> RevokeApiKeyAsync(string id)
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);
            if (apiKey == null)
                return false;

            var result = await _apiKeyRepository.DeleteAsync(id);
            
            if (result)
            {
                _logger.LogInformation($"API key revoked: {apiKey.KeyPrefix}");
            }

            return result;
        }

        public async Task<bool> CheckRateLimitAsync(string apiKeyId, int? customLimit = null)
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId);
            if (apiKey == null)
            {
                _logger.LogWarning($"Rate limit check failed - API key not found: {apiKeyId}");
                return false;
            }

            var limit = customLimit ?? apiKey.RateLimit;
            var windowStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);

            var isExceeded = await _apiKeyRepository.IsRateLimitExceededAsync(apiKeyId, limit, windowStart);
            
            if (!isExceeded)
            {
                await _apiKeyRepository.IncrementRateLimitAsync(apiKeyId, windowStart);
            }

            return !isExceeded;
        }

        public async Task LogApiUsageAsync(string apiKeyId, string endpoint, string method, int statusCode, int responseTimeMs, 
            string? ipAddress = null, string? userAgent = null, string? errorMessage = null)
        {
            var usage = new ApiKeyUsage
            {
                ApiKeyId = apiKeyId,
                Endpoint = endpoint,
                Method = method,
                StatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ErrorMessage = errorMessage
            };

            await _apiKeyRepository.LogUsageAsync(usage);
        }

        public async Task<IEnumerable<ApiKeyUsage>> GetApiKeyUsageHistoryAsync(string apiKeyId, int limit = 100)
        {
            // This would need to be implemented in the repository
            return new List<ApiKeyUsage>();
        }

        public string GenerateApiKey()
        {
            // Generate a cryptographically secure random API key
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Convert to base64 and make URL-safe
            var key = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            return key;
        }

        public string HashApiKey(string apiKey)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
                return Convert.ToBase64String(hashBytes);
            }
        }

        public bool VerifyApiKey(string apiKey, string hash)
        {
            var computedHash = HashApiKey(apiKey);
            return computedHash == hash;
        }
    }
}