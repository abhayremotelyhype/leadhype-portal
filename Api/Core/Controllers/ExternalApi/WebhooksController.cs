using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Controllers
{
    /// <summary>
    /// Webhook management API
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        /// <summary>
        /// Get all webhooks for the current user
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(WebhookResponse[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWebhooks()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.ReadWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            var webhooks = await _webhookService.GetUserWebhooksAsync(userId);
            return Ok(webhooks);
        }

        /// <summary>
        /// Get a specific webhook by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetWebhook(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.ReadWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            var webhook = await _webhookService.GetWebhookAsync(id);
            
            if (webhook == null)
            {
                return NotFound(new { message = "Webhook not found" });
            }

            // Ensure user owns this webhook or is admin
            if (webhook.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            var response = new WebhookResponse
            {
                Id = webhook.Id,
                Name = webhook.Name,
                Url = webhook.Url,
                Headers = webhook.Headers,
                IsActive = webhook.IsActive,
                RetryCount = webhook.RetryCount,
                TimeoutSeconds = webhook.TimeoutSeconds,
                LastTriggeredAt = webhook.LastTriggeredAt,
                FailureCount = webhook.FailureCount,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new webhook
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest request)
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

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.WriteWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            try
            {
                var webhookId = await _webhookService.CreateWebhookAsync(userId, request);
                
                _logger.LogInformation($"Webhook created by user {userId}: {webhookId}");
                
                return CreatedAtAction(nameof(GetWebhook), new { id = webhookId }, new { id = webhookId, message = "Webhook created successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating webhook");
                return StatusCode(500, new { message = "Failed to create webhook" });
            }
        }

        /// <summary>
        /// Update an existing webhook
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateWebhook(string id, [FromBody] UpdateWebhookRequest request)
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

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.WriteWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            var webhook = await _webhookService.GetWebhookAsync(id);
            
            if (webhook == null)
            {
                return NotFound(new { message = "Webhook not found" });
            }

            // Ensure user owns this webhook or is admin
            if (webhook.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            try
            {
                var result = await _webhookService.UpdateWebhookAsync(id, request);
                
                if (result)
                {
                    _logger.LogInformation($"Webhook {id} updated by user {userId}");
                    return Ok(new { message = "Webhook updated successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to update webhook" });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating webhook {id}");
                return StatusCode(500, new { message = "Failed to update webhook" });
            }
        }

        /// <summary>
        /// Delete a webhook
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteWebhook(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.DeleteWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            var webhook = await _webhookService.GetWebhookAsync(id);
            
            if (webhook == null)
            {
                return NotFound(new { message = "Webhook not found" });
            }

            // Ensure user owns this webhook or is admin
            if (webhook.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            try
            {
                var result = await _webhookService.DeleteWebhookAsync(id);
                
                if (result)
                {
                    _logger.LogInformation($"Webhook {id} deleted by user {userId}");
                    return Ok(new { message = "Webhook deleted successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to delete webhook" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting webhook {id}");
                return StatusCode(500, new { message = "Failed to delete webhook" });
            }
        }

        /// <summary>
        /// Get delivery history for a webhook
        /// </summary>
        [HttpGet("{id}/deliveries")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetWebhookDeliveries(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery] bool? failuresOnly = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.ReadWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            var webhook = await _webhookService.GetWebhookAsync(id);
            
            if (webhook == null)
            {
                return NotFound(new { message = "Webhook not found" });
            }

            // Ensure user owns this webhook or is admin
            if (webhook.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            // Support both pagination styles for backward compatibility
            // If page/pageSize are provided, use them; otherwise use limit/offset
            int actualLimit, actualOffset;
            if (page > 0 && pageSize > 0)
            {
                actualLimit = Math.Min(pageSize, 100); // Cap the page size
                actualOffset = (page - 1) * actualLimit;
            }
            else
            {
                actualLimit = Math.Min(limit, 100); // Cap the limit
                actualOffset = Math.Max(offset, 0); // Ensure offset is not negative
            }

            var (deliveries, totalCount) = await _webhookService.GetWebhookDeliveriesWithCountAsync(id, actualLimit, actualOffset, failuresOnly);
            
            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / actualLimit);
            var currentPage = (actualOffset / actualLimit) + 1;
            
            return Ok(new
            {
                data = deliveries,
                pagination = new
                {
                    currentPage = currentPage,
                    pageSize = actualLimit,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNext = currentPage < totalPages,
                    hasPrevious = currentPage > 1
                }
            });
        }

        /// <summary>
        /// Test a webhook by sending a test event
        /// </summary>
        [HttpPost("{id}/test")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TestWebhook(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check API key permissions
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.WriteWebhooks || c.Value == ApiPermissions.AdminAll));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            var webhook = await _webhookService.GetWebhookAsync(id);
            
            if (webhook == null)
            {
                return NotFound(new { message = "Webhook not found" });
            }

            // Ensure user owns this webhook or is admin
            if (webhook.UserId != userId && User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.Admin)
            {
                return Forbid();
            }

            try
            {
                await _webhookService.TestWebhookAsync(id);
                _logger.LogInformation($"Test webhook sent for {id} by user {userId}");
                return Ok(new { message = "Test webhook sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing webhook {id}");
                return StatusCode(500, new { message = "Failed to send test webhook" });
            }
        }

    }
}