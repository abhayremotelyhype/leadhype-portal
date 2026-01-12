using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Database;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.API.Requests;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using LeadHype.Api.Models.UI;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Dapper;

namespace LeadHype.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize]
public class CampaignController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly ILogger<CampaignController> _logger;
    private readonly IAuthService _authService;
    private readonly ICampaignEventRepository _campaignEventRepository;
    private readonly ICampaignDailyStatEntryRepository _campaignDailyStatRepository;
    private readonly ILeadConversationRepository _leadConversationRepository;
    private readonly ILeadEmailHistoryRepository _leadEmailHistoryRepository;
    private readonly IDbConnectionService _connectionService;

    public CampaignController(
        ICampaignRepository campaignRepository,
        IClientRepository clientRepository,
        IEmailAccountRepository emailAccountRepository,
        ILogger<CampaignController> logger,
        IAuthService authService,
        ICampaignEventRepository campaignEventRepository,
        ICampaignDailyStatEntryRepository campaignDailyStatRepository,
        ILeadConversationRepository leadConversationRepository,
        ILeadEmailHistoryRepository leadEmailHistoryRepository,
        IDbConnectionService connectionService)
    {
        _campaignRepository = campaignRepository;
        _clientRepository = clientRepository;
        _emailAccountRepository = emailAccountRepository;
        _logger = logger;
        _authService = authService;
        _campaignEventRepository = campaignEventRepository;
        _campaignDailyStatRepository = campaignDailyStatRepository;
        _leadConversationRepository = leadConversationRepository;
        _leadEmailHistoryRepository = leadEmailHistoryRepository;
        _connectionService = connectionService;
    }

    private async Task<List<string>?> GetUserAssignedClientIds()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admin users can see all campaigns
        if (userRole == UserRoles.Admin)
        {
            return null; // null means no filtering
        }
        
        // Regular users can only see campaigns from assigned clients
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return new List<string>(); // Empty list means no access
        
        var user = await _authService.GetUserByIdAsync(userId);
        return user?.AssignedClientIds ?? new List<string>();
    }


    private async Task ApplyTimeRangeFiltering(List<CampaignDetailsDbModel> campaigns, int? timeRangeDays)
    {
        // If no time range specified or "all time" (9999 days), keep total stats
        if (!timeRangeDays.HasValue || timeRangeDays.Value >= 9999)
        {
            return;
        }

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-timeRangeDays.Value);
        
        _logger.LogInformation("Applying time range filtering: {Days} days ({StartDate} to {EndDate})", 
            timeRangeDays.Value, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        // Get campaign API IDs (not database IDs) for the new repository
        var campaignApiIds = campaigns.Select(c => c.CampaignId.ToString()).ToList();
        
        // Fetch aggregated daily stats using the new event repository
        var dailyStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
            startDate, endDate, campaignApiIds);
        
        // Group stats by date and calculate totals per campaign
        var totalsByCampaign = new Dictionary<string, (int sent, int opened, int replied, int positive, int bounced, int clicked)>();
        
        foreach (var stat in dailyStats)
        {
            // This gives us daily totals across all campaigns - we need individual campaign stats
            // For now, we'll use the individual campaign stats approach instead
        }

        // Update campaigns with time-range specific totals using batch query
        // NOTE: campaign_events table uses numeric campaign IDs, not UUIDs
        var numericCampaignIds = campaigns.Select(c => c.CampaignId.ToString()).ToList();
        var batchStats = await _campaignEventRepository.GetStatsForCampaignsAsync(numericCampaignIds, startDate, endDate);
        
        // Aggregate daily stats to get totals per campaign (since function returns daily data)
        // Group by numeric campaign ID since that's what campaign_events uses
        var statsLookup = batchStats
            .GroupBy(s => s.CampaignId)
            .ToDictionary(g => g.Key, g => new
            {
                Sent = g.Sum(s => s.Sent),
                Opened = g.Sum(s => s.Opened),
                Clicked = g.Sum(s => s.Clicked),
                Replied = g.Sum(s => s.Replied),
                PositiveReplies = g.Sum(s => s.PositiveReplies),
                Bounced = g.Sum(s => s.Bounced)
            });

        foreach (var campaign in campaigns)
        {
            // Look up stats using numeric campaign ID
            var campaignIdKey = campaign.CampaignId.ToString();
            if (statsLookup.TryGetValue(campaignIdKey, out var campaignStats))
            {
                // Update totals from event-sourced stats
                campaign.TotalSent = campaignStats.Sent;
                campaign.TotalOpened = campaignStats.Opened;
                campaign.TotalClicked = campaignStats.Clicked;
                campaign.TotalReplied = campaignStats.Replied;
                campaign.TotalPositiveReplies = campaignStats.PositiveReplies;
                campaign.TotalBounced = campaignStats.Bounced;
            }
            else
            {
                // No stats in this time range - set all to 0
                campaign.TotalSent = 0;
                campaign.TotalOpened = 0;
                campaign.TotalClicked = 0;
                campaign.TotalReplied = 0;
                campaign.TotalPositiveReplies = 0;
                campaign.TotalBounced = 0;
            }
        }
        
        _logger.LogInformation("Applied time range filtering to {Count} campaigns", campaigns.Count);
    }


    private IOrderedEnumerable<CampaignDetailsDbModel> ApplySingleSortList(
        List<CampaignDetailsDbModel> campaigns,
        string sortColumn,
        bool isDescending,
        bool isPercentageMode)
    {
        return sortColumn.ToLower() switch
        {
            "name" => isDescending ? campaigns.OrderByDescending(c => c.Name) : campaigns.OrderBy(c => c.Name),
            "status" => isDescending ? campaigns.OrderByDescending(c => c.Status) : campaigns.OrderBy(c => c.Status),
            "client" => isDescending ? campaigns.OrderByDescending(c => c.ClientName) : campaigns.OrderBy(c => c.ClientName),
            "totalleads" => isDescending ? campaigns.OrderByDescending(c => c.TotalLeads ?? 0) : campaigns.OrderBy(c => c.TotalLeads ?? 0),
            "totalsent" => isDescending ? campaigns.OrderByDescending(c => c.TotalSent) : campaigns.OrderBy(c => c.TotalSent),
            "totalopened" => isPercentageMode
                ? (isDescending ? campaigns.OrderByDescending(c => c.TotalSent > 0 ? (double)c.TotalOpened / c.TotalSent : 0) : campaigns.OrderBy(c => c.TotalSent > 0 ? (double)c.TotalOpened / c.TotalSent : 0))
                : (isDescending ? campaigns.OrderByDescending(c => c.TotalOpened) : campaigns.OrderBy(c => c.TotalOpened)),
            "totalreplied" => isPercentageMode
                ? (isDescending ? campaigns.OrderByDescending(c => c.TotalSent > 0 ? (double)c.TotalReplied / c.TotalSent : 0) : campaigns.OrderBy(c => c.TotalSent > 0 ? (double)c.TotalReplied / c.TotalSent : 0))
                : (isDescending ? campaigns.OrderByDescending(c => c.TotalReplied) : campaigns.OrderBy(c => c.TotalReplied)),
            "totalpositivereplies" => isPercentageMode
                ? (isDescending ? campaigns.OrderByDescending(c => c.TotalReplied > 0 ? (double)(c.TotalPositiveReplies ?? 0) / c.TotalReplied.Value : 0) : campaigns.OrderBy(c => c.TotalReplied > 0 ? (double)(c.TotalPositiveReplies ?? 0) / c.TotalReplied.Value : 0))
                : (isDescending ? campaigns.OrderByDescending(c => c.TotalPositiveReplies ?? 0) : campaigns.OrderBy(c => c.TotalPositiveReplies ?? 0)),
            "positivereplyrate" => isDescending 
                ? campaigns.OrderByDescending(c => c.TotalSent > 0 ? (double)(c.TotalPositiveReplies ?? 0) / c.TotalSent.Value * 100 : 0) 
                : campaigns.OrderBy(c => c.TotalSent > 0 ? (double)(c.TotalPositiveReplies ?? 0) / c.TotalSent.Value * 100 : 0),
            "totalbounced" => isPercentageMode
                ? (isDescending ? campaigns.OrderByDescending(c => c.TotalSent > 0 ? (double)c.TotalBounced / c.TotalSent : 0) : campaigns.OrderBy(c => c.TotalSent > 0 ? (double)c.TotalBounced / c.TotalSent : 0))
                : (isDescending ? campaigns.OrderByDescending(c => c.TotalBounced) : campaigns.OrderBy(c => c.TotalBounced)),
            "totalclicked" => isPercentageMode
                ? (isDescending ? campaigns.OrderByDescending(c => c.TotalSent > 0 ? (double)c.TotalClicked / c.TotalSent : 0) : campaigns.OrderBy(c => c.TotalSent > 0 ? (double)c.TotalClicked / c.TotalSent : 0))
                : (isDescending ? campaigns.OrderByDescending(c => c.TotalClicked) : campaigns.OrderBy(c => c.TotalClicked)),
            "createdat" => isDescending ? campaigns.OrderByDescending(c => c.CreatedAt) : campaigns.OrderBy(c => c.CreatedAt),
            "updatedat" => isDescending ? campaigns.OrderByDescending(c => c.UpdatedAt) : campaigns.OrderBy(c => c.UpdatedAt),
            "lastupdatedat" => isDescending ? campaigns.OrderByDescending(c => c.LastUpdatedAt ?? c.UpdatedAt) : campaigns.OrderBy(c => c.LastUpdatedAt ?? c.UpdatedAt),
            "emailaccounts" => isDescending ? campaigns.OrderByDescending(c => c.EmailIds?.Count ?? 0) : campaigns.OrderBy(c => c.EmailIds?.Count ?? 0),
            "notes" => isDescending ? campaigns.OrderByDescending(c => c.Notes ?? "") : campaigns.OrderBy(c => c.Notes ?? ""),
            _ => campaigns.OrderByDescending(c => c.CreatedAt)
        };
    }


    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CampaignDetailsDbModel>>> GetCampaigns(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,          // single column to sort by
        [FromQuery] string? sortDirection = null,    // asc or desc
        [FromQuery] string? sortMode = null,         // count or percentage mode for stats columns
        [FromQuery] string? ids = null,
        [FromQuery] string? clientIds = null,
        [FromQuery] int? timeRangeDays = null,
        [FromQuery] long? emailAccountId = null,
        [FromQuery] string? filterByClientIds = null, // alias for clientIds to match frontend
        [FromQuery] string? filterByUserIds = null,   // filter by multiple user IDs (comma-separated)
        [FromQuery] int? performanceFilterMinSent = null,     // worst performing filter minimum sent emails
        [FromQuery] double? performanceFilterMaxReplyRate = null) // worst performing filter maximum reply rate (%)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500; // Max limit to prevent performance issues

            var allCampaigns = (await _campaignRepository.GetAllAsync()).AsQueryable();

            // Apply user-based filtering first
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
                {
                    // User has no assigned clients, return empty result
                    return Ok(new PaginatedResponse<CampaignDetailsDbModel>
                    {
                        Data = new List<CampaignDetailsDbModel>(),
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 0,
                        HasPrevious = false,
                        HasNext = false
                    });
                }
                allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId));
            }

            // Filter by specific campaign IDs if provided
            if (!string.IsNullOrWhiteSpace(ids))
            {
                var campaignIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
                allCampaigns = allCampaigns.Where(c => campaignIds.Contains(c.Id));
            }
            
            // Filter by client IDs if provided (support both clientIds and filterByClientIds parameters)
            var effectiveClientIds = clientIds ?? filterByClientIds;
            if (!string.IsNullOrWhiteSpace(effectiveClientIds))
            {
                var clientIdList = effectiveClientIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToArray();
                    
                if (clientIdList.Any())
                {
                    // Filter campaigns by specified client IDs
                    allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && clientIdList.Contains(c.ClientId));
                }
            }

            // Handle user-based filtering if provided
            if (!string.IsNullOrEmpty(filterByUserIds))
            {
                var requestedUserIds = filterByUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();

                if (requestedUserIds.Any())
                {
                    // Get client IDs for the specified users
                    var userClientIds = new HashSet<string>();
                    
                    foreach (var userId in requestedUserIds)
                    {
                        var user = await _authService.GetUserByIdAsync(userId);
                        if (user != null && user.AssignedClientIds != null)
                        {
                            foreach (var clientId in user.AssignedClientIds)
                            {
                                userClientIds.Add(clientId);
                            }
                        }
                    }

                    if (userClientIds.Any())
                    {
                        // Apply additional filtering based on user's assigned clients
                        if (assignedClientIds == null)
                        {
                            // Admin user - filter by user's client IDs directly
                            allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && userClientIds.Contains(c.ClientId));
                        }
                        else
                        {
                            // Regular user - intersect with their assigned client IDs
                            var intersectedClientIds = assignedClientIds.Intersect(userClientIds).ToList();
                            if (intersectedClientIds.Any())
                            {
                                allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && intersectedClientIds.Contains(c.ClientId));
                            }
                            else
                            {
                                // No intersection - return empty result
                                return Ok(new PaginatedResponse<CampaignDetailsDbModel>
                                {
                                    Data = new List<CampaignDetailsDbModel>(),
                                    TotalCount = 0,
                                    CurrentPage = page,
                                    PageSize = pageSize,
                                    TotalPages = 0,
                                    HasPrevious = false,
                                    HasNext = false
                                });
                            }
                        }
                    }
                    else
                    {
                        // No client IDs found for specified users - return empty result
                        return Ok(new PaginatedResponse<CampaignDetailsDbModel>
                        {
                            Data = new List<CampaignDetailsDbModel>(),
                            TotalCount = 0,
                            CurrentPage = page,
                            PageSize = pageSize,
                            TotalPages = 0,
                            HasPrevious = false,
                            HasNext = false
                        });
                    }
                }
            }
            
            // Filter by email account ID if provided
            if (emailAccountId.HasValue)
            {
                allCampaigns = allCampaigns.Where(c => c.EmailIds != null && c.EmailIds.Contains(emailAccountId.Value));
            }
            
            // Convert to list
            var campaignsList = allCampaigns.ToList();
            
            // Apply time range filtering to update stats based on selected period
            await ApplyTimeRangeFiltering(campaignsList, timeRangeDays);
            
            // Apply worst performing filter
            if (performanceFilterMinSent.HasValue && performanceFilterMaxReplyRate.HasValue)
            {
                campaignsList = campaignsList.Where(c => 
                    c.TotalSent >= performanceFilterMinSent.Value &&
                    (c.TotalSent > 0 ? (double)c.TotalReplied / c.TotalSent * 100 : 0) <= performanceFilterMaxReplyRate.Value
                ).ToList();
            }
            
            // Create a dictionary of all clients for better performance
            var allClients = (await _clientRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c);
            
            foreach (var campaign in campaignsList)
            {
                if (!string.IsNullOrEmpty(campaign.ClientId))
                {
                    if (allClients.TryGetValue(campaign.ClientId, out var client))
                    {
                        campaign.ClientName = client.Name;
                        campaign.ClientColor = client.Color;
                    }
                    else
                    {
                        // Debug: Check if there's a client ID mismatch
                        _logger.LogWarning("Campaign {CampaignName} has ClientId {ClientId} but no matching client found. Available client IDs: {ClientIds}", 
                            campaign.Name, campaign.ClientId, string.Join(", ", allClients.Keys.Take(5)));
                    }
                }
            }
            
            // Convert back to queryable for further operations
            allCampaigns = campaignsList.AsQueryable();

            // Apply search filter (now that ClientName is populated)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                
                allCampaigns = allCampaigns.Where(c => 
                    (c.Name != null && c.Name.ToLower().Contains(searchLower)) ||
                    (c.ClientName != null && c.ClientName.ToLower().Contains(searchLower)) ||
                    (c.Status != null && c.Status.ToLower().Contains(searchLower)) ||
                    (c.Id != null && c.Id.ToLower().Contains(searchLower)) ||
                    (c.Tags != null && c.Tags.Count > 0 && c.Tags.Any(tag => tag != null && tag.ToLower().Contains(searchLower))));
            }

            // Apply single-column sorting
            List<CampaignDetailsDbModel> finalCampaignsList;
            
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var campaignsForSorting = allCampaigns.ToList();
                var isDescending = !string.IsNullOrWhiteSpace(sortDirection) && sortDirection.ToLower() == "desc";
                
                // Determine if we're in percentage mode based on sortMode parameter
                bool isPercentageMode = !string.IsNullOrEmpty(sortMode) && sortMode.ToLower() == "percentage";
                
                finalCampaignsList = ApplySingleSortList(campaignsForSorting, sortBy, isDescending, isPercentageMode).ToList();
            }
            else
            {
                finalCampaignsList = allCampaigns.OrderByDescending(c => c.CreatedAt).ToList();
            }
            var totalCount = finalCampaignsList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var paginatedCampaigns = finalCampaignsList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
           
            PaginatedResponse<CampaignDetailsDbModel> response = new()
            {
                Data = paginatedCampaigns,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching campaigns");
            return StatusCode(500, new { message = "Error fetching campaigns" });
        }
    }

    [HttpPost("filter")]
    public async Task<ActionResult> GetCampaignsFiltered([FromBody] CampaignFilterRequest request)
    {
        try
        {
            // Validate pagination parameters
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 1000; // Increased default page size
            if (request.PageSize > 2000) request.PageSize = 2000; // Increased max page size

            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var allCampaigns = (await _campaignRepository.GetAllAsync()).AsQueryable();
            
            // Apply user-based filtering first
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
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
                allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId));
            }

            // Filter by client IDs from request if provided
            if (request.ClientIds != null && request.ClientIds.Any())
            {
                allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && request.ClientIds.Contains(c.ClientId));
            }

            // Get total count before pagination
            var totalCount = allCampaigns.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // Apply pagination
            var paginatedCampaigns = allCampaigns
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Create response with pagination info - optimized payload with only required fields
            var response = new
            {
                success = true,
                data = paginatedCampaigns.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    clientId = c.ClientId,
                    clientName = c.ClientName
                    // Removed unused fields: campaignId, status, totalLeads, totalSent, totalOpened, 
                    // totalReplied, totalBounced, totalClicked, positiveReplies
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
            return StatusCode(500, new { message = "Error fetching filtered campaigns" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignDetailsDbModel>> GetCampaign(string id)
    {
        try
        {
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }
            
            // Populate client name and color
            if (!string.IsNullOrEmpty(campaign.ClientId))
            {
                var client = await _clientRepository.GetByIdAsync(campaign.ClientId);
                campaign.ClientName = client?.Name;
                campaign.ClientColor = client?.Color;
            }
            
            return Ok(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error fetching campaign" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDetailsDbModel>> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Only admins can create campaigns
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            // Validate that client exists
            var client = await _clientRepository.GetByIdAsync(request.ClientId);
            if (client == null)
            {
                return BadRequest(new { message = "Selected client not found" });
            }

            var campaign = new CampaignDetailsDbModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                ClientId = request.ClientId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _campaignRepository.CreateAsync(campaign);
            
            // Populate client name for response
            campaign.ClientName = client.Name;

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return StatusCode(500, new { message = "Error creating campaign" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CampaignDetailsDbModel>> UpdateCampaign(string id, [FromBody] UpdateCampaignRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Only admins can update campaigns (including client assignment)
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            var existingCampaign = await _campaignRepository.GetByIdAsync(id);
            if (existingCampaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            // Validate that client exists (only if ClientId is provided)
            Client? client = null;
            if (!string.IsNullOrEmpty(request.ClientId))
            {
                client = await _clientRepository.GetByIdAsync(request.ClientId);
                if (client == null)
                {
                    return BadRequest(new { message = "Selected client not found" });
                }
            }

            // Update campaign
            existingCampaign.Name = request.Name;
            existingCampaign.ClientId = request.ClientId;
            existingCampaign.UpdatedAt = DateTime.UtcNow;

            await _campaignRepository.UpdateAsync(existingCampaign);
            
            // Populate client name for response
            existingCampaign.ClientName = client?.Name;
            existingCampaign.ClientColor = client?.Color;

            return Ok(existingCampaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error updating campaign" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCampaign(string id)
    {
        try
        {
            // Only admins can delete campaigns
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            await _campaignRepository.DeleteAsync(id);

            return Ok(new { message = "Campaign deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error deleting campaign" });
        }
    }

    [HttpPost("bulk-delete")]
    public async Task<ActionResult> BulkDeleteCampaigns([FromBody] BulkDeleteRequest request)
    {
        if (request?.CampaignIds == null || !request.CampaignIds.Any())
        {
            return BadRequest(new { message = "No campaign IDs provided" });
        }

        try
        {
            // Only admins can bulk delete campaigns
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            int deletedCount = 0;
            var failedDeletes = new List<string>();

            foreach (var campaignId in request.CampaignIds)
            {
                try
                {
                    var campaign = await _campaignRepository.GetByIdAsync(campaignId);
                    if (campaign != null)
                    {
                        var deleted = await _campaignRepository.DeleteAsync(campaignId);
                        if (deleted)
                        {
                            deletedCount++;
                        }
                        else
                        {
                            failedDeletes.Add(campaignId);
                        }
                    }
                    else
                    {
                        failedDeletes.Add(campaignId); // Campaign not found
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting campaign {CampaignId}", campaignId);
                    failedDeletes.Add(campaignId);
                }
            }

            var response = new
            {
                deletedCount = deletedCount,
                failedCount = failedDeletes.Count,
                failedCampaignIds = failedDeletes,
                message = $"Successfully deleted {deletedCount} out of {request.CampaignIds.Count} campaign(s)"
            };

            if (failedDeletes.Any())
            {
                _logger.LogWarning("Failed to delete {FailedCount} campaigns out of {TotalCount}", 
                    failedDeletes.Count, request.CampaignIds.Count);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete operation");
            return StatusCode(500, new { message = "Error during bulk delete operation" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResponse<CampaignDetailsDbModel>>> SearchCampaigns(
        [FromQuery] string q,
        [FromQuery] string? clientIds = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? filterByClientIds = null)
    {
        try
        {
            // Validate search query
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new PaginatedResponse<CampaignDetailsDbModel>
                {
                    Data = new List<CampaignDetailsDbModel>(),
                    CurrentPage = 1,
                    PageSize = limit,
                    TotalCount = 0,
                    TotalPages = 0,
                    HasPrevious = false,
                    HasNext = false
                });
            }

            var allCampaigns = (await _campaignRepository.GetAllAsync()).AsQueryable();
            
            // Filter by client IDs if provided (support both clientIds and filterByClientIds parameters)
            var effectiveClientIds = clientIds ?? filterByClientIds;
            if (!string.IsNullOrWhiteSpace(effectiveClientIds))
            {
                var clientIdList = effectiveClientIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToArray();
                    
                if (clientIdList.Any())
                {
                    // Filter campaigns by specified client IDs
                    allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && clientIdList.Contains(c.ClientId));
                }
            }

            // Apply search filter
            var searchLower = q.ToLower();
            allCampaigns = allCampaigns.Where(c => 
                (c.Name != null && c.Name.ToLower().Contains(searchLower)) ||
                (c.Status != null && c.Status.ToLower().Contains(searchLower)));

            // Get campaigns and populate client names
            var campaignsList = allCampaigns.ToList();
            var clients = (await _clientRepository.GetAllAsync()).ToDictionary(c => c.Id);
            
            foreach (var campaign in campaignsList)
            {
                if (!string.IsNullOrEmpty(campaign.ClientId) && clients.ContainsKey(campaign.ClientId))
                {
                    campaign.ClientName = clients[campaign.ClientId].Name;
                    campaign.ClientColor = clients[campaign.ClientId].Color;
                }
            }
            
            // Apply client name search filter after populating client names
            campaignsList = campaignsList.Where(c => 
                (c.Name != null && c.Name.ToLower().Contains(searchLower)) ||
                (c.ClientName != null && c.ClientName.ToLower().Contains(searchLower)) ||
                (c.Status != null && c.Status.ToLower().Contains(searchLower)) ||
                (c.Tags != null && c.Tags.Any(tag => tag.ToLower().Contains(searchLower)))).ToList();

            var totalCount = campaignsList.Count;
            
            // Apply offset and limit
            var paginatedCampaigns = campaignsList
                .Skip(offset)
                .Take(limit)
                .ToList();

            var currentPage = (offset / limit) + 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)limit);

            return Ok(new PaginatedResponse<CampaignDetailsDbModel>
            {
                Data = paginatedCampaigns,
                CurrentPage = currentPage,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = currentPage > 1,
                HasNext = currentPage < totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching campaigns");
            return StatusCode(500, new { message = "Error searching campaigns" });
        }
    }

    // [HttpGet("{id}/analytics")]
    // public ActionResult<CampaignAnalyticsResponse> GetCampaignAnalytics(string id)
    // {
    //     try
    //     {
    //         var campaign = _dbService.FindById<CampaignSummaryDbModel>(id);
    //         if (campaign == null)
    //         {
    //             return NotFound(new { message = "Campaign not found" });
    //         }
    //
    //         // Populate client name and color
    //         if (!string.IsNullOrEmpty(campaign.ClientId))
    //         {
    //             var client = _dbService.FindById<Client>(campaign.ClientId);
    //             campaign.ClientName = client?.Name;
    //             campaign.ClientColor = client?.Color;
    //         }
    //
    //         return NotFound(new { message = "Campaign analytics not found" });
    //         // Get or generate analytics data
    //         // var analytics = GetOrGenerateAnalytics(campaign);
    //         
    //         // return Ok(analytics);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error fetching campaign analytics for {CampaignId}", id);
    //         return StatusCode(500, new { message = "Error fetching campaign analytics" });
    //     }
    // }

    [HttpGet("download")]
    public async Task<ActionResult> DownloadCampaigns([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string? clientIds = null, [FromQuery] string? filterByClientIds = null)
    {
        try
        {
            // TODO: Implement CSV generation logic
            // This is a placeholder endpoint for the frontend to call
            // You can implement the CSV export functionality here
            
            bool isAllTimeData = string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate);
            
            // Parse date parameters
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;
            
            if (!isAllTimeData)
            {
                if (DateTime.TryParse(startDate, out var parsedStartDate))
                {
                    startDateTime = parsedStartDate.Date;
                }
                
                if (DateTime.TryParse(endDate, out var parsedEndDate))
                {
                    endDateTime = parsedEndDate.Date.AddDays(1).AddTicks(-1); // End of day
                }
            }
            
            // Get all campaigns
            var campaigns = (await _campaignRepository.GetAllAsync()).AsQueryable();
            
            // Apply date range filter if dates are provided (not all-time data)
            if (!isAllTimeData && startDateTime.HasValue && endDateTime.HasValue)
            {
                campaigns = campaigns.Where(c => c.CreatedAt >= startDateTime.Value && c.CreatedAt <= endDateTime.Value);
            }
            
            // Filter by client IDs if provided (support both clientIds and filterByClientIds parameters)
            var effectiveClientIds = clientIds ?? filterByClientIds;
            if (!string.IsNullOrEmpty(effectiveClientIds))
            {
                var selectedClientIds = effectiveClientIds.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList();
                if (selectedClientIds.Any())
                {
                    campaigns = campaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && selectedClientIds.Contains(c.ClientId));
                }
            }
            
            var campaignsList = campaigns.ToList();
            var clients = (await _clientRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c);
            
            // Create CSV export models
            var csvData = campaignsList.Select(campaign => new CampaignCsvModel
            {
                Id = campaign.Id ?? "",
                Name = campaign.Name ?? "",
                ClientName = !string.IsNullOrEmpty(campaign.ClientId) && clients.ContainsKey(campaign.ClientId) 
                    ? clients[campaign.ClientId].Name ?? "Unassigned"
                    : "Unassigned",
                Status = campaign.Status ?? "",
                TotalLeads = campaign.TotalLeads ?? 0,
                TotalSent = campaign.TotalSent ?? 0,
                TotalOpened = campaign.TotalOpened ?? 0,
                TotalReplied = campaign.TotalReplied ?? 0,
                TotalPositiveReplies = campaign.TotalPositiveReplies ?? 0,
                TotalBounced = campaign.TotalBounced ?? 0,
                TotalClicked = campaign.TotalClicked ?? 0,
                // Sent24Hours = campaign.Sent24Hours ?? 0,
                // Opened24Hours = campaign.Opened24Hours ?? 0,
                // Replied24Hours = campaign.Replied24Hours ?? 0,
                // Clicked24Hours = campaign.Clicked24Hours ?? 0,
                // Sent7Days = campaign.Sent7Days ?? 0,
                // Opened7Days = campaign.Opened7Days ?? 0,
                // Replied7Days = campaign.Replied7Days ?? 0,
                // Clicked7Days = campaign.Clicked7Days ?? 0,
                CreatedAt = campaign.CreatedAt,
                UpdatedAt = campaign.UpdatedAt
            }).ToList();
            
            // Generate CSV using CsvHelper
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });
            
            csv.WriteRecords(csvData);
            writer.Flush();
            
            var bytes = memoryStream.ToArray();
            var fileName = isAllTimeData 
                ? "campaigns_all_time.csv"
                : $"campaigns_{startDate}_to_{endDate}.csv";
            
            
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading campaigns CSV");
            return StatusCode(500, new { message = "Error downloading campaigns data" });
        }
    }



    [HttpPost("{id}/tags")]
    public async Task<ActionResult> AssignTagsToCampaign(string id, [FromBody] List<string> tags)
    {
        try
        {
            // Only admins can assign tags to campaigns
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            
            var campaign = await _campaignRepository.GetByIdAsync(id);

            if (campaign == null)
            {
                _logger.LogWarning("Campaign not found with ID: {CampaignId}", id);
                return NotFound(new { message = "Campaign not found" });
            }

            // Simply store the tag names as strings
            campaign.Tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
            campaign.UpdatedAt = DateTime.UtcNow;
            await _campaignRepository.UpdateAsync(campaign);

            return Ok(new { message = $"Successfully assigned {campaign.Tags.Count} tags to campaign", tags = campaign.Tags });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning tags to campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error assigning tags to campaign" });
        }
    }

    [HttpDelete("{id}/tags/{tagName}")]
    public async Task<ActionResult> RemoveTagFromCampaign(string id, string tagName)
    {
        try
        {
            // Only admins can remove tags from campaigns
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            var campaign = await _campaignRepository.GetByIdAsync(id);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            if (campaign.Tags.Contains(tagName))
            {
                campaign.Tags.Remove(tagName);
                campaign.UpdatedAt = DateTime.UtcNow;
                await _campaignRepository.UpdateAsync(campaign);
                
                return Ok(new { message = "Tag removed from campaign successfully" });
            }

            return NotFound(new { message = "Tag not found on this campaign" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag from campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error removing tag from campaign" });
        }
    }

    [HttpGet("by-campaign-id/{campaignId}")]
    public async Task<ActionResult<object>> GetCampaignByCampaignId(int campaignId)
    {
        try
        {
            // Find campaign by campaignId field
            var campaigns = await _campaignRepository.GetAllAsync();
            var campaign = campaigns.FirstOrDefault(c => c.CampaignId == campaignId);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            // Populate client name and color
            if (!string.IsNullOrEmpty(campaign.ClientId))
            {
                var client = await _clientRepository.GetByIdAsync(campaign.ClientId);
                campaign.ClientName = client?.Name;
                campaign.ClientColor = client?.Color;
            }

            // Fetch daily stats directly from campaign_events table (event-sourced system)
            // Note: campaign_events uses the numeric campaign_id as string, not the UUID
            using var connection = await _connectionService.GetConnectionAsync();
            const string dailyStatsSql = @"
                SELECT
                    DATE(event_date) as date,
                    event_type,
                    SUM(event_count) as total_count
                FROM campaign_events
                WHERE campaign_id = @CampaignId
                GROUP BY DATE(event_date), event_type
                ORDER BY DATE(event_date) ASC";

            var dailyStatsRaw = await connection.QueryAsync<dynamic>(dailyStatsSql,
                new { CampaignId = campaign.CampaignId.ToString() });

            // Convert daily stats to dictionary format (date -> count) for each metric
            var sent = new Dictionary<string, int>();
            var opened = new Dictionary<string, int>();
            var clicked = new Dictionary<string, int>();
            var replied = new Dictionary<string, int>();
            var positiveReplies = new Dictionary<string, int>();

            foreach (var stat in dailyStatsRaw)
            {
                var dateKey = ((DateTime)stat.date).ToString("yyyy-MM-dd");
                var eventType = (string)stat.event_type;
                var count = (int)(long)stat.total_count;

                switch (eventType)
                {
                    case "sent":
                        sent[dateKey] = count;
                        break;
                    case "opened":
                        opened[dateKey] = count;
                        break;
                    case "clicked":
                        clicked[dateKey] = count;
                        break;
                    case "replied":
                        replied[dateKey] = count;
                        break;
                    case "positive_reply":
                        positiveReplies[dateKey] = count;
                        break;
                }
            }

            // Return campaign details with daily stats
            return Ok(new
            {
                id = campaign.Id,
                campaignId = campaign.CampaignId,
                name = campaign.Name,
                totalLeads = campaign.TotalLeads,
                totalSent = campaign.TotalSent,
                totalReplied = campaign.TotalReplied,
                totalPositiveReplies = campaign.TotalPositiveReplies,
                clientName = campaign.ClientName,
                clientColor = campaign.ClientColor,
                status = campaign.Status,
                createdAt = campaign.CreatedAt,
                updatedAt = campaign.UpdatedAt,
                emailIds = campaign.EmailIds,
                sent = sent,
                opened = opened,
                clicked = clicked,
                replied = replied,
                positiveReplies = positiveReplies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching campaign analytics by campaignId {CampaignId}", campaignId);
            return StatusCode(500, new { message = "Error fetching campaign analytics" });
        }
    }

    [HttpGet("{campaignId}/positive-replies")]
    public ActionResult<object> GetPositiveReplies(int campaignId)
    {
        try
        {
            return Ok(new 
            {
                
            });
            
            // // First check if campaign exists by campaignId
            // var campaign = _dbService.FindAll<CampaignDetailsDbModel>()
            //     .FirstOrDefault(c => c.CampaignId == campaignId);
            // if (campaign == null)
            // {
            //     return NotFound(new { message = "Campaign not found" });
            // }
            //
            // // Get positive replies data using campaign ID
            // var positiveReplies = _dbService.FindAll<PositiveReplyDbModel>()
            //     .FirstOrDefault(pr => pr.CampaignId == campaignId.ToString());
            //
            // if (positiveReplies == null || positiveReplies.Stats == null || !positiveReplies.Stats.Any())
            // {
            //     // Return empty data structure
            //     return Ok(new
            //     {
            //         campaignId = campaignId,
            //         data = new List<object>()
            //     });
            // }
            //
            // // Convert to frontend-friendly format
            // var chartData = positiveReplies.Stats
            //     .Select(kvp => new
            //     {
            //         date = kvp.Key, // Already a string in yyyy-MM-dd format
            //         count = kvp.Value,
            //         parsedDate = DateTime.TryParse(kvp.Key, out var dt) ? dt : DateTime.MinValue
            //     })
            //     .OrderBy(item => item.parsedDate)
            //     .Select(item => new
            //     {
            //         date = item.date,
            //         count = item.count
            //     })
            //     .ToList();
            //
            // return Ok(new
            // {
            //     campaignId = campaignId,
            //     data = chartData,
            //     totalPositiveReplies = positiveReplies.Stats.Values.Sum()
            // });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching positive replies for campaign {CampaignId}", campaignId);
            return StatusCode(500, new { message = "Error fetching positive replies data" });
        }
    }

    [HttpGet("{id}/daily-stats")]
    public async Task<ActionResult<object>> GetCampaignDailyStats(string id)
    {
        try
        {
            // Find campaign by ID
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            // For now, return empty data structure
            // TODO: Implement actual daily stats fetching using _dailyStatsService
            var emptyStats = new Dictionary<string, int>();
            
            return Ok(new
            {
                campaignId = id,
                sent = emptyStats,
                opened = emptyStats,
                clicked = emptyStats,
                replied = emptyStats,
                positiveReplies = emptyStats,
                bounced = emptyStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily stats for campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error fetching campaign daily stats" });
        }
    }

    [HttpGet("{campaignId}/analytics")]
    public async Task<ActionResult<CampaignDetailsDbModel>> GetCampaignAnalytics(int campaignId)
    {
        try
        {
            // Find campaign by campaignId field (integer)
            var campaigns = await _campaignRepository.GetAllAsync();
            var campaign = campaigns.FirstOrDefault(c => c.CampaignId == campaignId);
            
            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }
            
            // Populate client name and color
            if (!string.IsNullOrEmpty(campaign.ClientId))
            {
                var client = await _clientRepository.GetByIdAsync(campaign.ClientId);
                campaign.ClientName = client?.Name;
                campaign.ClientColor = client?.Color;
            }
            
            return Ok(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching campaign analytics for campaign {CampaignId}", campaignId);
            return StatusCode(500, new { message = "Error fetching campaign analytics" });
        }
    }

    /// <summary>
    /// Bulk assign client to multiple campaigns
    /// </summary>
    /// <param name="request">Request containing campaign IDs and client ID</param>
    /// <returns>Success response with updated campaign count</returns>
    [HttpPut("bulk-assign-client")]
    public async Task<ActionResult> BulkAssignClient([FromBody] BulkAssignClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Only admins can bulk assign clients
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }

            // Validate that client exists (only if ClientId is provided)
            Client? client = null;
            if (!string.IsNullOrEmpty(request.ClientId))
            {
                client = await _clientRepository.GetByIdAsync(request.ClientId);
                if (client == null)
                {
                    return BadRequest(new { message = "Selected client not found" });
                }
            }

            int updatedCount = 0;
            var failedCampaigns = new List<string>();

            // Update each campaign
            foreach (var campaignId in request.CampaignIds)
            {
                try
                {
                    var existingCampaign = await _campaignRepository.GetByIdAsync(campaignId);
                    if (existingCampaign == null)
                    {
                        failedCampaigns.Add($"Campaign {campaignId} not found");
                        continue;
                    }

                    // Update campaign
                    existingCampaign.ClientId = request.ClientId;
                    existingCampaign.UpdatedAt = DateTime.UtcNow;

                    await _campaignRepository.UpdateAsync(existingCampaign);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating campaign {CampaignId} during bulk assign", campaignId);
                    failedCampaigns.Add($"Campaign {campaignId} update failed");
                }
            }

            var response = new
            {
                message = $"Client assigned to {updatedCount} of {request.CampaignIds.Count} campaigns",
                updatedCount = updatedCount,
                totalRequested = request.CampaignIds.Count,
                failedCampaigns = failedCampaigns
            };

            if (failedCampaigns.Any())
            {
                _logger.LogWarning("Bulk assign client completed with some failures: {FailedCount} out of {TotalCount}", 
                    failedCampaigns.Count, request.CampaignIds.Count);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk client assignment");
            return StatusCode(500, new { message = "Error during bulk client assignment" });
        }
    }

    [HttpPut("{id}/notes")]
    public async Task<ActionResult> UpdateCampaignNotes(string id, [FromBody] UpdateCampaignNotesRequest request)
    {
        try
        {
            // Only admins can update campaign notes
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            var campaign = await _campaignRepository.GetByIdAsync(id);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            campaign.Notes = request.Notes;
            campaign.UpdatedAt = DateTime.UtcNow;
            await _campaignRepository.UpdateAsync(campaign);
            
            _logger.LogInformation("Updated notes for campaign {CampaignId}", id);
            return Ok(new { message = "Notes updated successfully", notes = campaign.Notes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notes for campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error updating campaign notes" });
        }
    }

    [HttpGet("{id}/email-accounts")]
    public async Task<ActionResult<List<EmailAccountDbModel>>> GetCampaignEmailAccounts(string id)
    {
        try
        {
            // Try to parse as integer (campaignId) first
            CampaignDetailsDbModel? campaign = null;
            
            if (int.TryParse(id, out int campaignId))
            {
                // It's a numeric campaign ID, find by CampaignId field
                var campaigns = await _campaignRepository.GetAllAsync();
                campaign = campaigns.FirstOrDefault(c => c.CampaignId == campaignId);
            }
            else
            {
                // It's a string ID, find by Id field
                campaign = await _campaignRepository.GetByIdAsync(id);
            }
            
            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            // Check if user has access to this campaign based on client assignments
            var assignedClientIds = await GetUserAssignedClientIds();
            if (assignedClientIds != null && campaign.ClientId != null && !assignedClientIds.Contains(campaign.ClientId))
            {
                return Forbid("You don't have access to this campaign");
            }

            if (campaign.EmailIds == null || !campaign.EmailIds.Any())
            {
                return Ok(new List<EmailAccountDbModel>());
            }

            // Get email accounts linked to this campaign
            var (emailAccounts, _) = await _emailAccountRepository.GetPaginatedAsync(
                page: 1, 
                pageSize: 1000, // Get all linked accounts
                emailIds: campaign.EmailIds.ToList()
            );

            var result = emailAccounts.Select(account => new 
            {
                id = account.Id,
                email = account.Email,
                name = account.Name,
                status = account.Status,
                clientName = account.ClientName,
                clientColor = account.ClientColor
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email accounts for campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("list")]
    public async Task<ActionResult<PaginatedResponse<CampaignListItem>>> GetCampaignList(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 1000, // Default high limit for backward compatibility
        [FromQuery] int offset = 0)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var campaigns = (await _campaignRepository.GetAllAsync()).ToList();

            // Apply user-based filtering
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
                {
                    // User has no assigned campaigns, return empty result
                    return Ok(new PaginatedResponse<CampaignListItem>
                    {
                        Data = new List<CampaignListItem>(),
                        CurrentPage = 1,
                        PageSize = limit,
                        TotalCount = 0,
                        TotalPages = 0,
                        HasPrevious = false,
                        HasNext = false
                    });
                }
                campaigns = campaigns.Where(c => c.ClientId != null && assignedClientIds.Contains(c.ClientId)).ToList();
            }

            // Apply search filter
            var filteredCampaigns = campaigns
                .Where(c => string.IsNullOrWhiteSpace(search) || 
                           c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name)
                .ToList();
            
            // Calculate pagination
            var totalCount = filteredCampaigns.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)limit);
            var currentPage = (offset / limit) + 1;
            
            // Apply pagination
            var paginatedCampaigns = filteredCampaigns
                .Skip(offset)
                .Take(limit)
                .Select(c => new CampaignListItem 
                { 
                    Id = c.Id, 
                    Name = c.Name
                })
                .ToList();
            
            return Ok(new PaginatedResponse<CampaignListItem>
            {
                Data = paginatedCampaigns,
                CurrentPage = currentPage,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = offset > 0,
                HasNext = (offset + limit) < totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching campaign list");
            return StatusCode(500, new { message = "Error fetching campaign list" });
        }
    }

    [HttpGet("{id}/lead-conversations")]
    public async Task<ActionResult> GetLeadConversations(string id)
    {
        try
        {
            // Try to parse as integer (campaignId) first, then try as string ID
            CampaignDetailsDbModel? campaign = null;
            int campaignId = 0;
            
            if (int.TryParse(id, out campaignId))
            {
                // It's a numeric campaign ID, find by CampaignId field
                var campaigns = await _campaignRepository.GetAllAsync();
                campaign = campaigns.FirstOrDefault(c => c.CampaignId == campaignId);
            }
            else
            {
                // It's a string ID, find by Id field
                campaign = await _campaignRepository.GetByIdAsync(id);
                campaignId = campaign?.CampaignId ?? 0;
            }
            
            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            // Check if user has access to this campaign based on client assignments
            var assignedClientIds = await GetUserAssignedClientIds();
            if (assignedClientIds != null && campaign.ClientId != null && !assignedClientIds.Contains(campaign.ClientId))
            {
                return Forbid("You don't have access to this campaign");
            }

            // Fetch lead conversations from database
            var leadConversations = (await _leadConversationRepository.GetByCampaignIdAsync(campaignId)).ToList();
            
            // Fetch email history for all leads in this campaign
            var emailHistory = await _leadEmailHistoryRepository.GetByCampaignIdAsync(campaignId);
            
            // Group email history by lead ID
            var historyByLeadId = emailHistory.GroupBy(h => h.LeadId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.SequenceNumber).ToList());

            // Build response combining lead data and email history
            var response = leadConversations.Select(lead => {
                // Extract leadId from ConversationData if available
                string leadId = "";
                try
                {
                    if (!string.IsNullOrEmpty(lead.ConversationData))
                    {
                        var conversationData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(lead.ConversationData);
                        if (conversationData.TryGetProperty("Id", out var idProperty))
                        {
                            leadId = idProperty.GetString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse conversation data for lead {LeadId}", lead.Id);
                }

                return new
                {
                    id = lead.Id,
                    leadId = leadId,
                    leadEmail = lead.LeadEmail,
                    leadFirstName = lead.LeadFirstName,
                    leadLastName = lead.LeadLastName,
                    status = lead.Status,
                    createdAt = lead.CreatedAt,
                    updatedAt = lead.UpdatedAt,
                    emailHistory = historyByLeadId.ContainsKey(leadId) 
                        ? historyByLeadId[leadId].Select(h => new
                        {
                            subject = h.Subject,
                            body = h.Body,
                            sequenceNumber = h.SequenceNumber,
                            createdAt = h.CreatedAt
                        }).Cast<object>().ToList()
                        : new List<object>()
                };
            }).ToList();

            return Ok(new
            {
                campaignId = campaignId,
                campaignName = campaign.Name,
                totalLeads = leadConversations.Count,
                leads = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lead conversations for campaign {CampaignId}", id);
            return StatusCode(500, new { message = "Error fetching lead conversations" });
        }
    }

}

public class CampaignCsvModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalLeads { get; set; }
    public int TotalSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalReplied { get; set; }
    public int TotalPositiveReplies { get; set; }
    public int TotalBounced { get; set; }
    public int TotalClicked { get; set; }
    // public int Sent24Hours { get; set; }
    // public int Opened24Hours { get; set; }
    // public int Replied24Hours { get; set; }
    // public int Clicked24Hours { get; set; }
    // public int Sent7Days { get; set; }
    // public int Opened7Days { get; set; }
    // public int Replied7Days { get; set; }
    // public int Clicked7Days { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateCampaignNotesRequest
{
    public string? Notes { get; set; }
}

public class CampaignListItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}