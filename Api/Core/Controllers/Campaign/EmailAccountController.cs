using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Frontend;
using LeadHype.Api.Core.Models.API.Requests;
using LeadHype.Api.Services;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace LeadHype.Api.Core.Controllers;

[ApiController]
[Route("api/email-accounts")]
[Authorize]
public class EmailAccountController : ControllerBase
{
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IClassifiedEmailRepository _classifiedEmailRepository;
    private readonly ILogger<EmailAccountController> _logger;
    private readonly SmartleadSyncService _mapperService;
    private readonly IAuthService _authService;
    private readonly IEmailAccountDailyStatEntryService _dailyStatsService;

    public EmailAccountController(
        IEmailAccountRepository emailAccountRepository,
        IClientRepository clientRepository,
        ICampaignRepository campaignRepository,
        IClassifiedEmailRepository classifiedEmailRepository,
        ILogger<EmailAccountController> logger,
        SmartleadSyncService mapperService, 
        IAuthService authService, 
        IEmailAccountDailyStatEntryService dailyStatsService)
    {
        _emailAccountRepository = emailAccountRepository;
        _clientRepository = clientRepository;
        _campaignRepository = campaignRepository;
        _classifiedEmailRepository = classifiedEmailRepository;
        _logger = logger;
        _mapperService = mapperService;
        _authService = authService;
        _dailyStatsService = dailyStatsService;
    }

    private async Task<List<string>?> GetUserAssignedClientIds()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admin users can see all email accounts
        if (userRole == UserRoles.Admin)
        {
            return null; // null means no filtering
        }
        
        // Regular users can only see email accounts from assigned clients
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return new List<string>(); // Empty list means no access
        
        var user = await _authService.GetUserByIdAsync(userId);
        return user?.AssignedClientIds ?? new List<string>();
    }

    /// <summary>
    /// Converts EmailAccountDbModel to EmailAccountSummaryDto
    /// </summary>
    private static EmailAccountSummaryDto ConvertToDto(EmailAccountDbModel account)
    {
        return new EmailAccountSummaryDto
        {
            Id = account.Id,
            Email = account.Email,
            Name = account.Name,
            Status = account.Status,
            ClientId = account.ClientId,
            ClientName = account.ClientName,
            ClientColor = account.ClientColor,
            WarmupSent = account.WarmupSent,
            WarmupReplied = account.WarmupReplied,
            WarmupSavedFromSpam = account.WarmupSavedFromSpam,
            Sent = account.Sent,
            Opened = account.Opened,
            Clicked = 0, // This field doesn't exist in DbModel
            Replied = account.Replied,
            Unsubscribed = 0, // This field doesn't exist in DbModel
            Bounced = account.Bounced,
            Tags = account.Tags,
            CampaignCount = account.CampaignCount,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };
    }


    /// <summary>
    /// Applies sorting to EmailAccount database models.
    /// NOTE: This method does not support sorting by positive replies since that data
    /// is fetched separately and added at the DTO level.
    /// For positive replies sorting, use the DTO-level sorting logic instead.
    /// </summary>
    private IOrderedEnumerable<EmailAccountDbModel> ApplySingleSortList(
        List<EmailAccountDbModel> accounts,
        string sortColumn,
        bool isDescending,
        bool isPercentageMode)
    {
        return sortColumn.ToLower() switch
        {
            "email" => isDescending ? accounts.OrderByDescending(e => e.Email) : accounts.OrderBy(e => e.Email),
            "name" => isDescending ? accounts.OrderByDescending(e => e.Name) : accounts.OrderBy(e => e.Name),
            "status" => isDescending ? accounts.OrderByDescending(e => e.Status) : accounts.OrderBy(e => e.Status),
            "sent" => isDescending ? accounts.OrderByDescending(e => e.Sent) : accounts.OrderBy(e => e.Sent),
            "totalsent" => isDescending ? accounts.OrderByDescending(e => e.Sent) : accounts.OrderBy(e => e.Sent),
            "opened" => isPercentageMode 
                ? (isDescending ? accounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Opened / e.Sent : 0) : accounts.OrderBy(e => e.Sent > 0 ? (double)e.Opened / e.Sent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.Opened) : accounts.OrderBy(e => e.Opened)),
            "totalopened" => isPercentageMode 
                ? (isDescending ? accounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Opened / e.Sent : 0) : accounts.OrderBy(e => e.Sent > 0 ? (double)e.Opened / e.Sent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.Opened) : accounts.OrderBy(e => e.Opened)),
            "replied" => isPercentageMode
                ? (isDescending ? accounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Replied / e.Sent : 0) : accounts.OrderBy(e => e.Sent > 0 ? (double)e.Replied / e.Sent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.Replied) : accounts.OrderBy(e => e.Replied)),
            "totalreplied" => isPercentageMode
                ? (isDescending ? accounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Replied / e.Sent : 0) : accounts.OrderBy(e => e.Sent > 0 ? (double)e.Replied / e.Sent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.Replied) : accounts.OrderBy(e => e.Replied)),
            "bounced" => isPercentageMode
                ? (isDescending ? accounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Bounced / e.Sent : 0) : accounts.OrderBy(e => e.Sent > 0 ? (double)e.Bounced / e.Sent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.Bounced) : accounts.OrderBy(e => e.Bounced)),
            "totalbounced" => isPercentageMode
                ? (isDescending ? accounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Bounced / e.Sent : 0) : accounts.OrderBy(e => e.Sent > 0 ? (double)e.Bounced / e.Sent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.Bounced) : accounts.OrderBy(e => e.Bounced)),
            "warmupsent" => isDescending ? accounts.OrderByDescending(e => e.WarmupSent) : accounts.OrderBy(e => e.WarmupSent),
            "warmupreplied" => isPercentageMode
                ? (isDescending ? accounts.OrderByDescending(e => e.WarmupSent > 0 ? (double)e.WarmupReplied / e.WarmupSent : 0) : accounts.OrderBy(e => e.WarmupSent > 0 ? (double)e.WarmupReplied / e.WarmupSent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.WarmupReplied) : accounts.OrderBy(e => e.WarmupReplied)),
            "warmupsavedfromspam" => isDescending ? accounts.OrderByDescending(e => e.WarmupSavedFromSpam) : accounts.OrderBy(e => e.WarmupSavedFromSpam),
            "warmupspamcount" => isPercentageMode
                ? (isDescending ? accounts.OrderByDescending(e => e.WarmupSent > 0 ? (double)e.WarmupSpamCount / e.WarmupSent : 0) : accounts.OrderBy(e => e.WarmupSent > 0 ? (double)e.WarmupSpamCount / e.WarmupSent : 0))
                : (isDescending ? accounts.OrderByDescending(e => e.WarmupSpamCount) : accounts.OrderBy(e => e.WarmupSpamCount)),
            // "sent7d" => isDescending ? accounts.OrderByDescending(e => e.Sent7d) : accounts.OrderBy(e => e.Sent7d),
            // "sent24h" => isDescending ? accounts.OrderByDescending(e => e.Sent24h) : accounts.OrderBy(e => e.Sent24h),
            // "replied7d" => isPercentageMode
            //     ? (isDescending ? accounts.OrderByDescending(e => e.Sent7d > 0 ? (double)e.Replied7d / e.Sent7d : 0) : accounts.OrderBy(e => e.Sent7d > 0 ? (double)e.Replied7d / e.Sent7d : 0))
            //     : (isDescending ? accounts.OrderByDescending(e => e.Replied7d) : accounts.OrderBy(e => e.Replied7d)),
            // "replied24h" => isPercentageMode
            //     ? (isDescending ? accounts.OrderByDescending(e => e.Sent24h > 0 ? (double)e.Replied24h / e.Sent24h : 0) : accounts.OrderBy(e => e.Sent24h > 0 ? (double)e.Replied24h / e.Sent24h : 0))
            //     : (isDescending ? accounts.OrderByDescending(e => e.Replied24h) : accounts.OrderBy(e => e.Replied24h)),
            "createdat" => isDescending ? accounts.OrderByDescending(e => e.CreatedAt) : accounts.OrderBy(e => e.CreatedAt),
            "updatedat" => isDescending ? accounts.OrderByDescending(e => e.UpdatedAt) : accounts.OrderBy(e => e.UpdatedAt),
            "lastupdatedat" => isDescending ? accounts.OrderByDescending(e => e.LastUpdatedAt ?? e.UpdatedAt) : accounts.OrderBy(e => e.LastUpdatedAt ?? e.UpdatedAt),
            "tags" => isDescending 
                ? accounts.OrderByDescending(e => e.Tags != null && e.Tags.Any() ? string.Join(", ", e.Tags.OrderBy(t => t)) : "")
                : accounts.OrderBy(e => e.Tags != null && e.Tags.Any() ? string.Join(", ", e.Tags.OrderBy(t => t)) : ""),
            "tagcount" => isDescending 
                ? accounts.OrderByDescending(e => e.Tags != null ? e.Tags.Count : 0)
                : accounts.OrderBy(e => e.Tags != null ? e.Tags.Count : 0),
            "campaigns" => isDescending 
                ? accounts.OrderByDescending(e => e.CampaignCount)
                : accounts.OrderBy(e => e.CampaignCount),
            "campaigncount" => isDescending 
                ? accounts.OrderByDescending(e => e.CampaignCount)
                : accounts.OrderBy(e => e.CampaignCount),
            "activecampaigns" => isDescending 
                ? accounts.OrderByDescending(e => e.ActiveCampaignCount)
                : accounts.OrderBy(e => e.ActiveCampaignCount),
            "activecampaigncount" => isDescending 
                ? accounts.OrderByDescending(e => e.ActiveCampaignCount)
                : accounts.OrderBy(e => e.ActiveCampaignCount),
            "issendingactualemails" => isDescending 
                ? accounts.OrderByDescending(e => e.IsSendingActualEmails.HasValue ? (e.IsSendingActualEmails.Value ? 2 : 1) : 0)
                : accounts.OrderBy(e => e.IsSendingActualEmails.HasValue ? (e.IsSendingActualEmails.Value ? 2 : 1) : 0),
            "sendingactualemails" => isDescending 
                ? accounts.OrderByDescending(e => e.IsSendingActualEmails.HasValue ? (e.IsSendingActualEmails.Value ? 2 : 1) : 0)
                : accounts.OrderBy(e => e.IsSendingActualEmails.HasValue ? (e.IsSendingActualEmails.Value ? 2 : 1) : 0),
            "sendingstatus" => isDescending 
                ? accounts.OrderByDescending(e => e.IsSendingActualEmails.HasValue ? (e.IsSendingActualEmails.Value ? 2 : 1) : 0)
                : accounts.OrderBy(e => e.IsSendingActualEmails.HasValue ? (e.IsSendingActualEmails.Value ? 2 : 1) : 0),
            "notes" => isDescending ? accounts.OrderByDescending(e => e.Notes ?? "") : accounts.OrderBy(e => e.Notes ?? ""),
            _ => accounts.OrderBy(e => e.Email)
        };
    }


    private async Task CalculateTimeRangeStatsBatch(List<EmailAccountDbModel> accounts, int? timeRangeDays)
    {
        if (!accounts.Any()) return;
        
        var accountIds = accounts.Select(a => a.Id).ToList();
        
        // If no time range specified or "all time" (9999 days), calculate total stats from daily stats
        if (!timeRangeDays.HasValue || timeRangeDays.Value >= 9999)
        {
            // Use batch processing for all-time stats
            const int allTimeDays = 3650; // ~10 years, should cover all historical data
            var batchStats = await _dailyStatsService.GetBatchTotalStatsAsync(accountIds, allTimeDays);
            
            // Update accounts with calculated totals, fallback to existing values if no daily stats
            foreach (var account in accounts)
            {
                if (batchStats.TryGetValue(account.Id, out var stats) && 
                    (stats.Sent > 0 || stats.Opened > 0 || stats.Replied > 0 || stats.Bounced > 0))
                {
                    // Use daily stats if available and non-zero
                    account.Sent = stats.Sent;
                    account.Opened = stats.Opened;
                    account.Replied = stats.Replied;
                    account.Bounced = stats.Bounced;
                }
                // Keep existing database values if daily stats are empty/zero
            }
        }
        else
        {
            // Calculate date range for time-specific stats
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-timeRangeDays.Value);
            
            // Use batch processing for time-range stats
            var batchStats = await _dailyStatsService.GetBatchTotalStatsAsync(accountIds, startDate, endDate);
            
            // Update accounts with time-range stats
            foreach (var account in accounts)
            {
                if (batchStats.TryGetValue(account.Id, out var stats))
                {
                    account.Sent = stats.Sent;
                    account.Opened = stats.Opened;
                    account.Replied = stats.Replied;
                    account.Bounced = stats.Bounced;
                }
                else
                {
                    // No stats found for this time range, set to zero
                    account.Sent = 0;
                    account.Opened = 0;
                    account.Replied = 0;
                    account.Bounced = 0;
                }
            }
        }
    }

    private async Task<EmailAccountDbModel> CalculateTimeRangeStats(EmailAccountDbModel account, int? timeRangeDays)
    {
        // If no time range specified or "all time" (9999 days), calculate total stats from daily stats
        if (!timeRangeDays.HasValue || timeRangeDays.Value >= 9999)
        {
            // Calculate all-time totals using optimized SQL aggregation (single query)
            const int allTimeDays = 3650; // ~10 years, should cover all historical data
            var allTimeStats = await _dailyStatsService.GetTotalStatsAsync(account.Id, allTimeDays);
            
            // Update the account with calculated totals
            if (allTimeStats != null)
            {
                account.Sent = allTimeStats.Sent;
                account.Opened = allTimeStats.Opened;
                account.Replied = allTimeStats.Replied;
                account.Bounced = allTimeStats.Bounced;
            }
            
            // Still calculate campaign count for all time view
            // account.CampaignCount = await CalculateCampaignCountAsync(account.Id);
            return account;
        }
        
        // Create a copy of the account for time-range calculations
        var accountCopy = new EmailAccountDbModel
        {
            AdminUuid = account.AdminUuid,
            Id = account.Id,
            Email = account.Email,
            Name = account.Name,
            Status = account.Status,
            ClientId = account.ClientId,
            ClientName = account.ClientName,
            ClientColor = account.ClientColor,
            WarmupSent = account.WarmupSent,
            WarmupReplied = account.WarmupReplied,
            WarmupSavedFromSpam = account.WarmupSavedFromSpam,
            Tags = account.Tags,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };
        
        // Calculate date range for time-specific stats
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-timeRangeDays.Value);
        
        // Calculate time-range specific totals using optimized SQL aggregation with specific dates
        var rangeStats = await _dailyStatsService.GetTotalStatsAsync(account.Id, startDate, endDate);
        
        if (rangeStats != null)
        {
            accountCopy.Sent = rangeStats.Sent;
            accountCopy.Opened = rangeStats.Opened;
            accountCopy.Replied = rangeStats.Replied;
            accountCopy.Bounced = rangeStats.Bounced;
        }
        
        // Calculate campaign count (this is not time-dependent, so same for any time range)
        // accountCopy.CampaignCount = await CalculateCampaignCountAsync(account.Id);
        
        return accountCopy;
    }
    
    private int CalculateMetricFromDictionary(Dictionary<string, int>? dictionary, DateTime cutoffDate)
    {
        if (dictionary == null || !dictionary.Any())
            return 0;
        
        int total = 0;
        foreach (var kvp in dictionary)
        {
            if (DateTime.TryParse(kvp.Key, out DateTime date))
            {
                if (date >= cutoffDate)
                {
                    total += kvp.Value;
                }
            }
        }
        
        return total;
    }
    
    // Cache to avoid recalculating campaign counts on every request
    private static readonly Dictionary<long, int> _campaignCountCache = new();
    private static DateTime _campaignCountCacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan _campaignCountCacheLifetime = TimeSpan.FromMinutes(5);
    // private static bool _campaignDataCorrupted = false;

    // private async Task<int> CalculateCampaignCountAsync(long emailAccountId)
    // {
    //     // Check cache first
    //     if (DateTime.UtcNow < _campaignCountCacheExpiry && _campaignCountCache.ContainsKey(emailAccountId))
    //     {
    //         return _campaignCountCache[emailAccountId];
    //     }
    //
    //     // If cache is expired, rebuild it for all accounts
    //     if (DateTime.UtcNow >= _campaignCountCacheExpiry)
    //     {
    //         await RebuildCampaignCountCacheAsync();
    //     }
    //
    //     // Return cached value or 0 if not found
    //     return _campaignCountCache.GetValueOrDefault(emailAccountId, 0);
    // }

    // private async Task RebuildCampaignCountCacheAsync()
    // {
    //     try
    //     {
    //         _campaignCountCache.Clear();
    //         
    //         var campaigns = await _campaignRepository.GetAllAsync();
    //         
    //         if (!campaigns.Any())
    //         {
    //             // No campaigns, set cache expiry and return
    //             _campaignCountCacheExpiry = DateTime.UtcNow.Add(_campaignCountCacheLifetime);
    //             return;
    //         }
    //
    //         // Build count dictionary for all email accounts at once
    //         var emailAccountCounts = new Dictionary<long, int>();
    //         
    //         foreach (var campaign in campaigns)
    //         {
    //             if (campaign?.EmailIds != null)
    //             {
    //                 foreach (var emailId in campaign.EmailIds)
    //                 {
    //                     emailAccountCounts[emailId] = emailAccountCounts.GetValueOrDefault(emailId, 0) + 1;
    //                 }
    //             }
    //         }
    //
    //         // Update cache with collected data
    //         foreach (var kvp in emailAccountCounts)
    //         {
    //             _campaignCountCache[kvp.Key] = kvp.Value;
    //         }
    //         
    //         _campaignCountCacheExpiry = DateTime.UtcNow.Add(_campaignCountCacheLifetime);
    //         _campaignDataCorrupted = false;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error rebuilding campaign count cache");
    //         _campaignCountCacheExpiry = DateTime.UtcNow.Add(TimeSpan.FromMinutes(1));
    //     }
    // }
    //

    // [HttpPost("reset-campaign-cache")]
    // public ActionResult ResetCampaignCache()
    // {
    //     _campaignDataCorrupted = false;
    //     _campaignCountCacheExpiry = DateTime.MinValue;
    //     _campaignCountCache.Clear();
    //     
    //     _logger.LogInformation("Campaign count cache and corruption flag reset manually");
    //     
    //     return Ok(new { message = "Campaign count cache reset successfully", timestamp = DateTime.UtcNow });
    // }

    /// <summary>
    /// Get email accounts filtered by campaign IDs using POST to avoid header size issues
    /// </summary>
    /// <param name="request">Filter request with campaign IDs</param>
    /// <returns>Filtered email accounts</returns>
    [HttpPost("filter")]
    public async Task<ActionResult<List<EmailAccountSummaryDto>>> GetFilteredEmailAccounts([FromBody] EmailAccountFilterRequest request)
    {
        try
        {
            // Get user's assigned client IDs for access control
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // If user has no assigned clients and is not admin, return empty result
            if (assignedClientIds != null && !assignedClientIds.Any())
            {
                return Ok(new { data = new List<object>() });
            }

            // Get all email accounts first
            var allEmailAccounts = await _emailAccountRepository.GetAllAsync();
            
            // Apply client access filtering for non-admin users
            if (assignedClientIds != null)
            {
                allEmailAccounts = allEmailAccounts.Where(ea => assignedClientIds.Contains(ea.AdminUuid)).ToList();
            }

            // If campaign IDs are provided, filter by campaigns that use these email accounts
            if (request.CampaignIds != null && request.CampaignIds.Any())
            {
                // Get all campaigns for the provided campaign IDs
                var allCampaigns = await _campaignRepository.GetAllAsync();
                var targetCampaigns = allCampaigns.Where(c => request.CampaignIds.Contains(c.Id)).ToList();
                
                // Get all email IDs used by these campaigns
                var usedEmailIds = targetCampaigns
                    .Where(c => c.EmailIds != null)
                    .SelectMany(c => c.EmailIds)
                    .Distinct()
                    .ToHashSet();

                // Filter email accounts to only those used by the campaigns
                allEmailAccounts = allEmailAccounts.Where(ea => usedEmailIds.Contains(ea.Id)).ToList();
            }

            // Convert to DTOs
            var emailAccountDtos = allEmailAccounts.Select(ea => new EmailAccountSummaryDto
            {
                Id = ea.Id,
                Email = ea.Email,
                Name = ea.Name ?? string.Empty,
                Status = ea.Status ?? string.Empty,
                CreatedAt = ea.CreatedAt,
                UpdatedAt = ea.UpdatedAt
            }).ToList();

            return Ok(new { data = emailAccountDtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered email accounts");
            return StatusCode(500, new { message = "Error getting filtered email accounts" });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EmailAccountSummaryDto>>> GetEmailAccounts(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,           // single column to sort by
        [FromQuery] string? sortDirection = null,    // asc or desc
        [FromQuery] string? sortMode = null,         // count or percentage mode for stats columns
        [FromQuery] string? emailIds = null,
        [FromQuery] int? campaignId = null,          // filter by campaign ID
        [FromQuery] int? timeRangeDays = null,
        [FromQuery] string? filterByClientIds = null, // filter by multiple client IDs (comma-separated)
        [FromQuery] string? filterByUserIds = null,   // filter by multiple user IDs (comma-separated)
        [FromQuery] int? minSent = null,             // filter by minimum sent emails
        [FromQuery] string? warmupStatus = null,     // filter by warmup status
        [FromQuery] int? performanceFilterMinSent = null,     // worst performing filter minimum sent emails
        [FromQuery] double? performanceFilterMaxReplyRate = null) // worst performing filter maximum reply rate (%)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Handle specific client filtering if provided
            List<string>? effectiveClientIds = assignedClientIds;
            if (!string.IsNullOrEmpty(filterByClientIds))
            {
                var requestedClientIds = filterByClientIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();
                
                if (requestedClientIds.Any())
                {
                    if (assignedClientIds == null)
                    {
                        // Admin user - use the requested client IDs directly
                        effectiveClientIds = requestedClientIds;
                    }
                    else
                    {
                        // Regular user - intersect with assigned client IDs
                        effectiveClientIds = assignedClientIds.Intersect(requestedClientIds).ToList();
                    }
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
                    var userClientIds = new List<string>();
                    
                    foreach (var userId in requestedUserIds)
                    {
                        var user = await _authService.GetUserByIdAsync(userId);
                        if (user != null && user.AssignedClientIds != null)
                        {
                            userClientIds.AddRange(user.AssignedClientIds);
                        }
                    }

                    // Remove duplicates
                    userClientIds = userClientIds.Distinct().ToList();

                    if (effectiveClientIds == null)
                    {
                        // Admin user - use the user's client IDs directly
                        effectiveClientIds = userClientIds;
                    }
                    else
                    {
                        // Regular user - intersect with their assigned client IDs
                        effectiveClientIds = effectiveClientIds.Intersect(userClientIds).ToList();
                    }
                }
            }
            
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 500) pageSize = 500; // Max limit to prevent performance issues

            // Check if user has no assigned clients
            if (effectiveClientIds != null && !effectiveClientIds.Any())
            {
                // User has no assigned clients, return empty result
                return Ok(new PaginatedResponse<EmailAccountSummaryDto>
                {
                    Data = new List<EmailAccountSummaryDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = 0,
                    HasPrevious = false,
                    HasNext = false
                });
            }

            // Parse email IDs filter (for campaign filtering)
            List<long>? emailIdsList = null;
            
            // If campaignId is provided, fetch the campaign's email IDs
            if (campaignId.HasValue)
            {
                var campaigns = await _campaignRepository.GetAllAsync();
                var campaign = campaigns.FirstOrDefault(c => c.CampaignId == campaignId.Value);
                
                if (campaign != null && campaign.EmailIds != null && campaign.EmailIds.Any())
                {
                    emailIdsList = campaign.EmailIds.ToList();
                }
                else
                {
                    // Campaign not found or has no email accounts
                    emailIdsList = new List<long> { -1 }; // Use impossible ID to return empty results
                }
            }
            else if (!string.IsNullOrWhiteSpace(emailIds))
            {
                // Use the direct email IDs if provided
                emailIdsList = emailIds.Split(',')
                    .Where(id => long.TryParse(id.Trim(), out _))
                    .Select(id => long.Parse(id.Trim()))
                    .ToList();
                    
                if (!emailIdsList.Any()) emailIdsList = null;
            }

            // Check if we're sorting by stats columns 
            var sortDescending = !string.IsNullOrWhiteSpace(sortDirection) && sortDirection.ToLower() == "desc";
            bool isStatsSorting = !string.IsNullOrEmpty(sortBy) && (
                sortBy.Equals("sent", StringComparison.OrdinalIgnoreCase) ||
                sortBy.Equals("opened", StringComparison.OrdinalIgnoreCase) ||
                sortBy.Equals("replied", StringComparison.OrdinalIgnoreCase) ||
                sortBy.Equals("bounced", StringComparison.OrdinalIgnoreCase));
            
            // Check if we're sorting by positive replies (requires special handling)
            bool isPositiveRepliesSorting = !string.IsNullOrEmpty(sortBy) && 
                sortBy.Equals("positiveReplies", StringComparison.OrdinalIgnoreCase);

            // Determine if we need custom time-range calculation (not using database all-time stats)
            bool needsTimeRangeCalculation = isStatsSorting && timeRangeDays.HasValue && timeRangeDays.Value < 9999;
            
            List<EmailAccountSummaryDto> dtoList;
            int totalCount;
            int totalPages;

            if (isPositiveRepliesSorting)
            {
                // For positive replies sorting, we need to fetch all records, add positive replies data, then sort and paginate
                var (allAccounts, allTotalCount) = await _emailAccountRepository.GetPaginatedAsync(
                    1, // Get all records
                    int.MaxValue, // Large page size to get all
                    search, 
                    effectiveClientIds,
                    emailIdsList, 
                    null, // Don't sort in database for positive replies
                    false,
                    timeRangeDays,
                    sortMode,
                    minSent,
                    warmupStatus,
                    performanceFilterMinSent,
                    performanceFilterMaxReplyRate);

                var allAccountsList = allAccounts.ToList();
                totalCount = allTotalCount;
                
                // Get positive replies counts for ALL email accounts
                var allEmailAccountIds = allAccountsList.Select(a => (int)a.Id).ToList();
                var positiveRepliesCounts = await _classifiedEmailRepository.GetPositiveRepliesCountForEmailAccountsAsync(allEmailAccountIds);

                // Convert all database models to DTOs with positive replies count
                var allDtoList = allAccountsList.Select(account =>
                {
                    var dto = ConvertToDto(account);
                    dto.PositiveReplies = positiveRepliesCounts.TryGetValue((int)account.Id, out var count) ? count : 0;
                    return dto;
                }).ToList();

                // Sort by positive replies
                var isPercentageMode = sortMode?.ToLower() == "percentage";
                IOrderedEnumerable<EmailAccountSummaryDto> sortedDtos;
                
                if (isPercentageMode)
                {
                    // Sort by positive replies percentage (positive replies / total replied)
                    sortedDtos = sortDescending 
                        ? allDtoList.OrderByDescending(dto => dto.Replied > 0 ? (double)dto.PositiveReplies / dto.Replied : 0)
                        : allDtoList.OrderBy(dto => dto.Replied > 0 ? (double)dto.PositiveReplies / dto.Replied : 0);
                }
                else
                {
                    // Sort by positive replies count
                    sortedDtos = sortDescending 
                        ? allDtoList.OrderByDescending(dto => dto.PositiveReplies)
                        : allDtoList.OrderBy(dto => dto.PositiveReplies);
                }

                // Apply pagination to the sorted DTOs
                dtoList = sortedDtos
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                // Standard database-level sorting and pagination
                var (accounts, accountsTotalCount) = await _emailAccountRepository.GetPaginatedAsync(
                    page, 
                    pageSize, 
                    search, 
                    effectiveClientIds,
                    emailIdsList, 
                    sortBy, 
                    sortDescending,
                    timeRangeDays,
                    sortMode,
                    minSent,
                    warmupStatus,
                    performanceFilterMinSent,
                    performanceFilterMaxReplyRate);

                var accountsList = accounts.ToList();
                totalCount = accountsTotalCount;
                
                // Get positive replies counts for paginated email accounts
                var emailAccountIds = accountsList.Select(a => (int)a.Id).ToList();
                var positiveRepliesCounts = await _classifiedEmailRepository.GetPositiveRepliesCountForEmailAccountsAsync(emailAccountIds);

                // Convert database models to DTOs with positive replies count
                dtoList = accountsList.Select(account =>
                {
                    var dto = ConvertToDto(account);
                    dto.PositiveReplies = positiveRepliesCounts.TryGetValue((int)account.Id, out var count) ? count : 0;
                    return dto;
                }).ToList();
            }

            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            PaginatedResponse<EmailAccountSummaryDto> response = new()
            {
                Data = dtoList,
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
            _logger.LogError(ex, "Error fetching email accounts");
            return StatusCode(500, new { message = "Error fetching email accounts" });
        }
    }

    [HttpGet("by-id/{id:long}")]
    public async Task<ActionResult<EmailAccountSummaryDto>> GetEmailAccountById(long id)
    {
        try
        {
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);
            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            // Get positive replies count for this email account
            var positiveRepliesCount = await _classifiedEmailRepository.GetPositiveRepliesCountByEmailAccountAsync((int)emailAccount.Id);

            // Convert to DTO
            var dto = ConvertToDto(emailAccount);
            dto.PositiveReplies = positiveRepliesCount;

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching email account with ID: {Id}", id);
            return StatusCode(500, new { message = "Error fetching email account" });
        }
    }

    [HttpGet("{email}")]
    public async Task<ActionResult<EmailAccountSummaryDto>> GetEmailAccount(string email)
    {
        try
        {
            var emailAccount = await _emailAccountRepository.GetByEmailAsync(email);
            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            // Get positive replies count for this email account
            var positiveRepliesCount = await _classifiedEmailRepository.GetPositiveRepliesCountByEmailAccountAsync((int)emailAccount.Id);

            // Convert to DTO
            var dto = ConvertToDto(emailAccount);
            dto.PositiveReplies = positiveRepliesCount;

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching email account with email: {Email}", email);
            return StatusCode(500, new { message = "Error fetching email account" });
        }
    }

    // [HttpPost("refresh")]
    // public async Task<ActionResult> RefreshEmailAccountData()
    // {
    //     try
    //     {
    //         _logger.LogInformation("Starting email account data refresh");
    //         _mapperService.Fetch();
    //         _logger.LogInformation("Email account data refresh completed");
    //         return Ok(new { message = "Email account data refreshed successfully" });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error refreshing email account data");
    //         return StatusCode(500, new { message = "Error refreshing email account data" });
    //     }
    // }
    //

    [HttpPost("assign-client")]
    public async Task<ActionResult> AssignClientToEmailAccounts([FromBody] AssignClientRequest? request)
    {
        try
        {
            // Only admins can assign/unassign clients
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            if (request?.EmailAccountIds == null || !request.EmailAccountIds.Any())
            {
                return BadRequest(new
                {
                    message = "No email accounts specified"
                });
            }

            // Get the client details first
            Client? client = null;
            if (!string.IsNullOrEmpty(request.ClientId))
            {
                client = await _clientRepository.GetByIdAsync(request.ClientId);
                if (client == null)
                {
                    return BadRequest(new { message = "Invalid client ID" });
                }
            }

            int updatedCount = 0;
            foreach (var emailAccountId in request.EmailAccountIds)
            {
                if (!long.TryParse(emailAccountId, out long emailAccountIdLong))
                    continue;

                var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountIdLong);
                if (emailAccount != null)
                {
                    emailAccount.ClientId = client?.Id;
                    emailAccount.ClientName = client?.Name;
                    emailAccount.ClientColor = client?.Color;
                    await _emailAccountRepository.UpdateAsync(emailAccount);
                    updatedCount++;
                }
            }

            _logger.LogInformation("Assigned client {ClientName} to {Count} email accounts",
                client?.Name ?? "None", updatedCount);

            return Ok(new
            {
                message = $"Successfully assigned client to {updatedCount} email account(s)",
                updatedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning client to email accounts");
            return StatusCode(500, new { message = "Error assigning client to email accounts" });
        }
    }

    [HttpGet("{id}/warmup-metrics")]
    public async Task<ActionResult<WarmupMetricsDto>> GetWarmupMetrics(string id)
    {
        try
        {
            if (!long.TryParse(id, out long idLong))
                return NotFound(new { message = "Email account not found" });

            // Get the email account from PostgreSQL
            var emailAccount = await _emailAccountRepository.GetByIdAsync(idLong);
            if (emailAccount == null)
                return NotFound(new { message = "Email account not found" });

            // Get actual daily stats from PostgreSQL for the last 30 days
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-30);
            
            var statEntries = await _dailyStatsService.GetStatEntriesAsync(idLong, startDate, endDate);
            
            // Convert to dictionary for quick lookups
            var statsDict = statEntries.ToDictionary(e => e.StatDate.ToString("yyyy-MM-dd"), e => e);
            
            // Generate all dates in range, including dates with no data
            var dailyStats = new List<WarmupDailyData>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dateKey = date.ToString("yyyy-MM-dd");
                var stat = statsDict.GetValueOrDefault(dateKey);
                
                dailyStats.Add(new WarmupDailyData
                {
                    Date = dateKey,
                    Sent = stat?.Sent ?? 0,
                    Replied = stat?.Replied ?? 0,
                    SavedFromSpam = 0 // No specific saved from spam daily data yet
                });
            }

            // Return real warmup data from PostgreSQL
            var warmupData = new WarmupMetricsDto
            {
                Id = idLong,
                Email = emailAccount.Email,
                TotalSent = emailAccount.WarmupSent,
                TotalReplied = emailAccount.WarmupReplied,
                TotalSavedFromSpam = emailAccount.WarmupSavedFromSpam,
                LastUpdatedAt = emailAccount.WarmupUpdateDateTime ?? emailAccount.UpdatedAt,
                DailyStats = dailyStats
            };

            return Ok(warmupData);

            // // If no warmup data exists, generate sample data based on the email account's main metrics
            // _logger.LogInformation("No warmup data found, generating sample data for ID: {Id}", id);
            // // emailAccount is already retrieved above
            // if (emailAccount == null)
            // {
            //     return NotFound(new { message = "Email account not found" });
            // }
            //
            // var warmupMetrics = GenerateSampleWarmupData(emailAccount);
            //
            // // Save the generated data to database for consistency
            // _dbService.Insert(warmupMetrics);
            //
            // return Ok(warmupMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching warmup metrics for ID: {Id}", id);
            return StatusCode(500, new { message = "Error fetching warmup metrics" });
        }
    }

    [HttpGet("{id}/daily-stats")]
    public async Task<ActionResult> GetEmailAccountDailyStats(string id, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (!long.TryParse(id, out long idLong))
                return BadRequest(new { message = "Invalid email account ID" });

            // Find the email account first to get the admin UUID
            var emailAccount = await _emailAccountRepository.GetByIdAsync(idLong);
            if (emailAccount == null)
                return NotFound(new { message = "Email account not found" });

            // Set default date range if not provided (last 30 days)
            var endDateValue = endDate ?? DateTime.UtcNow.Date;
            var startDateValue = startDate ?? endDateValue.AddDays(-30);

            // Get daily stats using optimized single query with date range
            var statEntries = await _dailyStatsService.GetStatEntriesAsync(idLong, startDateValue, endDateValue);
            
            // Convert to dictionary format for daily breakdown response
            var sentStats = statEntries.ToDictionary(e => e.StatDate.ToString("yyyy-MM-dd"), e => e.Sent);
            var openedStats = statEntries.ToDictionary(e => e.StatDate.ToString("yyyy-MM-dd"), e => e.Opened);
            var repliedStats = statEntries.ToDictionary(e => e.StatDate.ToString("yyyy-MM-dd"), e => e.Replied);
            var bouncedStats = statEntries.ToDictionary(e => e.StatDate.ToString("yyyy-MM-dd"), e => e.Bounced);

            // Generate all dates in the range, including dates with no data
            var allDates = new List<string>();
            for (var date = startDateValue; date <= endDateValue; date = date.AddDays(1))
            {
                allDates.Add(date.ToString("yyyy-MM-dd"));
            }

            var dailyStats = allDates.Select(date => new
            {
                date,
                sent = sentStats.GetValueOrDefault(date, 0),
                opened = openedStats.GetValueOrDefault(date, 0),
                replied = repliedStats.GetValueOrDefault(date, 0),
                bounced = bouncedStats.GetValueOrDefault(date, 0)
            }).ToList();

            return Ok(new
            {
                emailAccountId = idLong,
                startDate = startDateValue,
                endDate = endDateValue,
                dailyStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily stats for email account ID: {Id}", id);
            return StatusCode(500, new { message = "Error fetching daily stats" });
        }
    }

    [HttpPost("{id}/warmup-metrics/refresh")]
    public ActionResult RefreshWarmupMetrics(string id)
    {
        try
        {
            return Ok(new { message = "Warmup metrics refresh not implemented yet" });

            // _logger.LogInformation("Refreshing warmup metrics for ID: {Id}", id);
            //
            // // In a real implementation, this would fetch fresh data from the warmup service
            // // For now, we'll regenerate the sample data
            // var emailAccount = _dbService.FindById<EmailAccountSummaryDto>(id);
            // if (emailAccount == null)
            // {
            //     return NotFound(new { message = "Email account not found" });
            // }
            //
            // var existingWarmupData = _dbService.FindOne<WarmupMetricsDto>(w => w.Email == emailAccount.Email);
            // var warmupMetrics = GenerateSampleWarmupData(emailAccount);
            //
            // if (existingWarmupData != null)
            // {
            //     // Update existing record
            //     warmupMetrics.Id = existingWarmupData.Id;
            //     _dbService.Update(warmupMetrics);
            // }
            // else
            // {
            //     // Insert new record
            //     _dbService.Insert(warmupMetrics);
            // }
            //
            // _logger.LogInformation("Warmup metrics refreshed for ID: {Id}", id);
            // return Ok(new { message = "Warmup metrics refreshed successfully", data = warmupMetrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing warmup metrics for ID: {Id}", id);
            return StatusCode(500, new { message = "Error refreshing warmup metrics" });
        }
    }

    [HttpGet("by-client/{clientId}")]
    public async Task<ActionResult<PaginatedResponse<EmailAccountSummaryDto>>> GetEmailAccountsByClient(
        string clientId,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sortColumn = "email",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? sortMode = "count")
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500; // Max limit to prevent performance issues

            var allEmailAccounts = (await _emailAccountRepository.GetAllAsync())
                .Where(e => e.ClientId == clientId)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                allEmailAccounts = allEmailAccounts.Where(e => 
                    (e.Email != null && e.Email.ToLower().Contains(searchLower)) ||
                    (e.Name != null && e.Name.ToLower().Contains(searchLower)) ||
                    (e.Status != null && e.Status.ToLower().Contains(searchLower)) ||
                    e.Id.ToString().Contains(searchLower) ||
                    (e.Tags != null && e.Tags.Count > 0 && e.Tags.Any(tag => tag != null && tag.ToLower().Contains(searchLower))));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                var isDescending = sortDirection?.ToLower() == "desc";
                var isPercentageMode = sortMode?.ToLower() == "percentage";
                
                allEmailAccounts = sortColumn.ToLower() switch
                {
                    "email" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Email) : allEmailAccounts.OrderBy(e => e.Email),
                    "name" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Name) : allEmailAccounts.OrderBy(e => e.Name),
                    "status" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Status) : allEmailAccounts.OrderBy(e => e.Status),
                    "sent" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent) : allEmailAccounts.OrderBy(e => e.Sent),
                    "opened" => isPercentageMode 
                        ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Opened / e.Sent : 0) : allEmailAccounts.OrderBy(e => e.Sent > 0 ? (double)e.Opened / e.Sent : 0))
                        : (isDescending ? allEmailAccounts.OrderByDescending(e => e.Opened) : allEmailAccounts.OrderBy(e => e.Opened)),
                    "replied" => isPercentageMode
                        ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Replied / e.Sent : 0) : allEmailAccounts.OrderBy(e => e.Sent > 0 ? (double)e.Replied / e.Sent : 0))
                        : (isDescending ? allEmailAccounts.OrderByDescending(e => e.Replied) : allEmailAccounts.OrderBy(e => e.Replied)),
                    "bounced" => isPercentageMode
                        ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent > 0 ? (double)e.Bounced / e.Sent : 0) : allEmailAccounts.OrderBy(e => e.Sent > 0 ? (double)e.Bounced / e.Sent : 0))
                        : (isDescending ? allEmailAccounts.OrderByDescending(e => e.Bounced) : allEmailAccounts.OrderBy(e => e.Bounced)),
                    "warmupsent" => isDescending ? allEmailAccounts.OrderByDescending(e => e.WarmupSent) : allEmailAccounts.OrderBy(e => e.WarmupSent),
                    "warmupreplied" => isPercentageMode
                        ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.WarmupSent > 0 ? (double)e.WarmupReplied / e.WarmupSent : 0) : allEmailAccounts.OrderBy(e => e.WarmupSent > 0 ? (double)e.WarmupReplied / e.WarmupSent : 0))
                        : (isDescending ? allEmailAccounts.OrderByDescending(e => e.WarmupReplied) : allEmailAccounts.OrderBy(e => e.WarmupReplied)),
                    "warmupsavedfromspam" => isDescending ? allEmailAccounts.OrderByDescending(e => e.WarmupSavedFromSpam) : allEmailAccounts.OrderBy(e => e.WarmupSavedFromSpam),
                    "warmupspamcount" => isPercentageMode
                        ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.WarmupSent > 0 ? (double)e.WarmupSpamCount / e.WarmupSent : 0) : allEmailAccounts.OrderBy(e => e.WarmupSent > 0 ? (double)e.WarmupSpamCount / e.WarmupSent : 0))
                        : (isDescending ? allEmailAccounts.OrderByDescending(e => e.WarmupSpamCount) : allEmailAccounts.OrderBy(e => e.WarmupSpamCount)),
                    "clientname" => isDescending ? allEmailAccounts.OrderByDescending(e => e.ClientName ?? "") : allEmailAccounts.OrderBy(e => e.ClientName ?? ""),
                    "campaigncount" => isDescending ? allEmailAccounts.OrderByDescending(e => e.CampaignCount) : allEmailAccounts.OrderBy(e => e.CampaignCount),
                    "activecampaigns" => isDescending ? allEmailAccounts.OrderByDescending(e => e.ActiveCampaignCount) : allEmailAccounts.OrderBy(e => e.ActiveCampaignCount),
                    "activecampaigncount" => isDescending ? allEmailAccounts.OrderByDescending(e => e.ActiveCampaignCount) : allEmailAccounts.OrderBy(e => e.ActiveCampaignCount),
                    "issendingactualemails" => isDescending ? allEmailAccounts.OrderByDescending(e => e.IsSendingActualEmails) : allEmailAccounts.OrderBy(e => e.IsSendingActualEmails),
                    "sendingactualemails" => isDescending ? allEmailAccounts.OrderByDescending(e => e.IsSendingActualEmails) : allEmailAccounts.OrderBy(e => e.IsSendingActualEmails),
                    // "sent7d" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent7d) : allEmailAccounts.OrderBy(e => e.Sent7d),
                    // "sent24h" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent24h) : allEmailAccounts.OrderBy(e => e.Sent24h),
                    // "replied7d" => isPercentageMode
                    //     ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent7d > 0 ? (double)e.Replied7d / e.Sent7d : 0) : allEmailAccounts.OrderBy(e => e.Sent7d > 0 ? (double)e.Replied7d / e.Sent7d : 0))
                    //     : (isDescending ? allEmailAccounts.OrderByDescending(e => e.Replied7d) : allEmailAccounts.OrderBy(e => e.Replied7d)),
                    // "replied24h" => isPercentageMode
                    //     ? (isDescending ? allEmailAccounts.OrderByDescending(e => e.Sent24h > 0 ? (double)e.Replied24h / e.Sent24h : 0) : allEmailAccounts.OrderBy(e => e.Sent24h > 0 ? (double)e.Replied24h / e.Sent24h : 0))
                    //     : (isDescending ? allEmailAccounts.OrderByDescending(e => e.Replied24h) : allEmailAccounts.OrderBy(e => e.Replied24h)),
                    "createdat" => isDescending ? allEmailAccounts.OrderByDescending(e => e.CreatedAt) : allEmailAccounts.OrderBy(e => e.CreatedAt),
                    "updatedat" => isDescending ? allEmailAccounts.OrderByDescending(e => e.UpdatedAt) : allEmailAccounts.OrderBy(e => e.UpdatedAt),
                    "lastupdatedat" => isDescending ? allEmailAccounts.OrderByDescending(e => e.LastUpdatedAt ?? e.UpdatedAt) : allEmailAccounts.OrderBy(e => e.LastUpdatedAt ?? e.UpdatedAt),
                    "tags" => isDescending 
                        ? allEmailAccounts.OrderByDescending(e => e.Tags != null && e.Tags.Any() ? string.Join(", ", e.Tags.OrderBy(t => t)) : "")
                        : allEmailAccounts.OrderBy(e => e.Tags != null && e.Tags.Any() ? string.Join(", ", e.Tags.OrderBy(t => t)) : ""),
                    "tagcount" => isDescending 
                        ? allEmailAccounts.OrderByDescending(e => e.Tags != null ? e.Tags.Count : 0)
                        : allEmailAccounts.OrderBy(e => e.Tags != null ? e.Tags.Count : 0),
                    "notes" => isDescending ? allEmailAccounts.OrderByDescending(e => e.Notes ?? "") : allEmailAccounts.OrderBy(e => e.Notes ?? ""),
                    "positivereplies" => allEmailAccounts.OrderBy(e => e.Email), // Positive replies sorting handled after DTO conversion
                    _ => allEmailAccounts.OrderBy(e => e.Email)
                };
            }
            else
            {
                allEmailAccounts = allEmailAccounts.OrderBy(e => e.Email);
            }

            List<EmailAccountDbModel> accountsList = allEmailAccounts.ToList();
            
            var totalCount = accountsList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var paginatedAccounts = accountsList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get positive replies counts for paginated accounts
            var emailAccountIds = paginatedAccounts.Select(a => (int)a.Id).ToList();
            var positiveRepliesCounts = await _classifiedEmailRepository.GetPositiveRepliesCountForEmailAccountsAsync(emailAccountIds);

            // Convert to DTOs with positive replies
            var dtoList = paginatedAccounts.Select(account =>
            {
                var dto = ConvertToDto(account);
                dto.PositiveReplies = positiveRepliesCounts.TryGetValue((int)account.Id, out var count) ? count : 0;
                return dto;
            }).ToList();

            // Handle positive replies sorting at DTO level if needed
            if (!string.IsNullOrWhiteSpace(sortColumn) && sortColumn.ToLower() == "positivereplies")
            {
                var isDescending = sortDirection?.ToLower() == "desc";
                var isPercentageMode = sortMode?.ToLower() == "percentage";
                
                if (isPercentageMode)
                {
                    // Sort by positive replies percentage (positive replies / total replied)
                    dtoList = isDescending 
                        ? dtoList.OrderByDescending(dto => dto.Replied > 0 ? (double)dto.PositiveReplies / dto.Replied : 0).ToList()
                        : dtoList.OrderBy(dto => dto.Replied > 0 ? (double)dto.PositiveReplies / dto.Replied : 0).ToList();
                }
                else
                {
                    // Sort by positive replies count
                    dtoList = isDescending 
                        ? dtoList.OrderByDescending(dto => dto.PositiveReplies).ToList()
                        : dtoList.OrderBy(dto => dto.PositiveReplies).ToList();
                }
            }
           
            PaginatedResponse<EmailAccountSummaryDto> response = new()
            {
                Data = dtoList,
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
            _logger.LogError(ex, "Error fetching email accounts for client {ClientId}", clientId);
            return StatusCode(500, new { message = "Error fetching email accounts for client" });
        }
    }

    [HttpGet("download")]
    public async Task<ActionResult> DownloadEmailAccounts([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string? clientIds = null)
    {
        try
        {
            _logger.LogInformation("Download email accounts CSV requested for date range: {StartDate} to {EndDate} with client filter: {ClientIds}", startDate, endDate, clientIds);
            
            // Parse date parameters
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;
            bool isAllTimeData = string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate);
            
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
            
            // Get all email accounts
            var accounts = (await _emailAccountRepository.GetAllAsync()).AsQueryable();
            
            // Apply date range filter if dates are provided (not all-time data)
            if (!isAllTimeData && startDateTime.HasValue && endDateTime.HasValue)
            {
                accounts = accounts.Where(a => a.CreatedAt >= startDateTime.Value && a.CreatedAt <= endDateTime.Value);
            }
            
            // Filter by client IDs if provided
            if (!string.IsNullOrEmpty(clientIds))
            {
                var selectedClientIds = clientIds.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList();
                accounts = accounts.Where(a => !string.IsNullOrEmpty(a.ClientId) && selectedClientIds.Contains(a.ClientId));
            }
            
            var accountsList = accounts.ToList();
            var clients = (await _clientRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c);
            
            // Create CSV export models
            List<EmailAccountCsvModel> csvData = accountsList.Select(account => new EmailAccountCsvModel
            {
                Id = account.Id,
                Email = account.Email ?? "",
                Name = account.Name ?? "",
                Status = account.Status ?? "",
                ClientName = !string.IsNullOrEmpty(account.ClientId) && clients.ContainsKey(account.ClientId) 
                    ? clients[account.ClientId].Name ?? "Unassigned"
                    : "Unassigned",
                TotalSent = account.Sent,
                TotalOpened = account.Opened,
                TotalReplied = account.Replied,
                TotalBounced = account.Bounced,
                WarmupSent = account.WarmupSent,
                WarmupReplied = account.WarmupReplied,
                WarmupSavedFromSpam = account.WarmupSavedFromSpam,
                // Sent24h = account.Sent24h,
                // Replied24h = account.Replied24h,
                // Sent7d = account.Sent7d,
                // Replied7d = account.Replied7d,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
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
                ? "email_accounts_all_time.csv"
                : $"email_accounts_{startDate}_to_{endDate}.csv";
            
            _logger.LogInformation("Generated CSV with {Count} email accounts (All time: {IsAllTime})", csvData.Count, isAllTimeData);
            
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading email accounts CSV");
            return StatusCode(500, new { message = "Error downloading email accounts data" });
        }
    }
    
    public class EmailAccountCsvModel
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public int TotalSent { get; set; }
        public int TotalOpened { get; set; }
        public int TotalClicked { get; set; }
        public int TotalReplied { get; set; }
        public int TotalUnsubscribed { get; set; }
        public int TotalBounced { get; set; }
        public int WarmupSent { get; set; }
        public int WarmupReplied { get; set; }
        public int WarmupSavedFromSpam { get; set; }
        // public int Sent24h { get; set; }
        // public int Replied24h { get; set; }
        // public int Sent7d { get; set; }
        // public int Replied7d { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    [HttpPost("{id}/tags")]
    public async Task<ActionResult> AssignTagsToEmailAccount(string id, [FromBody] List<string> tags)
    {
        try
        {
            // Only admins can assign tags to email accounts
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            // Parse the string ID to long for database lookup
            if (!long.TryParse(id, out long emailAccountId))
            {
                return BadRequest(new { message = "Invalid email account ID format" });
            }

            var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountId);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            // Simply store the tag names as strings
            emailAccount.Tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
            emailAccount.UpdatedAt = DateTime.UtcNow;
            await _emailAccountRepository.UpdateAsync(emailAccount);

            _logger.LogInformation("Assigned {TagCount} tags to email account {EmailAccountId}", emailAccount.Tags.Count, emailAccountId);
            return Ok(new { message = $"Successfully assigned {emailAccount.Tags.Count} tags to email account", tags = emailAccount.Tags });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning tags to email account {EmailAccountId}", id);
            return StatusCode(500, new { message = "Error assigning tags to email account" });
        }
    }

    [HttpDelete("{id}/tags/{tagName}")]
    public async Task<ActionResult> RemoveTagFromEmailAccount(string id, string tagName)
    {
        try
        {
            // Only admins can remove tags from email accounts
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            // Parse the string ID to long for database lookup
            if (!long.TryParse(id, out long emailAccountId))
            {
                return BadRequest(new { message = "Invalid email account ID format" });
            }

            var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountId);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            if (emailAccount.Tags.Contains(tagName))
            {
                emailAccount.Tags.Remove(tagName);
                emailAccount.UpdatedAt = DateTime.UtcNow;
                await _emailAccountRepository.UpdateAsync(emailAccount);
                
                _logger.LogInformation("Removed tag {TagName} from email account {EmailAccountId}", tagName, emailAccountId);
                return Ok(new { message = "Tag removed from email account successfully" });
            }

            return NotFound(new { message = "Tag not found on this email account" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag from email account {EmailAccountId}", id);
            return StatusCode(500, new { message = "Error removing tag from email account" });
        }
    }

    [HttpPut("{id}/notes")]
    public async Task<ActionResult> UpdateEmailAccountNotes(string id, [FromBody] UpdateNotesRequest request)
    {
        try
        {
            // Only admins can update email account notes
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            // Parse the string ID to long for database lookup
            if (!long.TryParse(id, out long emailAccountId))
            {
                return BadRequest(new { message = "Invalid email account ID format" });
            }

            var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountId);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            emailAccount.Notes = request.Notes;
            emailAccount.UpdatedAt = DateTime.UtcNow;
            await _emailAccountRepository.UpdateAsync(emailAccount);
            
            _logger.LogInformation("Updated notes for email account {EmailAccountId}", emailAccountId);
            return Ok(new { message = "Notes updated successfully", notes = emailAccount.Notes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notes for email account {EmailAccountId}", id);
            return StatusCode(500, new { message = "Error updating email account notes" });
        }
    }

    [HttpGet("{id}/campaigns")]
    public async Task<ActionResult<List<object>>> GetEmailAccountCampaigns(string id)
    {
        try
        {
            // Parse the string ID to long for database lookup
            if (!long.TryParse(id, out long emailAccountId))
            {
                return BadRequest(new { message = "Invalid email account ID format" });
            }

            // Check if email account exists and user has access
            var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountId);
            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            // Get user's assigned client IDs (for non-admin users)
            var userAssignedClientIds = await GetUserAssignedClientIds();
            if (userAssignedClientIds != null && !userAssignedClientIds.Contains(emailAccount.ClientId))
            {
                return Forbid("You don't have access to this email account");
            }

            // Get all campaigns
            var campaigns = await _campaignRepository.GetAllAsync();
            
            // Filter campaigns that contain this email account
            var emailAccountCampaigns = campaigns
                .Where(c => c.EmailIds.Contains((int)emailAccountId))
                .Select(c => new
                {
                    id = c.Id,
                    campaignId = c.CampaignId,
                    name = c.Name,
                    status = c.Status,
                    clientName = c.ClientName,
                    clientColor = c.ClientColor,
                    totalLeads = c.TotalLeads,
                    totalSent = c.TotalSent,
                    totalReplied = c.TotalReplied,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                })
                .OrderByDescending(c => c.updatedAt)
                .ToList<object>();

            _logger.LogInformation("Retrieved {Count} campaigns for email account {EmailAccountId}", emailAccountCampaigns.Count, emailAccountId);
            
            return Ok(emailAccountCampaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns for email account {EmailAccountId}", id);
            return StatusCode(500, new { message = "Error retrieving campaigns for email account" });
        }
    }

}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignClientRequest
{
    public string? ClientId { get; set; }
    public List<string> EmailAccountIds { get; set; } = new();
}

public class UpdateNotesRequest
{
    public string? Notes { get; set; }
}