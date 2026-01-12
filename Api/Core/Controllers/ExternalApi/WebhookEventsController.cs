using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.Database.WebhookEvent;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace LeadHype.Api.Controllers;

[ApiController]
[Route("api/webhook-events")]
[Authorize]
public class WebhookEventsController : ControllerBase
{
    private readonly IWebhookEventConfigRepository _eventConfigRepository;
    private readonly IWebhookEventTriggerRepository _eventTriggerRepository;
    private readonly IWebhookRepository _webhookRepository;
    private readonly ILogger<WebhookEventsController> _logger;

    public WebhookEventsController(
        IWebhookEventConfigRepository eventConfigRepository,
        IWebhookEventTriggerRepository eventTriggerRepository,
        IWebhookRepository webhookRepository,
        ILogger<WebhookEventsController> logger)
    {
        _eventConfigRepository = eventConfigRepository;
        _eventTriggerRepository = eventTriggerRepository;
        _webhookRepository = webhookRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WebhookEventConfigResponse>>> GetEventConfigs()
    {
        try
        {
            var adminUuid = GetAdminUuid();
            var configs = await _eventConfigRepository.GetByAdminAsync(adminUuid);

            // Filter out admin-only event types for non-admin users
            if (!IsAdmin())
            {
                configs = configs.Where(c => !IsAdminOnlyEventType(c.EventType)).ToList();
            }

            var response = configs.Select(MapToResponse).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook event configs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WebhookEventConfigResponse>> GetEventConfig(string id)
    {
        try
        {
            var config = await _eventConfigRepository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(new { message = "Event configuration not found" });
            }

            var adminUuid = GetAdminUuid();
            if (config.AdminUuid != adminUuid)
            {
                return Forbid();
            }

            // Check if user has permission to view admin-only event types
            if (IsAdminOnlyEventType(config.EventType) && !IsAdmin())
            {
                return Forbid();
            }

            return Ok(MapToResponse(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook event config {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<WebhookEventConfigResponse>> CreateEventConfig([FromBody] CreateWebhookEventConfigRequest request)
    {
        try
        {
            var adminUuid = GetAdminUuid();

            // Verify webhook exists and belongs to user
            var webhook = await _webhookRepository.GetByIdAsync(request.WebhookId);
            if (webhook == null || webhook.UserId != adminUuid)
            {
                return BadRequest(new { message = "Invalid webhook ID" });
            }

            // Validate event type
            try
            {
                WebhookEventTypeExtensions.FromEventString(request.EventType);
            }
            catch (ArgumentException)
            {
                var supportedTypes = string.Join(", ", WebhookEventTypeExtensions.GetAllEventTypes().Select(et => $"'{et.Type}'"));
                return BadRequest(new { message = $"Invalid event type. Supported types: {supportedTypes}" });
            }

            // Check if user has permission to create admin-only event types
            if (IsAdminOnlyEventType(request.EventType) && !IsAdmin())
            {
                return Forbid();
            }

            // Validate config parameters for both event types
            if (request.EventType == "reply_rate_drop" || request.EventType == "bounce_rate_high")
            {
                if (!request.ConfigParameters.ContainsKey("thresholdPercent") ||
                    !request.ConfigParameters.ContainsKey("monitoringPeriodDays"))
                {
                    return BadRequest(new { message = "Missing required parameters: thresholdPercent, monitoringPeriodDays" });
                }

                var thresholdObj = request.ConfigParameters["thresholdPercent"];
                var periodObj = request.ConfigParameters["monitoringPeriodDays"];
                
                double threshold;
                int period;
                
                // Handle JsonElement conversion for System.Text.Json
                if (thresholdObj is System.Text.Json.JsonElement thresholdElement)
                {
                    threshold = thresholdElement.GetDouble();
                }
                else
                {
                    threshold = Convert.ToDouble(thresholdObj);
                }
                
                if (periodObj is System.Text.Json.JsonElement periodElement)
                {
                    period = periodElement.GetInt32();
                }
                else
                {
                    period = Convert.ToInt32(periodObj);
                }

                if (threshold <= 0 || threshold > 100)
                {
                    return BadRequest(new { message = "thresholdPercent must be greater than 0 and less than or equal to 100" });
                }

                if (period < 1 || period > 30)
                {
                    return BadRequest(new { message = "monitoringPeriodDays must be between 1 and 30" });
                }
            }
            else if (request.EventType == "no_positive_reply_for_x_days" || request.EventType == "no_reply_for_x_days")
            {
                if (!request.ConfigParameters.ContainsKey("daysSinceLastReply"))
                {
                    return BadRequest(new { message = "Missing required parameter: daysSinceLastReply" });
                }

                var daysObj = request.ConfigParameters["daysSinceLastReply"];
                int days;
                
                // Handle JsonElement conversion for System.Text.Json
                if (daysObj is System.Text.Json.JsonElement daysElement)
                {
                    days = daysElement.GetInt32();
                }
                else
                {
                    days = Convert.ToInt32(daysObj);
                }

                if (days < 1 || days > 365)
                {
                    return BadRequest(new { message = "daysSinceLastReply must be between 1 and 365" });
                }
            }

            // Validate target scope
            if (request.TargetScope?.Type != "clients" && request.TargetScope?.Type != "campaigns")
            {
                return BadRequest(new { message = "TargetScope type must be 'clients' or 'campaigns'" });
            }

            if (request.TargetScope?.Ids == null || !request.TargetScope.Ids.Any())
            {
                return BadRequest(new { message = "TargetScope must include at least one ID" });
            }

            // Convert JsonElement objects to proper values before serialization
            var configParams = new Dictionary<string, object>();
            foreach (var param in request.ConfigParameters)
            {
                if (param.Value is System.Text.Json.JsonElement jsonElement)
                {
                    configParams[param.Key] = ConvertJsonElementToObject(jsonElement);
                }
                else
                {
                    configParams[param.Key] = param.Value;
                }
            }

            var config = new WebhookEventConfig
            {
                AdminUuid = adminUuid,
                WebhookId = request.WebhookId,
                EventType = request.EventType,
                Name = request.Name,
                Description = request.Description,
                ConfigParameters = JsonConvert.SerializeObject(configParams),
                TargetScope = JsonConvert.SerializeObject(request.TargetScope)
            };

            var createdConfig = await _eventConfigRepository.CreateAsync(config);
            return CreatedAtAction(nameof(GetEventConfig), new { id = createdConfig.Id }, MapToResponse(createdConfig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook event config");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WebhookEventConfigResponse>> UpdateEventConfig(string id, [FromBody] UpdateWebhookEventConfigRequest request)
    {
        try
        {
            var config = await _eventConfigRepository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(new { message = "Event configuration not found" });
            }

            var adminUuid = GetAdminUuid();
            if (config.AdminUuid != adminUuid)
            {
                return Forbid();
            }

            // Check if user has permission to update admin-only event types
            if (IsAdminOnlyEventType(config.EventType) && !IsAdmin())
            {
                return Forbid();
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(request.Name))
                config.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                config.Description = request.Description;

            if (request.ConfigParameters != null)
            {
                // Validate parameters for both event types
                if (config.EventType == "reply_rate_drop" || config.EventType == "bounce_rate_high")
                {
                    if (request.ConfigParameters.ContainsKey("thresholdPercent"))
                    {
                        var thresholdObj = request.ConfigParameters["thresholdPercent"];
                        double threshold;
                        if (thresholdObj is System.Text.Json.JsonElement thresholdElement)
                        {
                            threshold = thresholdElement.GetDouble();
                        }
                        else
                        {
                            threshold = Convert.ToDouble(thresholdObj);
                        }
                        
                        if (threshold <= 0 || threshold > 100)
                        {
                            return BadRequest(new { message = "thresholdPercent must be greater than 0 and less than or equal to 100" });
                        }
                    }

                    if (request.ConfigParameters.ContainsKey("monitoringPeriodDays"))
                    {
                        var periodObj = request.ConfigParameters["monitoringPeriodDays"];
                        int period;
                        if (periodObj is System.Text.Json.JsonElement periodElement)
                        {
                            period = periodElement.GetInt32();
                        }
                        else
                        {
                            period = Convert.ToInt32(periodObj);
                        }
                        
                        if (period < 1 || period > 30)
                        {
                            return BadRequest(new { message = "monitoringPeriodDays must be between 1 and 30" });
                        }
                    }
                }

                // Convert JsonElement objects to proper values before serialization
                var configParams = new Dictionary<string, object>();
                foreach (var param in request.ConfigParameters)
                {
                    if (param.Value is System.Text.Json.JsonElement jsonElement)
                    {
                        configParams[param.Key] = ConvertJsonElementToObject(jsonElement);
                    }
                    else
                    {
                        configParams[param.Key] = param.Value;
                    }
                }
                config.ConfigParameters = JsonConvert.SerializeObject(configParams);
            }

            if (request.TargetScope != null)
            {
                if (request.TargetScope.Type != "clients" && request.TargetScope.Type != "campaigns")
                {
                    return BadRequest(new { message = "TargetScope type must be 'clients' or 'campaigns'" });
                }

                if (!request.TargetScope.Ids.Any())
                {
                    return BadRequest(new { message = "TargetScope must include at least one ID" });
                }

                config.TargetScope = JsonConvert.SerializeObject(request.TargetScope);
            }

            if (request.IsActive.HasValue)
                config.IsActive = request.IsActive.Value;

            var updated = await _eventConfigRepository.UpdateAsync(config);
            if (!updated)
            {
                return StatusCode(500, new { message = "Failed to update event configuration" });
            }

            return Ok(MapToResponse(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook event config {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEventConfig(string id)
    {
        try
        {
            var config = await _eventConfigRepository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(new { message = "Event configuration not found" });
            }

            var adminUuid = GetAdminUuid();
            if (config.AdminUuid != adminUuid)
            {
                return Forbid();
            }

            // Check if user has permission to delete admin-only event types
            if (IsAdminOnlyEventType(config.EventType) && !IsAdmin())
            {
                return Forbid();
            }

            var deleted = await _eventConfigRepository.DeleteAsync(id);
            if (!deleted)
            {
                return StatusCode(500, new { message = "Failed to delete event configuration" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook event config {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}/triggers")]
    public async Task<ActionResult<IEnumerable<WebhookEventTrigger>>> GetEventTriggers(string id, [FromQuery] int limit = 50)
    {
        try
        {
            var config = await _eventConfigRepository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(new { message = "Event configuration not found" });
            }

            var adminUuid = GetAdminUuid();
            if (config.AdminUuid != adminUuid)
            {
                return Forbid();
            }

            // Check if user has permission to view triggers for admin-only event types
            if (IsAdminOnlyEventType(config.EventType) && !IsAdmin())
            {
                return Forbid();
            }

            var triggers = await _eventTriggerRepository.GetByEventConfigIdAsync(id, Math.Min(limit, 100));
            return Ok(triggers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook event triggers for config {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("triggers/recent")]
    public async Task<ActionResult<IEnumerable<WebhookEventTrigger>>> GetRecentTriggers([FromQuery] int limit = 20)
    {
        try
        {
            var adminUuid = GetAdminUuid();
            var triggers = await _eventTriggerRepository.GetRecentTriggersAsync(adminUuid, Math.Min(limit, 50));
            return Ok(triggers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent webhook event triggers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("event-types")]
    public ActionResult<IEnumerable<WebhookEventTypeInfo>> GetAvailableEventTypes()
    {
        var eventTypes = WebhookEventTypeExtensions.GetAllEventTypes();
        
        // Filter out admin-only event types for non-admin users
        if (!IsAdmin())
        {
            eventTypes = eventTypes.Where(et => !IsAdminOnlyEventType(et.Type));
        }
        
        return Ok(eventTypes);
    }

    private string GetAdminUuid()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               throw new UnauthorizedAccessException("User not authenticated");
    }

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? UserRoles.User;
    }

    private bool IsAdmin()
    {
        return GetUserRole() == UserRoles.Admin;
    }

    private bool IsAdminOnlyEventType(string eventType)
    {
        return eventType == "campaign.created";
    }

    private static object ConvertJsonElementToObject(System.Text.Json.JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => jsonElement.GetString()!,
            System.Text.Json.JsonValueKind.Number => jsonElement.TryGetInt32(out var intValue) ? intValue : jsonElement.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null!,
            System.Text.Json.JsonValueKind.Array => jsonElement.EnumerateArray().Select(ConvertJsonElementToObject).ToArray(),
            System.Text.Json.JsonValueKind.Object => jsonElement.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElementToObject(p.Value)),
            _ => jsonElement.GetRawText()
        };
    }

    private WebhookEventConfigResponse MapToResponse(WebhookEventConfig config)
    {
        var configParams = new Dictionary<string, object>();
        var targetScope = new TargetScopeConfig();

        try
        {
            if (!string.IsNullOrEmpty(config.ConfigParameters))
                configParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(config.ConfigParameters) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize config parameters for {ConfigId}", config.Id);
        }

        try
        {
            if (!string.IsNullOrEmpty(config.TargetScope))
                targetScope = JsonConvert.DeserializeObject<TargetScopeConfig>(config.TargetScope) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize target scope for {ConfigId}", config.Id);
        }

        return new WebhookEventConfigResponse
        {
            Id = config.Id,
            WebhookId = config.WebhookId,
            EventType = config.EventType,
            Name = config.Name,
            Description = config.Description,
            ConfigParameters = configParams,
            TargetScope = targetScope,
            IsActive = config.IsActive,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            LastCheckedAt = config.LastCheckedAt,
            LastTriggeredAt = config.LastTriggeredAt
        };
    }
}