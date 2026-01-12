using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Controllers.V1
{
    /// <summary>
    /// Campaign Inbox Replies API v1 - RESTful API for fetching campaign replied emails
    /// </summary>
    [ApiController]
    [Route("api/v1/campaigns")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Tags("Campaigns")]
    public class MasterInboxV1Controller : ControllerBase
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<MasterInboxV1Controller> _logger;

        public MasterInboxV1Controller(
            ICampaignRepository campaignRepository,
            IAuthService authService,
            ILogger<MasterInboxV1Controller> logger)
        {
            _campaignRepository = campaignRepository;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Get replied emails from campaigns
        /// </summary>
        /// <param name="requestBody">Request containing offset, limit, and campaign IDs</param>
        /// <returns>Replied emails data from specified campaigns</returns>
        [HttpPost("inbox-replies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInboxReplies([FromBody] InboxRepliesRequest requestBody)
        {
            try
            {
                // Get user permissions
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isApiKey = User.FindFirst("AuthMethod")?.Value == "ApiKey";

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Invalid authentication" });
                }

                // Check API key permissions - for now, allow any authenticated API key user
                if (isApiKey)
                {
                    var hasPermission = User.Claims.Any(c => c.Type == "Permission");
                    
                    if (!hasPermission)
                    {
                        return Forbid();
                    }
                }

                // Use static Smartlead API key
                const string staticSmartleadApiKey = "c38e38e8-d7b6-4c00-836d-3437678ef4d9_dtw3zft";
                var smartleadService = new SmartleadApiService(staticSmartleadApiKey);
                
                // Create filters with fixed email status as 'Replied'
                var filters = new
                {
                    emailStatus = new[] { "Replied" },
                    campaignId = requestBody.CampaignIds
                };

                if(requestBody.Limit > 20)
                    requestBody.Limit = 20;
                
                var result = smartleadService.FetchMasterInboxReplies(
                    requestBody.Offset,
                    requestBody.Limit,
                    filters
                );

                if (result == null)
                {
                    return StatusCode(500, new { 
                        success = false,
                        message = "Failed to fetch inbox replies from Smartlead API" 
                    });
                }

                // Parse the JSON response to clean up email_body content
                try
                {
                    var jsonDocument = JsonDocument.Parse(result.ToString());
                    var cleanedJson = RemoveSmartleadFromEmailBodies(jsonDocument.RootElement);
                    return Content(cleanedJson, "application/json");
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, return the original response
                    return Content(result.ToString(), "application/json");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching master inbox replies");
                return StatusCode(500, new { success = false, message = "An error occurred while fetching inbox replies" });
            }
        }

        private string RemoveSmartleadFromEmailBodies(JsonElement jsonElement)
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            ProcessJsonElement(jsonElement, writer);
            writer.Flush();

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        private void ProcessJsonElement(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        
                        // Check if this is an email_body field and clean it
                        if (property.Name.Equals("email_body", StringComparison.OrdinalIgnoreCase) && 
                            property.Value.ValueKind == JsonValueKind.String)
                        {
                            var emailBody = property.Value.GetString() ?? "";
                            var cleanedBody = Regex.Replace(emailBody, @"\bsmartlead\b|open\.sleadtrack\.com", "", RegexOptions.IgnoreCase);
                            
                            writer.WriteStringValue(cleanedBody);
                        }
                        else
                        {
                            ProcessJsonElement(property.Value, writer);
                        }
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        ProcessJsonElement(item, writer);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    writer.WriteStringValue(element.GetString());
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        writer.WriteNumberValue(intValue);
                    else if (element.TryGetInt64(out var longValue))
                        writer.WriteNumberValue(longValue);
                    else if (element.TryGetDouble(out var doubleValue))
                        writer.WriteNumberValue(doubleValue);
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;
            }
        }
    }

    public class InboxRepliesRequest
    {
        /// <summary>
        /// Pagination offset (number of items to skip)
        /// </summary>
        [DefaultValue(0)]
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Number of items to return (max 100)
        /// </summary>
        [DefaultValue(20)]
        [Range(1, 100)]
        public int Limit { get; set; } = 20;

        /// <summary>
        /// Array of Smartlead campaign IDs to fetch replied emails from
        /// </summary>
        [Required]
        public int[] CampaignIds { get; set; } = Array.Empty<int>();
    }
}