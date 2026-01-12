using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiKeysController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeysController> _logger;

        public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        /// <summary>
        /// Get all API keys for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyApiKeys()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userId);
            return Ok(apiKeys);
        }

        /// <summary>
        /// Get a specific API key by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetApiKey(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var apiKey = await _apiKeyService.GetApiKeyAsync(id);
            
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Ensure user owns this API key or is admin
            if (apiKey.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            // Don't return the hash
            return Ok(new
            {
                apiKey.Id,
                apiKey.KeyPrefix,
                apiKey.Name,
                apiKey.Description,
                apiKey.Permissions,
                apiKey.RateLimit,
                apiKey.IsActive,
                apiKey.LastUsedAt,
                apiKey.ExpiresAt,
                apiKey.IpWhitelist,
                apiKey.CreatedAt,
                apiKey.UpdatedAt
            });
        }

        /// <summary>
        /// Create a new API key
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Validate permissions - users can't grant permissions they don't have
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin && request.Permissions.Contains(ApiPermissions.AdminAll))
            {
                return BadRequest(new { message = "You cannot grant admin permissions" });
            }

            // TODO: Temporarily bypass API key limit check due to mapping issues
            // Check if user has reached API key limit (e.g., 10 keys per user)
            // var existingKeys = await _apiKeyService.GetUserApiKeysAsync(userId);
            // if (existingKeys.Count() >= 10)
            // {
            //     return BadRequest(new { message = "API key limit reached. Please delete unused keys." });
            // }

            try
            {
                var apiKeyResponse = await _apiKeyService.CreateApiKeyAsync(userId, request);
                
                _logger.LogInformation($"API key created by user {userId}: {apiKeyResponse.KeyPrefix}");
                
                return CreatedAtAction(nameof(GetApiKey), new { id = apiKeyResponse.Id }, apiKeyResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return StatusCode(500, new { message = "Failed to create API key" });
            }
        }

        /// <summary>
        /// Update an existing API key
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApiKey(string id, [FromBody] CreateApiKeyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var apiKey = await _apiKeyService.GetApiKeyAsync(id);
            
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Ensure user owns this API key or is admin
            if (apiKey.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            // Validate permissions
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin && request.Permissions.Contains(ApiPermissions.AdminAll))
            {
                return BadRequest(new { message = "You cannot grant admin permissions" });
            }

            try
            {
                var result = await _apiKeyService.UpdateApiKeyAsync(id, request);
                
                if (result)
                {
                    _logger.LogInformation($"API key {id} updated by user {userId}");
                    return Ok(new { message = "API key updated successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to update API key" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating API key {id}");
                return StatusCode(500, new { message = "Failed to update API key" });
            }
        }

        /// <summary>
        /// Revoke (soft delete) an API key
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RevokeApiKey(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var apiKey = await _apiKeyService.GetApiKeyAsync(id);
            
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Ensure user owns this API key or is admin
            if (apiKey.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            try
            {
                var result = await _apiKeyService.RevokeApiKeyAsync(id);
                
                if (result)
                {
                    _logger.LogInformation($"API key {id} revoked by user {userId}");
                    return Ok(new { message = "API key revoked successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to revoke API key" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking API key {id}");
                return StatusCode(500, new { message = "Failed to revoke API key" });
            }
        }

        /// <summary>
        /// Get usage statistics for an API key
        /// </summary>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetApiKeyUsage(string id, [FromQuery] int limit = 100)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var apiKey = await _apiKeyService.GetApiKeyAsync(id);
            
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Ensure user owns this API key or is admin
            if (apiKey.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            var usage = await _apiKeyService.GetApiKeyUsageHistoryAsync(id, limit);
            
            return Ok(usage);
        }

        /// <summary>
        /// Get all available API permissions
        /// </summary>
        [HttpGet("permissions")]
        public IActionResult GetAvailablePermissions()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            var permissions = ApiPermissions.AllPermissions
                .Where(p => userRole == UserRoles.Admin || p != ApiPermissions.AdminAll)
                .Select(p => new
                {
                    permission = p,
                    description = ApiPermissions.PermissionDescriptions[p]
                })
                .ToList();

            return Ok(permissions);
        }
    }

    // Extension for IServiceCollection to register API key services
    public static class ApiKeyServiceExtensions
    {
        public static IServiceCollection AddApiKeyServices(this IServiceCollection services)
        {
            services.AddScoped<IApiKeyService, ApiKeyService>();
            services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
            
            return services;
        }
    }
}