using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Api;
using LeadHype.Api.Core.Models.API.Smartlead;
using LeadHype.Api.Core.Models.API.Requests;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Controllers.V1
{
    /// <summary>
    /// Campaigns API v1 - RESTful API for campaign management
    /// </summary>
    [ApiController]
    [Route("api/v1/campaigns")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Tags("Campaigns")]
    public class CampaignsV1Controller : ControllerBase
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IEmailAccountRepository _emailAccountRepository;
        private readonly IAuthService _authService;
        private readonly ICampaignService _campaignService;
        private readonly IWebhookService _webhookService;
        private readonly ICampaignDailyStatEntryRepository _campaignDailyStatsRepository;
        private readonly IEmailTemplateRepository _emailTemplateRepository;
        private readonly ILeadConversationRepository _leadConversationRepository;
        private readonly ILeadEmailHistoryRepository _leadEmailHistoryRepository;
        private readonly ILogger<CampaignsV1Controller> _logger;

        public CampaignsV1Controller(
            ICampaignRepository campaignRepository,
            IClientRepository clientRepository,
            IEmailAccountRepository emailAccountRepository,
            IAuthService authService,
            ICampaignService campaignService,
            IWebhookService webhookService,
            ICampaignDailyStatEntryRepository campaignDailyStatsRepository,
            IEmailTemplateRepository emailTemplateRepository,
            ILeadConversationRepository leadConversationRepository,
            ILeadEmailHistoryRepository leadEmailHistoryRepository,
            ILogger<CampaignsV1Controller> logger)
        {
            _campaignRepository = campaignRepository;
            _clientRepository = clientRepository;
            _emailAccountRepository = emailAccountRepository;
            _authService = authService;
            _campaignService = campaignService;
            _webhookService = webhookService;
            _campaignDailyStatsRepository = campaignDailyStatsRepository;
            _emailTemplateRepository = emailTemplateRepository;
            _leadConversationRepository = leadConversationRepository;
            _leadEmailHistoryRepository = leadEmailHistoryRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all campaigns with pagination and filtering
        /// </summary>
        /// <param name="query">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of campaigns with metrics and pagination info</returns>
        /// <response code="200">Returns the paginated list of campaigns with complete metrics and pagination information</response>
        /// <response code="400">Invalid query parameters</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": [
        ///     {
        ///       "id": "550e8400-e29b-41d4-a716-446655440000",
        ///       "campaignId": 12345,
        ///       "name": "Q4 Product Launch Campaign",
        ///       "clientId": "client-001",
        ///       "clientName": "Acme Corporation",
        ///       "status": "Active",
        ///       "metrics": {
        ///         "totalLeads": 1500,
        ///         "totalSent": 8750,
        ///         "totalOpened": 3200,
        ///         "totalReplied": 240,
        ///         "totalBounced": 125,
        ///         "totalClicked": 680,
        ///         "positiveReplies": 180
        ///       }
        ///     }
        ///   ],
        ///   "pagination": {
        ///     "page": 1,
        ///     "pageSize": 20,
        ///     "totalCount": 45,
        ///     "totalPages": 3
        ///   },
        ///   "timestamp": "2024-01-15T10:30:00.000Z"
        /// }
        /// </example>
        [HttpGet]
        [ProducesResponseType(typeof(CampaignListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCampaigns([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1 || pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid pagination parameters"));
                }

                // Get user permissions
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var isApiKey = User.FindFirst("AuthMethod")?.Value == "ApiKey";
                
                // Check API key permissions early
                if (isApiKey)
                {
                    var hasReadPermission = User.Claims.Any(c => c.Type == "Permission" && 
                        (c.Value == ApiPermissions.ReadCampaigns || c.Value == ApiPermissions.AdminAll));
                    
                    if (!hasReadPermission)
                    {
                        return Forbid();
                    }
                }
                
                // Get assigned client IDs for non-admin users
                List<string>? assignedClientIds = null;
                if (userRole != UserRoles.Admin)
                {
                    var user = await _authService.GetUserByIdAsync(userId!);
                    assignedClientIds = user?.AssignedClientIds;
                    
                    if (assignedClientIds == null || !assignedClientIds.Any())
                    {
                        // User has no assigned clients, return empty result
                        return Ok(new { success = true, data = new List<object>(), pagination = new { page = page, pageSize = pageSize, totalCount = 0, totalPages = 0 }, timestamp = DateTime.UtcNow });
                    }
                }

                // Get all campaigns from database
                var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
                
                // Apply client filtering for non-admin users
                if (assignedClientIds != null)
                {
                    allCampaigns = allCampaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
                }

                // Simple default ordering by creation date (newest first)
                var filteredCampaigns = allCampaigns.OrderByDescending(c => c.CreatedAt).ToList();

                // Get total count before pagination
                var totalCount = filteredCampaigns.Count();

                // Apply pagination
                var paginatedCampaigns = filteredCampaigns
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();


                // Create simple response with just the data
                var response = new
                {
                    success = true,
                    data = paginatedCampaigns.Select(c => new
                    {
                        id = c.Id,
                        campaignId = c.CampaignId,
                        name = c.Name,
                        clientId = c.ClientId,
                        clientName = c.ClientName,
                        status = c.Status,
                        metrics = new
                        {
                            totalLeads = c.TotalLeads ?? 0,
                            totalSent = c.TotalSent ?? 0,
                            totalOpened = c.TotalOpened ?? 0,
                            totalReplied = c.TotalReplied ?? 0,
                            totalBounced = c.TotalBounced ?? 0,
                            totalClicked = c.TotalClicked ?? 0,
                            positiveReplies = c.TotalPositiveReplies ?? 0
                        }
                    }),
                    pagination = new
                    {
                        page = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    timestamp = DateTime.UtcNow
                };

                // Log API usage
                if (isApiKey)
                {
                    _logger.LogInformation($"API Key used to fetch campaigns - Page: {page}, Count: {paginatedCampaigns.Count}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching campaigns");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Get a specific campaign by campaign ID - Retrieves detailed campaign information including metrics, client details, and configuration
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>Complete campaign details with metrics and configuration</returns>
        /// <response code="200">Campaign details retrieved successfully</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="403">Access denied</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "id": "550e8400-e29b-41d4-a716-446655440000",
        ///     "campaignId": 12345,
        ///     "name": "Q4 Product Launch Campaign",
        ///     "clientId": "client-001",
        ///     "clientName": "Acme Corporation",
        ///     "status": "Active",
        ///     "createdAt": "2024-01-15T08:30:00.000Z",
        ///     "updatedAt": "2024-01-20T14:22:00.000Z",
        ///     "totalLeads": 1500,
        ///     "totalSent": 8750,
        ///     "totalOpened": 3200,
        ///     "totalReplied": 240,
        ///     "totalBounced": 125,
        ///     "totalClicked": 680,
        ///     "totalPositiveReplies": 180,
        ///     "emailIds": [1001, 1002, 1003]
        ///   },
        ///   "message": "Campaign retrieved successfully",
        ///   "errorCode": null
        /// }
        /// </example>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CampaignDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCampaign(int id)
        {
            try
            {
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();
                
                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                // Check access permissions
                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                return Ok(ApiResponse<CampaignDetailsDbModel>.SuccessResponse(campaign));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Get campaign statistics with day-by-day breakdown - Retrieves comprehensive metrics including totals, rates, and daily breakdown for campaign performance analysis
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="startDate">Start date for statistics (YYYY-MM-DD format, defaults to 30 days ago)</param>
        /// <param name="endDate">End date for statistics (YYYY-MM-DD format, defaults to today)</param>
        /// <returns>Campaign statistics with comprehensive metrics and daily breakdown</returns>
        /// <response code="200">Campaign statistics retrieved successfully with complete daily breakdown and performance metrics</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="400">Invalid date range (start date after end date or range exceeds 1 year)</response>
        /// <response code="403">Access denied to campaign</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "campaign": {
        ///       "id": "550e8400-e29b-41d4-a716-446655440000",
        ///       "campaignId": 12345,
        ///       "name": "Q4 Product Launch Campaign",
        ///       "status": "Active"
        ///     },
        ///     "timeRange": {
        ///       "startDate": "2024-01-01",
        ///       "endDate": "2024-01-31",
        ///       "days": 31
        ///     },
        ///     "summary": {
        ///       "totalSent": 8750,
        ///       "totalOpened": 3200,
        ///       "totalClicked": 680,
        ///       "totalReplied": 240,
        ///       "totalPositiveReplies": 180,
        ///       "totalBounced": 125,
        ///       "openRate": 36.57,
        ///       "clickRate": 7.77,
        ///       "replyRate": 2.74,
        ///       "bounceRate": 1.43,
        ///       "positiveReplyRate": 75.00
        ///     },
        ///     "dailyStats": [
        ///       {
        ///         "date": "2024-01-01",
        ///         "dayOfWeek": "Monday",
        ///         "sent": 150,
        ///         "opened": 65,
        ///         "clicked": 12,
        ///         "replied": 4,
        ///         "positiveReplies": 3,
        ///         "bounced": 2,
        ///         "openRate": 43.33,
        ///         "clickRate": 8.00,
        ///         "replyRate": 2.67,
        ///         "bounceRate": 1.33
        ///       }
        ///     ]
        ///   },
        ///   "message": "Campaign statistics retrieved successfully",
        ///   "errorCode": null
        /// }
        /// </example>
        [HttpGet("{id:int}/stats")]
        [ProducesResponseType(typeof(CampaignDetailedStatsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCampaignStats(int id, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();
                
                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Default to last 30 days if no dates provided
                var effectiveStartDate = startDate ?? DateTime.UtcNow.AddDays(-30).Date;
                var effectiveEndDate = endDate ?? DateTime.UtcNow.Date;

                // Validate date range
                if (effectiveStartDate > effectiveEndDate)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Start date cannot be after end date", "INVALID_DATE_RANGE"));
                }

                // Limit to maximum 1 year range
                if ((effectiveEndDate - effectiveStartDate).Days > 365)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Date range cannot exceed 1 year", "DATE_RANGE_TOO_LARGE"));
                }

                // Get daily stats for the campaign
                var dailyStats = await _campaignDailyStatsRepository.GetByCampaignIdAndDateRangeAsync(
                    campaign.Id, effectiveStartDate, effectiveEndDate);

                var dailyStatsOrdered = dailyStats.OrderBy(s => s.StatDate).ToList();

                // Calculate totals
                var totalStats = new
                {
                    TotalSent = dailyStatsOrdered.Sum(s => s.Sent),
                    TotalOpened = dailyStatsOrdered.Sum(s => s.Opened),
                    TotalClicked = dailyStatsOrdered.Sum(s => s.Clicked),
                    TotalReplied = dailyStatsOrdered.Sum(s => s.Replied),
                    TotalPositiveReplies = dailyStatsOrdered.Sum(s => s.PositiveReplies),
                    TotalBounced = dailyStatsOrdered.Sum(s => s.Bounced)
                };

                // Calculate rates
                var openRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalOpened / totalStats.TotalSent * 100), 2) : 0;
                var clickRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalClicked / totalStats.TotalSent * 100), 2) : 0;
                var replyRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalReplied / totalStats.TotalSent * 100), 2) : 0;
                var bounceRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalBounced / totalStats.TotalSent * 100), 2) : 0;
                var positiveReplyRate = totalStats.TotalReplied > 0 ? Math.Round((decimal)((double)totalStats.TotalPositiveReplies / totalStats.TotalReplied * 100), 2) : 0;

                // Prepare daily breakdown
                var dailyBreakdown = dailyStatsOrdered.Select(stat => new
                {
                    Date = stat.StatDate.ToString("yyyy-MM-dd"),
                    DayOfWeek = stat.StatDate.ToString("dddd"),
                    Sent = stat.Sent,
                    Opened = stat.Opened,
                    Clicked = stat.Clicked,
                    Replied = stat.Replied,
                    PositiveReplies = stat.PositiveReplies,
                    Bounced = stat.Bounced,
                    OpenRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Opened / stat.Sent * 100), 2) : 0,
                    ClickRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Clicked / stat.Sent * 100), 2) : 0,
                    ReplyRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Replied / stat.Sent * 100), 2) : 0,
                    BounceRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Bounced / stat.Sent * 100), 2) : 0
                }).ToList();

                var stats = new
                {
                    Campaign = new
                    {
                        campaign.Id,
                        campaign.CampaignId,
                        campaign.Name,
                        campaign.Status
                    },
                    TimeRange = new
                    {
                        StartDate = effectiveStartDate.ToString("yyyy-MM-dd"),
                        EndDate = effectiveEndDate.ToString("yyyy-MM-dd"),
                        Days = (effectiveEndDate - effectiveStartDate).Days + 1
                    },
                    Summary = new
                    {
                        totalStats.TotalSent,
                        totalStats.TotalOpened,
                        totalStats.TotalClicked,
                        totalStats.TotalReplied,
                        totalStats.TotalPositiveReplies,
                        totalStats.TotalBounced,
                        OpenRate = openRate,
                        ClickRate = clickRate,
                        ReplyRate = replyRate,
                        BounceRate = bounceRate,
                        PositiveReplyRate = positiveReplyRate
                    },
                    DailyStats = dailyBreakdown
                };

                return Ok(ApiResponse<object>.SuccessResponse(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching campaign stats for {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }






        /// <summary>
        /// Get email accounts assigned to a specific campaign - Retrieves all email accounts used for sending emails in this campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>List of email accounts with detailed information</returns>
        /// <response code="200">Email accounts retrieved successfully</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="403">Access denied</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": [
        ///     {
        ///       "id": 1001,
        ///       "email": "sender1@acme.com",
        ///       "name": "John Sender",
        ///       "status": "Active",
        ///       "warmupEnabled": true,
        ///       "adminUuid": "client-001"
        ///     },
        ///     {
        ///       "id": 1002,
        ///       "email": "sender2@acme.com",
        ///       "name": "Jane Sender",
        ///       "status": "Active",
        ///       "warmupEnabled": false,
        ///       "adminUuid": "client-001"
        ///     }
        ///   ],
        ///   "message": "Email accounts retrieved successfully",
        ///   "errorCode": null
        /// }
        /// </example>
        [HttpGet("{id:int}/email-accounts")]
        [ProducesResponseType(typeof(CampaignEmailAccountsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCampaignEmailAccounts(int id)
        {
            try
            {
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();
                
                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Check if campaign has email IDs
                if (campaign.EmailIds == null || !campaign.EmailIds.Any())
                {
                    return Ok(ApiResponse<List<EmailAccountDbModel>>.SuccessResponse(new List<EmailAccountDbModel>()));
                }

                // Get email accounts by IDs
                var allEmailAccounts = await _emailAccountRepository.GetAllAsync();
                var campaignEmailAccounts = allEmailAccounts
                    .Where(ea => campaign.EmailIds.Contains(ea.Id))
                    .ToList();

                // Apply user access filtering for non-admin users
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != UserRoles.Admin)
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await _authService.GetUserByIdAsync(userId!);
                    
                    if (user?.AssignedClientIds != null)
                    {
                        campaignEmailAccounts = campaignEmailAccounts
                            .Where(ea => user.AssignedClientIds.Contains(ea.AdminUuid))
                            .ToList();
                    }
                }

                _logger.LogInformation($"Retrieved {campaignEmailAccounts.Count} email accounts for campaign {id}");

                return Ok(ApiResponse<List<EmailAccountDbModel>>.SuccessResponse(campaignEmailAccounts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching email accounts for campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Get lead history for a specific campaign - Retrieves paginated list of leads with conversation data and message counts
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of leads per page (max 50)</param>
        /// <returns>Paginated lead history with conversation data and message counts</returns>
        /// <response code="200">Lead history retrieved successfully with pagination</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="403">Access denied</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "campaign": {
        ///       "id": "550e8400-e29b-41d4-a716-446655440000",
        ///       "campaignId": 12345,
        ///       "name": "Q4 Product Launch Campaign"
        ///     },
        ///     "leads": [
        ///       {
        ///         "id": "lead-abc123",
        ///         "externalLeadId": "sl-456789",
        ///         "email": "john@example.com",
        ///         "firstName": "John",
        ///         "lastName": "Doe",
        ///         "status": "Replied",
        ///         "messageCount": 4
        ///       }
        ///     ],
        ///     "totalLeads": 1500,
        ///     "totalMessages": 6750,
        ///     "currentPage": 1,
        ///     "pageSize": 20,
        ///     "totalPages": 75
        ///   },
        ///   "message": "Lead history retrieved successfully",
        ///   "errorCode": null
        /// }
        /// </example>
        [HttpGet("{id:int}/lead-history")]
        [ProducesResponseType(typeof(CampaignLeadHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCampaignLeadHistory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool withRepliesOnly = false)
        {
            try
            {
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();

                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Validate pagination parameters
                pageSize = Math.Min(Math.Max(pageSize, 1), 50); // Clamp between 1 and 50
                page = Math.Max(page, 1); // Ensure page is at least 1

                // Get cached leads for this campaign (with optional filter for replies)
                var cachedLeads = (await _leadConversationRepository.GetByCampaignIdAsync(id, withRepliesOnly)).ToList();
                
                if (cachedLeads == null || !cachedLeads.Any())
                {
                    // Still get the total message count even if no leads are cached
                    var totalMessagesForEmptyLeads = await _leadEmailHistoryRepository.GetTotalMessageCountByCampaignIdAsync(id);
                    
                    return Ok(ApiResponse<object>.SuccessResponse(new 
                    {
                        campaign = new { id = campaign.Id, campaignId = campaign.CampaignId, name = campaign.Name },
                        leads = new List<object>(),
                        totalLeads = 0,
                        totalMessages = totalMessagesForEmptyLeads,
                        currentPage = page,
                        pageSize = pageSize,
                        totalPages = 0
                    }));
                }

                // Get message counts efficiently using SQL aggregation
                var messageCountsByLeadId = await _leadEmailHistoryRepository.GetMessageCountsByCampaignIdAsync(id);
                var totalMessages = await _leadEmailHistoryRepository.GetTotalMessageCountByCampaignIdAsync(id);

                // Apply pagination
                var totalLeads = cachedLeads.Count();
                var totalPages = (int)Math.Ceiling((double)totalLeads / pageSize);
                var paginatedLeads = cachedLeads.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Process leads to extract data and calculate message counts
                var leadData = new List<object>();
                
                foreach (var lead in paginatedLeads)
                {
                    // Extract leadId from ConversationData if available
                    string smartleadLeadId = "";
                    try
                    {
                        if (!string.IsNullOrEmpty(lead.ConversationData))
                        {
                            var conversationData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(lead.ConversationData);
                            
                            // Debug log the available properties
                            if (conversationData.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var availableProperties = string.Join(", ", conversationData.EnumerateObject().Select(p => p.Name));
                                _logger.LogDebug($"Available properties in conversation data for lead {lead.Id}: {availableProperties}");
                            }
                            
                            // Try multiple possible property names for the external lead ID
                            if (conversationData.TryGetProperty("Id", out var idProperty))
                            {
                                smartleadLeadId = idProperty.GetString() ?? "";
                            }
                            else if (conversationData.TryGetProperty("id", out var lowerIdProperty))
                            {
                                smartleadLeadId = lowerIdProperty.GetString() ?? "";
                            }
                            else if (conversationData.TryGetProperty("lead_id", out var leadIdProperty))
                            {
                                smartleadLeadId = leadIdProperty.GetString() ?? "";
                            }
                            else if (conversationData.TryGetProperty("smartlead_id", out var smartleadIdProperty))
                            {
                                smartleadLeadId = smartleadIdProperty.GetString() ?? "";
                            }
                            else if (conversationData.TryGetProperty("campaign_lead_map_id", out var mapIdProperty))
                            {
                                smartleadLeadId = mapIdProperty.GetString() ?? "";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse conversation data for lead {LeadId}", lead.Id);
                    }

                    // Get message count for this lead from the counts dictionary
                    var messageCount = 0;
                    if (!string.IsNullOrEmpty(smartleadLeadId) && messageCountsByLeadId.ContainsKey(smartleadLeadId))
                    {
                        messageCount = messageCountsByLeadId[smartleadLeadId];
                    }

                    leadData.Add(new
                    {
                        id = lead.Id, // Database lead ID
                        externalLeadId = smartleadLeadId, // External platform lead ID
                        email = lead.LeadEmail,
                        firstName = lead.LeadFirstName,
                        lastName = lead.LeadLastName,
                        status = lead.Status,
                        messageCount = messageCount, // Number of messages for this lead
                        conversationData = lead.ConversationData // Full lead conversation data stored as JSON
                    });
                }

                return Ok(ApiResponse<object>.SuccessResponse(new 
                {
                    campaign = new { id = campaign.Id, campaignId = campaign.CampaignId, name = campaign.Name },
                    leads = leadData,
                    totalLeads = totalLeads,
                    totalMessages = totalMessages,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = totalPages
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching lead history for campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Get email history for a specific lead in a campaign - Retrieves complete email conversation thread including subject, body, and sequence information
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="leadId">Lead ID (internal database lead ID)</param>
        /// <returns>Email history with complete conversation thread for the lead</returns>
        /// <response code="200">Email history retrieved successfully with complete conversation thread</response>
        /// <response code="404">Campaign or lead not found</response>
        /// <response code="403">Access denied to campaign</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "id": "lead-abc123",
        ///     "emailHistory": [
        ///       {
        ///         "subject": "Introduction - Partnership Opportunity",
        ///         "body": "Hi John,\n\nI hope this email finds you well. I wanted to reach out regarding a potential partnership opportunity...",
        ///         "sequenceNumber": 1,
        ///         "createdAt": "2024-01-15T09:30:00.000Z"
        ///       },
        ///       {
        ///         "subject": "Re: Introduction - Partnership Opportunity",
        ///         "body": "Thank you for reaching out. This sounds interesting. Could you provide more details about...",
        ///         "sequenceNumber": 2,
        ///         "createdAt": "2024-01-16T14:22:00.000Z"
        ///       },
        ///       {
        ///         "subject": "Follow-up: Partnership Details",
        ///         "body": "Absolutely! Here are the details you requested:\n\n1. Partnership structure...",
        ///         "sequenceNumber": 3,
        ///         "createdAt": "2024-01-17T11:15:00.000Z"
        ///       }
        ///     ]
        ///   },
        ///   "message": "Lead email history retrieved successfully",
        ///   "errorCode": null
        /// }
        /// </example>
        [HttpGet("{id:int}/leads/{leadId}/history")]
        [ProducesResponseType(typeof(LeadEmailHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLeadHistory(int id, string leadId)
        {
            try
            {
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();
                
                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Get cached lead conversation data for this campaign
                var cachedLeads = (await _leadConversationRepository.GetByCampaignIdAsync(id)).ToList();
                var targetLead = cachedLeads?.FirstOrDefault(l => l.Id == leadId);
                
                if (targetLead == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Lead not found", "LEAD_NOT_FOUND"));
                }

                // Extract the external lead ID from cached conversation data
                string smartleadLeadId = "";
                try
                {
                    if (!string.IsNullOrEmpty(targetLead.ConversationData))
                    {
                        var conversationData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(targetLead.ConversationData);
                        // Try multiple possible property names for the external lead ID
                        if (conversationData.TryGetProperty("Id", out var idProperty))
                        {
                            smartleadLeadId = idProperty.GetString() ?? "";
                        }
                        else if (conversationData.TryGetProperty("id", out var lowerIdProperty))
                        {
                            smartleadLeadId = lowerIdProperty.GetString() ?? "";
                        }
                        else if (conversationData.TryGetProperty("lead_id", out var leadIdProperty))
                        {
                            smartleadLeadId = leadIdProperty.GetString() ?? "";
                        }
                        else if (conversationData.TryGetProperty("smartlead_id", out var smartleadIdProperty))
                        {
                            smartleadLeadId = smartleadIdProperty.GetString() ?? "";
                        }
                        else if (conversationData.TryGetProperty("campaign_lead_map_id", out var mapIdProperty))
                        {
                            smartleadLeadId = mapIdProperty.GetString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to extract external lead ID for lead {leadId}");
                }
                
                // Get email history from database instead of API
                var emailHistoryFromDb = await _leadEmailHistoryRepository.GetByCampaignAndLeadIdAsync(id, smartleadLeadId);
                
                var emailHistory = emailHistoryFromDb?.Select(h => new
                {
                    subject = h.Subject,
                    body = h.Body,
                    sequenceNumber = h.SequenceNumber,
                    type = h.Type,
                    time = h.Time,
                    createdAt = h.CreatedAt,
                    // RevReply classification data
                    classificationResult = h.ClassificationResult,
                    classifiedAt = h.ClassifiedAt,
                    isClassified = h.IsClassified
                }).Cast<object>().ToList() ?? new List<object>();

                // If no email history from database, provide lead information from cache
                if (!emailHistory.Any())
                {
                    try
                    {
                        // Deserialize using Newtonsoft.Json (same as serialization)  
                        var leadDatum = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(targetLead.ConversationData);
                        
                        emailHistory.Add(new
                        {
                            subject = $"Lead Profile - {targetLead.Status}",
                            body = $"Lead Information from Cache:\n\n" +
                                  $"Name: {targetLead.LeadFirstName} {targetLead.LeadLastName}\n" +
                                  $"Email: {targetLead.LeadEmail}\n" +
                                  $"Status: {targetLead.Status}\n" +
                                  $"Company: {leadDatum?.lead?.company_name ?? "Not available"}\n" +
                                  $"Phone: {leadDatum?.lead?.phone_number ?? "Not available"}\n" +
                                  $"Created: {leadDatum?.created_at ?? "Unknown"}\n" +
                                  $"Campaign Lead Map ID: {leadDatum?.campaign_lead_map_id ?? "Unknown"}\n\n" +
                                  $"Note: No detailed email conversation history available for this lead.",
                            sequenceNumber = 1
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse conversation data for lead {leadId}");
                        
                        emailHistory.Add(new
                        {
                            subject = "Lead Information",
                            body = $"Basic lead information:\n" +
                                  $"Name: {targetLead.LeadFirstName} {targetLead.LeadLastName}\n" +
                                  $"Email: {targetLead.LeadEmail}\n" +
                                  $"Status: {targetLead.Status}",
                            sequenceNumber = 1
                        });
                    }
                }

                return Ok(ApiResponse<object>.SuccessResponse(new 
                {
                    id = leadId,
                    emailHistory = emailHistory
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching history for lead {leadId} in campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Get email templates used in a specific campaign - Retrieves all email templates with subject, body, and sequence information
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>Complete email templates with sequence order</returns>
        /// <response code="200">Email templates retrieved successfully</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="403">Access denied</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "campaign": {
        ///       "id": "550e8400-e29b-41d4-a716-446655440000",
        ///       "campaignId": 12345,
        ///       "name": "Q4 Product Launch Campaign"
        ///     },
        ///     "templates": [
        ///       {
        ///         "subject": "Introduction - Partnership Opportunity",
        ///         "body": "Hi {{FirstName}},\n\nI hope this email finds you well...",
        ///         "sequenceNumber": 1
        ///       },
        ///       {
        ///         "subject": "Follow-up: Did you see my previous email?",
        ///         "body": "Hi {{FirstName}},\n\nI wanted to follow up on my previous email...",
        ///         "sequenceNumber": 2
        ///       }
        ///     ],
        ///     "totalTemplates": 2
        ///   },
        ///   "message": "Templates retrieved successfully",
        ///   "errorCode": null
        /// }
        /// </example>
        [HttpGet("{id:int}/templates")]
        [ProducesResponseType(typeof(CampaignTemplatesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCampaignTemplates(int id)
        {
            try
            {
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();

                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Get cached templates with variants from database
                var templatesWithVariants = await _emailTemplateRepository.GetByCampaignIdWithVariantsAsync(id);

                var formattedTemplates = templatesWithVariants.Select(kvp => new
                {
                    subject = kvp.Key.Subject,
                    body = kvp.Key.Body,
                    sequenceNumber = kvp.Key.SequenceNumber,
                    variants = kvp.Value.Any() ? kvp.Value.Select(v => new
                    {
                        id = v.SmartleadVariantId,
                        variantLabel = v.VariantLabel,
                        subject = v.Subject,
                        body = v.Body
                    }).ToList() : null
                }).OrderBy(t => t.sequenceNumber).Cast<object>().ToList();

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    campaign = new { id = campaign.Id, campaignId = campaign.CampaignId, name = campaign.Name },
                    templates = formattedTemplates,
                    totalTemplates = formattedTemplates.Count
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching templates for campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Get campaigns filtered by client IDs using POST to avoid header size issues
        /// </summary>
        /// <param name="request">Filter request with client IDs and pagination</param>
        /// <returns>Filtered campaigns with pagination</returns>
        /// <response code="200">Returns the filtered campaigns</response>
        /// <response code="400">Invalid request parameters</response>
        [HttpPost("filtered")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFilteredCampaigns([FromBody] CampaignFilterRequest request)
        {
            try
            {
                // Validate pagination parameters
                if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid pagination parameters"));
                }

                // Get user permissions
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var isApiKey = User.FindFirst("AuthMethod")?.Value == "ApiKey";
                
                // Check API key permissions early
                if (isApiKey)
                {
                    var hasReadPermission = User.Claims.Any(c => c.Type == "Permission" && 
                        (c.Value == ApiPermissions.ReadCampaigns || c.Value == ApiPermissions.AdminAll));
                    
                    if (!hasReadPermission)
                    {
                        return Forbid();
                    }
                }
                
                // Get assigned client IDs for non-admin users
                List<string>? assignedClientIds = null;
                if (userRole != UserRoles.Admin)
                {
                    var user = await _authService.GetUserByIdAsync(userId!);
                    assignedClientIds = user?.AssignedClientIds;
                    
                    if (assignedClientIds == null || !assignedClientIds.Any())
                    {
                        // User has no assigned clients, return empty result
                        return Ok(new { 
                            success = true, 
                            data = new List<object>(), 
                            pagination = new { 
                                page = request.Page, 
                                pageSize = request.PageSize, 
                                totalCount = 0, 
                                totalPages = 0 
                            }, 
                            hasNext = false,
                            timestamp = DateTime.UtcNow 
                        });
                    }
                }

                // Get all campaigns from database
                var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
                
                // Apply client filtering for non-admin users
                if (assignedClientIds != null)
                {
                    allCampaigns = allCampaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
                }

                // Apply client ID filtering from request
                if (request.ClientIds != null && request.ClientIds.Any())
                {
                    allCampaigns = allCampaigns.Where(c => request.ClientIds.Contains(c.ClientId ?? "")).ToList();
                }

                // Get total count before pagination
                var totalCount = allCampaigns.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                // Apply pagination
                var paginatedCampaigns = allCampaigns
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Create response with pagination info
                var response = new
                {
                    success = true,
                    data = paginatedCampaigns.Select(c => new
                    {
                        id = c.Id,
                        campaignId = c.CampaignId,
                        name = c.Name,
                        clientId = c.ClientId,
                        clientName = c.ClientName,
                        status = c.Status,
                        totalLeads = c.TotalLeads ?? 0,
                        totalSent = c.TotalSent ?? 0,
                        totalOpened = c.TotalOpened ?? 0,
                        totalReplied = c.TotalReplied ?? 0,
                        totalBounced = c.TotalBounced ?? 0,
                        totalClicked = c.TotalClicked ?? 0,
                        positiveReplies = c.TotalPositiveReplies ?? 0
                    }),
                    pagination = new
                    {
                        page = request.Page,
                        pageSize = request.PageSize,
                        totalCount = totalCount,
                        totalPages = totalPages
                    },
                    hasNext = request.Page < totalPages,
                    timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching filtered campaigns");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Create a new campaign
        /// </summary>
        /// <param name="request">Campaign creation request</param>
        /// <returns>Success message and campaign ID</returns>
        /// <response code="201">Campaign created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied</response>
        [HttpPost]
        [ProducesResponseType(typeof(CampaignCreationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignV1Request request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Validation failed: {string.Join(", ", errors)}", "VALIDATION_ERROR"));
                }

                // Check permissions
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var isApiKey = User.FindFirst("AuthMethod")?.Value == "ApiKey";
                
                if (isApiKey)
                {
                    var hasCreatePermission = User.Claims.Any(c => c.Type == "Permission" && 
                        (c.Value == ApiPermissions.WriteCampaigns || c.Value == ApiPermissions.AdminAll));
                    
                    if (!hasCreatePermission)
                    {
                        return Forbid();
                    }
                }

                // Use hardcoded client ID 3138 for Smartlead API
                const int clientId = 3138;

                // Create campaign using campaign service with hardcoded client ID
                var campaignId = await _campaignService.CreateCampaignAsync(request, clientId);

                if (campaignId == null)
                {
                    return StatusCode(500, new CampaignCreationResponse
                    {
                        Success = false,
                        Message = "Failed to create campaign",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation($"Campaign created successfully: {campaignId} for client {clientId}");

                var successResponse = new CampaignCreationResponse
                {
                    Success = true,
                    Message = "Campaign created successfully",
                    CampaignId = campaignId.Value,
                    Timestamp = DateTime.UtcNow
                };

                // Trigger webhook event for admin users only
                if (userRole == UserRoles.Admin)
                {
                    try
                    {
                        var webhookPayload = new
                        {
                            campaign = new
                            {
                                campaignId = campaignId.Value
                            },
                            request = new
                            {
                                title = request.Title,
                                clientId = clientId
                            },
                            user = new
                            {
                                id = userId,
                                role = userRole
                            }
                        };

                        // Send callback to the required callback URL
                        await _webhookService.SendWebhookToUrlAsync(request.CallbackUrl, webhookPayload);
                        _logger.LogInformation($"Callback sent to {request.CallbackUrl} for campaign creation: {campaignId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to trigger webhook for campaign creation: {campaignId}");
                        // Don't fail the campaign creation if webhook fails
                    }
                }

                return StatusCode(201, successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }


        /// <summary>
        /// Configure campaign sequence and email sequences - Sets up email templates and timing for automated email sequences
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="request">Sequence configuration request</param>
        /// <returns>Success confirmation with configuration details</returns>
        /// <response code="200">Sequence configured successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Campaign not found</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "message": "Campaign sequence configured successfully",
        ///   "campaignId": 12345,
        ///   "timestamp": "2024-01-25T10:30:45.123Z"
        /// }
        /// </example>
        [HttpPut("{id:int}/sequence")]
        [ProducesResponseType(typeof(CampaignSequenceConfigResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfigureCampaignSequence(int id, [FromBody] ConfigureSequenceV1Request request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Validation failed: {string.Join(", ", errors)}", "VALIDATION_ERROR"));
                }

                // Check permissions
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var isApiKey = User.FindFirst("AuthMethod")?.Value == "ApiKey";
                
                if (isApiKey)
                {
                    var hasWritePermission = User.Claims.Any(c => c.Type == "Permission" && 
                        (c.Value == ApiPermissions.WriteCampaigns || c.Value == ApiPermissions.AdminAll));
                    
                    if (!hasWritePermission)
                    {
                        return Forbid();
                    }
                }

                // Verify campaign exists and user has access
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();
                
                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Configure campaign sequence
                var success = await _campaignService.ConfigureCampaignSequenceAsync(id, request);

                if (!success)
                {
                    return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to configure campaign sequence", "CONFIGURATION_FAILED"));
                }

                var successResponse = new
                {
                    success = true,
                    message = "Campaign sequence configured successfully",
                    campaignId = id,
                    timestamp = DateTime.UtcNow
                };

                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error configuring sequence for campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Upload leads to a specific campaign - Bulk upload leads with contact information and custom fields
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="leads">List of leads to upload (max 100 per request)</param>
        /// <returns>Upload results with lead IDs and success confirmation</returns>
        /// <response code="200">Leads uploaded successfully</response>
        /// <response code="400">Invalid request data or validation errors</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Campaign not found</response>
        /// <example>
        /// Sample response:
        /// {
        ///   "success": true,
        ///   "message": "Leads uploaded successfully",
        ///   "campaignId": 12345,
        ///   "uploadedCount": 25,
        ///   "leadIds": ["lead-001", "lead-002", "lead-003"],
        ///   "timestamp": "2024-01-25T10:30:45.123Z"
        /// }
        /// </example>
        [HttpPost("{id:int}/leads")]
        [ProducesResponseType(typeof(CampaignLeadUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadCampaignLeads(int id, [FromBody] List<LeadInput> leads)
        {
            try
            {
                // Validate request
                if (leads == null || !leads.Any())
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Lead list cannot be empty", "VALIDATION_ERROR"));
                }
                
                if (leads.Count > 100)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Maximum 100 leads allowed per request", "VALIDATION_ERROR"));
                }
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Validation failed: {string.Join(", ", errors)}", "VALIDATION_ERROR"));
                }

                // Check permissions
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var isApiKey = User.FindFirst("AuthMethod")?.Value == "ApiKey";
                
                if (isApiKey)
                {
                    var hasWritePermission = User.Claims.Any(c => c.Type == "Permission" && 
                        (c.Value == ApiPermissions.WriteCampaigns || c.Value == ApiPermissions.AdminAll));
                    
                    if (!hasWritePermission)
                    {
                        return Forbid();
                    }
                }

                // Verify campaign exists and user has access
                var campaigns = await _campaignRepository.GetByCampaignIdAsync(id);
                var campaign = campaigns.FirstOrDefault();
                
                if (campaign == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Campaign not found", "NOT_FOUND"));
                }

                if (!await HasCampaignAccess(campaign))
                {
                    return Forbid();
                }

                // Upload leads using campaign service
                var uploadResult = await _campaignService.UploadLeadsAsync(id, leads);

                if (!uploadResult.Success)
                {
                    return StatusCode(500, ApiResponse<object>.ErrorResponse(uploadResult.ErrorMessage ?? "Failed to upload leads", "UPLOAD_FAILED"));
                }

                var successResponse = new
                {
                    success = true,
                    message = "Leads uploaded successfully",
                    campaignId = id,
                    uploadedCount = leads.Count,
                    leadIds = uploadResult.LeadIds,
                    timestamp = DateTime.UtcNow
                };

                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading leads for campaign {id}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
            }
        }

        #region Helper Methods

        private async Task<bool> HasCampaignAccess(CampaignDetailsDbModel campaign)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Admins have access to all campaigns
            if (userRole == UserRoles.Admin)
            {
                return true;
            }

            // Check if user has permission through API key
            if (User.FindFirst("AuthMethod")?.Value == "ApiKey")
            {
                var hasReadPermission = User.Claims.Any(c => c.Type == "Permission" && 
                    (c.Value == ApiPermissions.ReadCampaigns || c.Value == ApiPermissions.AdminAll));
                
                if (!hasReadPermission)
                {
                    return false;
                }
            }

            // Check if user has access to the campaign's client
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _authService.GetUserByIdAsync(userId!);
            
            if (user?.AssignedClientIds != null && campaign.ClientId != null)
            {
                return user.AssignedClientIds.Contains(campaign.ClientId);
            }

            return false;
        }

        #endregion
    }
}