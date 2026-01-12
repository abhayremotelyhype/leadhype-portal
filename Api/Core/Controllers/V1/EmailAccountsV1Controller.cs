using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Api;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeadHype.Api.Services;
using System.Security.Claims;

namespace LeadHype.Api.Controllers.V1;

/// <summary>
/// Email Accounts management API for external users
/// </summary>
[ApiController]
[Route("api/v1/email-accounts")]
[Authorize(AuthenticationSchemes = "ApiKey")]
[Produces("application/json")]
[Tags("Email Accounts")]
public class EmailAccountsV1Controller : ControllerBase
{
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IEmailAccountDailyStatEntryRepository _emailAccountDailyStatsRepository;
    private readonly IEmailAccountDailyStatEntryService _emailAccountDailyStatsService;
    private readonly ILogger<EmailAccountsV1Controller> _logger;
    private readonly IAuthService _authService;

    public EmailAccountsV1Controller(
        IEmailAccountRepository emailAccountRepository,
        ICampaignRepository campaignRepository,
        IEmailAccountDailyStatEntryRepository emailAccountDailyStatsRepository,
        IEmailAccountDailyStatEntryService emailAccountDailyStatsService,
        ILogger<EmailAccountsV1Controller> logger,
        IAuthService authService)
    {
        _emailAccountRepository = emailAccountRepository;
        _campaignRepository = campaignRepository;
        _emailAccountDailyStatsRepository = emailAccountDailyStatsRepository;
        _emailAccountDailyStatsService = emailAccountDailyStatsService;
        _logger = logger;
        _authService = authService;
    }

    private async Task<List<string>?> GetUserAssignedClientIds()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        var user = await _authService.GetUserByIdAsync(userIdClaim);
        return user?.AssignedClientIds;
    }

    /// <summary>
    /// Get paginated list of email accounts
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <returns>Paginated list of email accounts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<EmailAccountDbModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<EmailAccountDbModel>>> GetEmailAccounts(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Get user role for admin check
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Get all email accounts
            var allEmailAccounts = await _emailAccountRepository.GetAllAsync();

            // Apply client filtering (skip filtering for Admin users)
            if (!isAdmin && assignedClientIds != null)
            {
                var filteredAccounts = new List<EmailAccountDbModel>();
                foreach (var account in allEmailAccounts)
                {
                    if (assignedClientIds.Contains(account.AdminUuid))
                    {
                        filteredAccounts.Add(account);
                    }
                }
                allEmailAccounts = filteredAccounts;
            }

            var totalCount = allEmailAccounts.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / (double)pageSize);

            // Apply pagination
            var skip = (page - 1) * pageSize;
            var pagedAccounts = allEmailAccounts.Skip(skip).Take(pageSize).ToList();

            return Ok(new PaginatedResponse<EmailAccountDbModel>
            {
                Data = pagedAccounts,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email accounts");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific email account
    /// </summary>
    /// <param name="id">Email account ID</param>
    /// <returns>Email account details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmailAccountDbModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmailAccountDbModel>> GetEmailAccount(long id)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            // Check if user has access to this email account (skip check for Admin users)
            if (!isAdmin && assignedClientIds != null && !assignedClientIds.Contains(emailAccount.AdminUuid))
            {
                return Forbid("Access denied to this email account");
            }

            return Ok(emailAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get summary of all email accounts - Provides overview with total count and status breakdown
    /// </summary>
    /// <returns>Summary with totals and status breakdown</returns>
    /// <response code="200">Email accounts summary retrieved successfully</response>
    /// <response code="401">Authentication failed</response>
    /// <response code="403">Access denied</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "totalAccounts": 25,
    ///   "accountsByStatus": {
    ///     "Active": 18,
    ///     "Paused": 5,
    ///     "Blocked": 2
    ///   }
    /// }
    /// </example>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(EmailAccountSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetEmailAccountsSummary()
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            var allEmailAccounts = (await _emailAccountRepository.GetAllAsync()).ToList();

            // Apply client filtering (skip filtering for Admin users)
            if (!isAdmin && assignedClientIds != null)
            {
                var filteredAccounts = new List<EmailAccountDbModel>();
                foreach (var account in allEmailAccounts)
                {
                    if (assignedClientIds.Contains(account.AdminUuid))
                    {
                        filteredAccounts.Add(account);
                    }
                }
                allEmailAccounts = filteredAccounts;
            }

            var accountsByStatus = new Dictionary<string, int>();
            foreach (var account in allEmailAccounts)
            {
                if (accountsByStatus.ContainsKey(account.Status))
                {
                    accountsByStatus[account.Status]++;
                }
                else
                {
                    accountsByStatus[account.Status] = 1;
                }
            }

            return Ok(new 
            {
                totalAccounts = allEmailAccounts.Count(),
                accountsByStatus = accountsByStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email accounts summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get email account statistics with day-by-day breakdown - Retrieves comprehensive email metrics with daily performance data
    /// </summary>
    /// <param name="id">Email account ID</param>
    /// <param name="startDate">Start date for statistics (YYYY-MM-DD format, defaults to 30 days ago)</param>
    /// <param name="endDate">End date for statistics (YYYY-MM-DD format, defaults to today)</param>
    /// <returns>Email account statistics with comprehensive metrics and daily breakdown</returns>
    /// <response code="200">Email account statistics retrieved successfully</response>
    /// <response code="400">Invalid date range or parameters</response>
    /// <response code="401">Authentication failed</response>
    /// <response code="403">Access denied</response>
    /// <response code="404">Email account not found</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "emailAccount": {
    ///       "id": 1001,
    ///       "email": "sender@company.com",
    ///       "name": "Jane Sender",
    ///       "status": "Active"
    ///     },
    ///     "timeRange": {
    ///       "startDate": "2024-01-01",
    ///       "endDate": "2024-01-31",
    ///       "days": 31
    ///     },
    ///     "summary": {
    ///       "totalSent": 2500,
    ///       "totalOpened": 950,
    ///       "totalReplied": 120,
    ///       "totalBounced": 35,
    ///       "openRate": 38.00,
    ///       "replyRate": 4.80,
    ///       "bounceRate": 1.40
    ///     },
    ///     "dailyStats": [
    ///       {
    ///         "date": "2024-01-01",
    ///         "dayOfWeek": "Monday",
    ///         "sent": 85,
    ///         "opened": 32,
    ///         "replied": 4,
    ///         "bounced": 1,
    ///         "openRate": 37.65,
    ///         "replyRate": 4.71,
    ///         "bounceRate": 1.18
    ///       }
    ///     ]
    ///   },
    ///   "message": "Email account statistics retrieved successfully",
    ///   "errorCode": null
    /// }
    /// </example>
    [HttpGet("{id}/stats")]
    [ProducesResponseType(typeof(EmailAccountStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetEmailAccountStats(long id, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);

            if (emailAccount == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Email account not found", "NOT_FOUND"));
            }

            if (!isAdmin && assignedClientIds != null && !assignedClientIds.Contains(emailAccount.AdminUuid))
            {
                return Forbid();
            }

            // Parse and validate date parameters
            DateTime effectiveStartDate;
            DateTime effectiveEndDate;

            try 
            {
                effectiveStartDate = string.IsNullOrEmpty(startDate) 
                    ? DateTime.UtcNow.AddDays(-30).Date
                    : DateTime.ParseExact(startDate, "yyyy-MM-dd", null);
            }
            catch (FormatException)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Invalid startDate format '{startDate}'. Please use YYYY-MM-DD format (e.g., 2025-08-24)", 
                    "INVALID_START_DATE_FORMAT"));
            }

            try 
            {
                effectiveEndDate = string.IsNullOrEmpty(endDate) 
                    ? DateTime.UtcNow.Date
                    : DateTime.ParseExact(endDate, "yyyy-MM-dd", null);
            }
            catch (FormatException)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Invalid endDate format '{endDate}'. Please use YYYY-MM-DD format (e.g., 2025-08-24)", 
                    "INVALID_END_DATE_FORMAT"));
            }

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

            // Get daily stats for the email account using the service (same as dashboard)
            var dailyStats = await _emailAccountDailyStatsService.GetStatEntriesAsync(
                emailAccount.Id, effectiveStartDate, effectiveEndDate);

            var dailyStatsOrdered = dailyStats.OrderBy(s => s.StatDate).ToList();

            // Calculate totals
            var totalStats = new
            {
                TotalSent = dailyStatsOrdered.Sum(s => s.Sent),
                TotalOpened = dailyStatsOrdered.Sum(s => s.Opened),
                TotalReplied = dailyStatsOrdered.Sum(s => s.Replied),
                TotalBounced = dailyStatsOrdered.Sum(s => s.Bounced)
            };

            // Calculate rates
            var openRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalOpened / totalStats.TotalSent * 100), 2) : 0;
            var replyRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalReplied / totalStats.TotalSent * 100), 2) : 0;
            var bounceRate = totalStats.TotalSent > 0 ? Math.Round((decimal)((double)totalStats.TotalBounced / totalStats.TotalSent * 100), 2) : 0;

            // Prepare daily breakdown
            var dailyBreakdown = dailyStatsOrdered.Select(stat => new
            {
                Date = stat.StatDate.ToString("yyyy-MM-dd"),
                DayOfWeek = stat.StatDate.ToString("dddd"),
                Sent = stat.Sent,
                Opened = stat.Opened,
                Replied = stat.Replied,
                Bounced = stat.Bounced,
                OpenRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Opened / stat.Sent * 100), 2) : 0,
                ReplyRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Replied / stat.Sent * 100), 2) : 0,
                BounceRate = stat.Sent > 0 ? Math.Round((decimal)((double)stat.Bounced / stat.Sent * 100), 2) : 0
            }).ToList();

            var stats = new
            {
                EmailAccount = new
                {
                    emailAccount.Id,
                    emailAccount.Email,
                    emailAccount.Name,
                    emailAccount.Status
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
                    totalStats.TotalReplied,
                    totalStats.TotalBounced,
                    OpenRate = openRate,
                    ReplyRate = replyRate,
                    BounceRate = bounceRate
                },
                DailyStats = dailyBreakdown
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account stats for {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Get email account warmup status and statistics
    /// </summary>
    /// <param name="id">Email account ID</param>
    /// <returns>Warmup statistics and status</returns>
    [HttpGet("{id}/warmup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetEmailAccountWarmup(long id)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            if (!isAdmin && assignedClientIds != null && !assignedClientIds.Contains(emailAccount.AdminUuid))
            {
                return Forbid("Access denied to this email account");
            }

            // Determine warmup status based on actual data
            var isWarmupActive = emailAccount.Status?.ToLower().Contains("warm") == true || 
                                emailAccount.WarmupUpdateDateTime.HasValue && 
                                emailAccount.WarmupUpdateDateTime > DateTime.UtcNow.AddDays(-7);
            
            var warmupPhase = emailAccount.Status?.ToLower() switch
            {
                var s when s.Contains("warming") => "warming_up",
                var s when s.Contains("warmed") => "warmed_up",
                "active" => emailAccount.WarmupSent > 0 ? "warmed_up" : "not_started",
                "inactive" => "paused",
                _ => "not_configured"
            };

            // Calculate warmup progress (rough estimate based on typical warmup period)
            var warmupProgress = emailAccount.WarmupSent switch
            {
                var sent when sent >= 200 => 100,
                var sent when sent >= 100 => 75,
                var sent when sent >= 50 => 50,
                var sent when sent >= 20 => 25,
                var sent when sent > 0 => 10,
                _ => 0
            };

            // Estimate daily limit based on warmup phase
            var estimatedDailyLimit = warmupPhase switch
            {
                "warmed_up" => 50,
                "warming_up" => Math.Min(20, Math.Max(5, emailAccount.WarmupSent / 10)),
                _ => 0
            };

            // Calculate reputation based on reply rate
            var reputationScore = emailAccount.WarmupSent > 0 
                ? Math.Round((double)emailAccount.WarmupReplied / emailAccount.WarmupSent * 100, 1)
                : 0.0;
            
            var reputation = reputationScore switch
            {
                >= 40 => "excellent",
                >= 25 => "good", 
                >= 15 => "fair",
                >= 5 => "poor",
                _ => "unknown"
            };

            var warmupInfo = new
            {
                emailAccount = new
                {
                    id = emailAccount.Id,
                    email = emailAccount.Email,
                    name = emailAccount.Name,
                    status = emailAccount.Status
                },
                warmupStatus = new
                {
                    isActive = isWarmupActive,
                    progress = warmupProgress,
                    phase = warmupPhase,
                    dailyLimit = estimatedDailyLimit,
                    currentSent = emailAccount.WarmupSent,
                    reputation = reputation
                },
                warmupMetrics = new
                {
                    totalSent = emailAccount.WarmupSent,
                    totalReplied = emailAccount.WarmupReplied,
                    totalSavedFromSpam = emailAccount.WarmupSavedFromSpam,
                    replyRate = emailAccount.WarmupSent > 0 
                        ? Math.Round((double)emailAccount.WarmupReplied / emailAccount.WarmupSent * 100, 2)
                        : 0.0,
                    lastUpdated = emailAccount.WarmupUpdateDateTime?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? null
                }
            };

            return Ok(warmupInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account warmup for {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get email account warmup statistics with day-by-day breakdown
    /// </summary>
    /// <param name="id">Email account ID</param>
    /// <param name="startDate">Start date for warmup statistics (YYYY-MM-DD format)</param>
    /// <param name="endDate">End date for warmup statistics (YYYY-MM-DD format)</param>
    /// <returns>Warmup statistics with daily breakdown</returns>
    [HttpGet("{id}/warmup/daily-stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetEmailAccountWarmupDailyStats(long id, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);

            if (emailAccount == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Email account not found", "NOT_FOUND"));
            }

            if (!isAdmin && assignedClientIds != null && !assignedClientIds.Contains(emailAccount.AdminUuid))
            {
                return Forbid();
            }

            // Parse and validate date parameters
            DateTime effectiveStartDate;
            DateTime effectiveEndDate;

            try 
            {
                effectiveStartDate = string.IsNullOrEmpty(startDate) 
                    ? DateTime.UtcNow.AddDays(-30).Date
                    : DateTime.ParseExact(startDate, "yyyy-MM-dd", null);
            }
            catch (FormatException)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Invalid startDate format '{startDate}'. Please use YYYY-MM-DD format (e.g., 2025-08-24)", 
                    "INVALID_START_DATE_FORMAT"));
            }

            try 
            {
                effectiveEndDate = string.IsNullOrEmpty(endDate) 
                    ? DateTime.UtcNow.Date
                    : DateTime.ParseExact(endDate, "yyyy-MM-dd", null);
            }
            catch (FormatException)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Invalid endDate format '{endDate}'. Please use YYYY-MM-DD format (e.g., 2025-08-24)", 
                    "INVALID_END_DATE_FORMAT"));
            }

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

            // Get actual daily stats from the service (same source as regular daily stats but interpreted as warmup)
            // Since we don't have separate warmup daily tables yet, we use the email daily stats
            // In the future, this could be enhanced to use actual warmup-specific daily data
            var dailyStats = await _emailAccountDailyStatsService.GetStatEntriesAsync(
                emailAccount.Id, effectiveStartDate, effectiveEndDate);

            var dailyStatsOrdered = dailyStats.OrderBy(s => s.StatDate).ToList();

            // For warmup stats, we interpret the daily stats differently:
            // - 'Sent' represents warmup emails sent
            // - 'Replied' represents warmup replies received  
            // - We don't have separate spam data in daily stats yet
            var dailyWarmupBreakdown = new List<WarmupDailyDataDto>();
            
            for (var date = effectiveStartDate; date <= effectiveEndDate; date = date.AddDays(1))
            {
                var dayStats = dailyStatsOrdered.FirstOrDefault(s => s.StatDate.Date == date.Date);
                
                dailyWarmupBreakdown.Add(new WarmupDailyDataDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Sent = dayStats?.Sent ?? 0,
                    Replied = dayStats?.Replied ?? 0,
                    SavedFromSpam = 0 // Not available in current daily stats structure
                });
            }

            // Calculate totals from daily data
            var totalSent = dailyWarmupBreakdown.Sum(d => d.Sent);
            var totalReplied = dailyWarmupBreakdown.Sum(d => d.Replied);
            var totalSavedFromSpam = dailyWarmupBreakdown.Sum(d => d.SavedFromSpam);

            var warmupStats = new
            {
                EmailAccount = new
                {
                    emailAccount.Id,
                    emailAccount.Email,
                    emailAccount.Name,
                    emailAccount.Status
                },
                TimeRange = new
                {
                    StartDate = effectiveStartDate.ToString("yyyy-MM-dd"),
                    EndDate = effectiveEndDate.ToString("yyyy-MM-dd"),
                    Days = (effectiveEndDate - effectiveStartDate).Days + 1
                },
                Summary = new
                {
                    TotalSent = totalSent,
                    TotalReplied = totalReplied,
                    TotalSavedFromSpam = totalSavedFromSpam,
                    ReplyRate = totalSent > 0 ? Math.Round((double)totalReplied / totalSent * 100, 2) : 0.0,
                    SpamProtectionRate = totalSent > 0 ? Math.Round((double)totalSavedFromSpam / totalSent * 100, 2) : 0.0
                },
                DailyStats = dailyWarmupBreakdown
            };

            return Ok(ApiResponse<object>.SuccessResponse(warmupStats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account warmup daily stats for {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Get email account health metrics and reputation status
    /// </summary>
    /// <param name="id">Email account ID</param>
    /// <param name="days">Number of days to analyze (default: 7)</param>
    /// <returns>Health metrics and reputation data</returns>
    [HttpGet("{id}/health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetEmailAccountHealth(long id, [FromQuery] int days = 7)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            if (!isAdmin && assignedClientIds != null && !assignedClientIds.Contains(emailAccount.AdminUuid))
            {
                return Forbid("Access denied to this email account");
            }

            var health = new
            {
                emailAccount = new
                {
                    id = emailAccount.Id,
                    email = emailAccount.Email,
                    name = emailAccount.Name,
                    status = emailAccount.Status
                },
                healthStatus = new
                {
                    status = "unknown",
                    score = 0,
                    color = "gray"
                },
                timeRange = new
                {
                    days = days,
                    startDate = DateTime.UtcNow.AddDays(-days).Date,
                    endDate = DateTime.UtcNow.Date
                },
                metrics = new
                {
                    bounceRate = 0.0,
                    spamRate = 0.0,
                    deliveryRate = 0.0,
                    reputation = "unknown"
                },
                recommendations = new[]
                {
                    "Health monitoring will be available when statistics service is configured",
                    "Connect Smartlead API for comprehensive health analysis"
                },
                message = "Health data will be available when stats and monitoring services are configured"
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account health for {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get campaigns that use a specific email account
    /// </summary>
    /// <param name="id">Email account ID</param>
    /// <returns>List of campaigns using this email account</returns>
    [HttpGet("{id}/campaigns")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetEmailAccountCampaigns(long id)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";
            
            // First check if the email account exists and user has access
            var emailAccount = await _emailAccountRepository.GetByIdAsync(id);

            if (emailAccount == null)
            {
                return NotFound(new { message = "Email account not found" });
            }

            // Check if user has access to this email account (skip check for Admin users)
            if (!isAdmin && assignedClientIds != null && !assignedClientIds.Contains(emailAccount.AdminUuid))
            {
                return Forbid("Access denied to this email account");
            }

            // Get all campaigns
            var allCampaigns = await _campaignRepository.GetAllAsync();
            
            // Filter campaigns that contain this email account ID
            var campaignsWithEmailAccount = allCampaigns
                .Where(c => c.EmailIds != null && c.EmailIds.Contains(id))
                .ToList();

            // Apply client filtering for non-admin users
            if (!isAdmin && assignedClientIds != null)
            {
                campaignsWithEmailAccount = campaignsWithEmailAccount
                    .Where(c => c.ClientId != null && assignedClientIds.Contains(c.ClientId))
                    .ToList();
            }

            // Format the response
            var response = campaignsWithEmailAccount.Select(c => new
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
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt
            }).ToList();

            _logger.LogInformation($"Retrieved {response.Count} campaigns for email account {id}");

            return Ok(ApiResponse<object>.SuccessResponse(new 
            {
                emailAccount = new
                {
                    id = emailAccount.Id,
                    email = emailAccount.Email,
                    name = emailAccount.Name,
                    status = emailAccount.Status
                },
                campaigns = response,
                totalCampaigns = response.Count
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaigns for email account {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
        }
    }
}