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

namespace LeadHype.Api.Controllers.V1;

/// <summary>
/// Webhook management API for external users
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Authorize(AuthenticationSchemes = "ApiKey")]
[Produces("application/json")]
[Tags("Webhooks")]
public class WebhooksV1Controller : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksV1Controller> _logger;

    public WebhooksV1Controller(IWebhookService webhookService, ILogger<WebhooksV1Controller> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Get all webhooks for the current user
    /// </summary>
    /// <returns>List of webhooks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WebhookResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWebhooks()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check API key permissions
        var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
            (c.Value == ApiPermissions.ReadWebhooks || c.Value == ApiPermissions.AdminAll));
        
        if (!hasPermission)
        {
            return Forbid();
        }

        var webhooks = await _webhookService.GetUserWebhooksAsync(userId);
        return Ok(webhooks);
    }

    /// <summary>
    /// Get a specific webhook by ID
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <returns>Webhook details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWebhook(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check API key permissions
        var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
            (c.Value == ApiPermissions.ReadWebhooks || c.Value == ApiPermissions.AdminAll));
        
        if (!hasPermission)
        {
            return Forbid();
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
    /// Get delivery history for a webhook
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="limit">Maximum number of deliveries to return (default: 50, max: 100)</param>
    /// <returns>Webhook delivery history</returns>
    [HttpGet("{id}/deliveries")]
    [ProducesResponseType(typeof(WebhookDeliveryResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWebhookDeliveries(string id, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check API key permissions
        var hasPermission = User.Claims.Any(c => c.Type == "Permission" && 
            (c.Value == ApiPermissions.ReadWebhooks || c.Value == ApiPermissions.AdminAll));
        
        if (!hasPermission)
        {
            return Forbid();
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

        if (limit > 100) limit = 100; // Cap the limit
        if (offset < 0) offset = 0; // Ensure offset is not negative

        var deliveries = await _webhookService.GetWebhookDeliveriesAsync(id, limit, offset);
        return Ok(deliveries);
    }


}