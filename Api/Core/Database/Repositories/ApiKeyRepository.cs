using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using LeadHype.Api.Core.Database;
using LeadHype.Api.Core.Models;
using Newtonsoft.Json;

namespace LeadHype.Api.Core.Database.Repositories
{
    public interface IApiKeyRepository
    {
        Task<ApiKey?> GetByIdAsync(string id);
        Task<ApiKey?> GetByKeyHashAsync(string keyHash);
        Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId);
        Task<string> CreateAsync(ApiKey apiKey);
        Task<bool> UpdateAsync(ApiKey apiKey);
        Task<bool> DeleteAsync(string id);
        Task<bool> LogUsageAsync(ApiKeyUsage usage);
        Task<bool> IsRateLimitExceededAsync(string apiKeyId, int limit, DateTime windowStart);
        Task<bool> IncrementRateLimitAsync(string apiKeyId, DateTime windowStart);
    }

    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly IDbConnectionService _connectionService;

        public ApiKeyRepository(IDbConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public async Task<ApiKey?> GetByIdAsync(string id)
        {
            const string sql = @"
                SELECT 
                    id, key_hash, key_prefix, user_id, name, description, 
                    permissions, rate_limit, is_active, last_used_at, 
                    expires_at, ip_whitelist, created_at, updated_at
                FROM api_keys
                WHERE id = @Id AND is_active = true";

            using var connection = await _connectionService.GetConnectionAsync();
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            
            return result != null ? MapToApiKey(result) : null;
        }

        public async Task<ApiKey?> GetByKeyHashAsync(string keyHash)
        {
            const string sql = @"
                SELECT 
                    id, key_hash, key_prefix, user_id, name, description, 
                    permissions, rate_limit, is_active, last_used_at, 
                    expires_at, ip_whitelist, created_at, updated_at
                FROM api_keys
                WHERE key_hash = @KeyHash AND is_active = true";

            using var connection = await _connectionService.GetConnectionAsync();
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { KeyHash = keyHash });
            
            return result != null ? MapToApiKey(result) : null;
        }

        public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId)
        {
            const string sql = @"
                SELECT 
                    id, key_hash, key_prefix, user_id, name, description, 
                    permissions, rate_limit, is_active, last_used_at, 
                    expires_at, ip_whitelist, created_at, updated_at
                FROM api_keys
                WHERE user_id = @UserId
                ORDER BY created_at DESC";

            using var connection = await _connectionService.GetConnectionAsync();
            var results = await connection.QueryAsync(sql, new { UserId = userId });
            
            return results.Select(MapToApiKey);
        }

        public async Task<string> CreateAsync(ApiKey apiKey)
        {
            apiKey.Id = string.IsNullOrEmpty(apiKey.Id) ? Guid.NewGuid().ToString() : apiKey.Id;
            apiKey.CreatedAt = DateTime.UtcNow;
            apiKey.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO api_keys (
                    id, key_hash, key_prefix, user_id, name, description,
                    permissions, rate_limit, is_active, expires_at,
                    ip_whitelist, created_at, updated_at
                )
                VALUES (
                    @Id, @KeyHash, @KeyPrefix, @UserId, @Name, @Description,
                    @Permissions::jsonb, @RateLimit, @IsActive, @ExpiresAt,
                    @IpWhitelist::jsonb, @CreatedAt, @UpdatedAt
                )
                RETURNING id";

            using var connection = await _connectionService.GetConnectionAsync();
            return await connection.QuerySingleAsync<string>(sql, new
            {
                apiKey.Id,
                apiKey.KeyHash,
                apiKey.KeyPrefix,
                apiKey.UserId,
                apiKey.Name,
                apiKey.Description,
                Permissions = JsonConvert.SerializeObject(apiKey.Permissions),
                apiKey.RateLimit,
                apiKey.IsActive,
                apiKey.ExpiresAt,
                IpWhitelist = JsonConvert.SerializeObject(apiKey.IpWhitelist),
                apiKey.CreatedAt,
                apiKey.UpdatedAt
            });
        }

        public async Task<bool> UpdateAsync(ApiKey apiKey)
        {
            apiKey.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE api_keys SET
                    name = @Name,
                    description = @Description,
                    permissions = @Permissions::jsonb,
                    rate_limit = @RateLimit,
                    is_active = @IsActive,
                    expires_at = @ExpiresAt,
                    ip_whitelist = @IpWhitelist::jsonb,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            using var connection = await _connectionService.GetConnectionAsync();
            var affected = await connection.ExecuteAsync(sql, new
            {
                apiKey.Id,
                apiKey.Name,
                apiKey.Description,
                Permissions = JsonConvert.SerializeObject(apiKey.Permissions),
                apiKey.RateLimit,
                apiKey.IsActive,
                apiKey.ExpiresAt,
                IpWhitelist = JsonConvert.SerializeObject(apiKey.IpWhitelist),
                apiKey.UpdatedAt
            });

            return affected > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            const string sql = "UPDATE api_keys SET is_active = false, updated_at = @UpdatedAt WHERE id = @Id";

            using var connection = await _connectionService.GetConnectionAsync();
            var affected = await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow });

            return affected > 0;
        }

        public async Task<bool> LogUsageAsync(ApiKeyUsage usage)
        {
            usage.Id = string.IsNullOrEmpty(usage.Id) ? Guid.NewGuid().ToString() : usage.Id;
            usage.CreatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO api_key_usage (
                    id, api_key_id, endpoint, method, status_code,
                    response_time_ms, ip_address, user_agent,
                    request_body_size, response_body_size, error_message,
                    created_at
                )
                VALUES (
                    @Id, @ApiKeyId, @Endpoint, @Method, @StatusCode,
                    @ResponseTimeMs, @IpAddress, @UserAgent,
                    @RequestBodySize, @ResponseBodySize, @ErrorMessage,
                    @CreatedAt
                )";

            using var connection = await _connectionService.GetConnectionAsync();
            var affected = await connection.ExecuteAsync(sql, usage);
            
            return affected > 0;
        }

        public async Task<bool> IsRateLimitExceededAsync(string apiKeyId, int limit, DateTime windowStart)
        {
            const string sql = @"
                SELECT COALESCE(request_count, 0)
                FROM rate_limits
                WHERE api_key_id = @ApiKeyId AND window_start = @WindowStart";

            using var connection = await _connectionService.GetConnectionAsync();
            var count = await connection.QuerySingleOrDefaultAsync<int>(sql, new { ApiKeyId = apiKeyId, WindowStart = windowStart });
            
            return count >= limit;
        }

        public async Task<bool> IncrementRateLimitAsync(string apiKeyId, DateTime windowStart)
        {
            const string sql = @"
                INSERT INTO rate_limits (id, api_key_id, window_start, request_count, created_at, updated_at)
                VALUES (@Id, @ApiKeyId, @WindowStart, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT (api_key_id, window_start)
                DO UPDATE SET 
                    request_count = rate_limits.request_count + 1,
                    updated_at = CURRENT_TIMESTAMP";

            using var connection = await _connectionService.GetConnectionAsync();
            var affected = await connection.ExecuteAsync(sql, new 
            { 
                Id = Guid.NewGuid().ToString(),
                ApiKeyId = apiKeyId, 
                WindowStart = windowStart 
            });
            
            return affected > 0;
        }

        private static ApiKey MapToApiKey(dynamic row)
        {
            return new ApiKey
            {
                Id = row.id?.ToString() ?? string.Empty,
                KeyHash = row.key_hash?.ToString() ?? string.Empty,
                KeyPrefix = row.key_prefix?.ToString() ?? string.Empty,
                UserId = row.user_id?.ToString() ?? string.Empty,
                Name = row.name?.ToString() ?? string.Empty,
                Description = row.description?.ToString(),
                Permissions = JsonConvert.DeserializeObject<List<string>>(row.permissions?.ToString() ?? "[]") ?? new List<string>(),
                RateLimit = Convert.ToInt32(row.rate_limit ?? 1000),
                IsActive = Convert.ToBoolean(row.is_active ?? true),
                LastUsedAt = row.last_used_at as DateTime?,
                ExpiresAt = row.expires_at as DateTime?,
                IpWhitelist = JsonConvert.DeserializeObject<List<string>>(row.ip_whitelist?.ToString() ?? "[]") ?? new List<string>(),
                CreatedAt = row.created_at as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.updated_at as DateTime? ?? DateTime.UtcNow
            };
        }
    }
}