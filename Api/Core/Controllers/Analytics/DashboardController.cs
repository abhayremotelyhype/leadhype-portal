using LeadHype.Api.Core.Models.Frontend;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeadHype.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get comprehensive dashboard overview with all key metrics
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DashboardFilterRequest? filter = null, [FromQuery] bool allCampaigns = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Validate admin access for allCampaigns parameter
                if (allCampaigns && !IsAdminUser())
                {
                    return Forbid("Only administrators can access all campaigns system-wide");
                }
                
                // If allCampaigns is true, clear any client/campaign filters to get system-wide data
                if (allCampaigns && filter != null)
                {
                    filter.ClientIds = new List<string>();
                    filter.CampaignIds = new List<string>();
                }
                
                var overview = await _dashboardService.GetDashboardOverviewAsync(
                    allCampaigns ? null : userId, // Pass null for userId to bypass user filtering for admins
                    filter);
                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load dashboard overview", error = ex.Message });
            }
        }

        /// <summary>
        /// Get filtered dashboard overview with POST request for complex filter data
        /// </summary>
        [HttpPost("filtered-overview")]
        public async Task<IActionResult> GetFilteredOverview([FromBody] DashboardFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Validate admin access for allCampaigns parameter
                if (filter.AllCampaigns && !IsAdminUser())
                {
                    return Forbid("Only administrators can access all campaigns system-wide");
                }
                
                // If AllCampaigns is true, clear any client/campaign filters to get system-wide data
                if (filter.AllCampaigns)
                {
                    filter.ClientIds = new List<string>();
                    filter.CampaignIds = new List<string>();
                }
                
                var overview = await _dashboardService.GetDashboardOverviewAsync(
                    filter.AllCampaigns ? null : userId, // Pass null for userId to bypass user filtering for admins
                    filter);
                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load filtered dashboard overview", error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance trend data for charts
        /// </summary>
        [HttpGet("performance-trend")]
        public async Task<IActionResult> GetPerformanceTrend([FromQuery] DashboardFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var trendData = await _dashboardService.GetPerformanceTrendAsync(filter, userId);
                return Ok(trendData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load performance trend", error = ex.Message });
            }
        }

        /// <summary>
        /// Get filtered performance trend data with POST request for complex filter data
        /// </summary>
        [HttpPost("filtered-performance-trend")]
        public async Task<IActionResult> GetFilteredPerformanceTrend([FromBody] DashboardFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var trendData = await _dashboardService.GetPerformanceTrendAsync(filter, userId);
                return Ok(trendData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load filtered performance trend", error = ex.Message });
            }
        }

        /// <summary>
        /// Get campaign performance trend data for charts
        /// </summary>
        [HttpGet("campaign-performance-trend")]
        public async Task<IActionResult> GetCampaignPerformanceTrend([FromQuery] DashboardFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var trendData = await _dashboardService.GetCampaignPerformanceTrendAsync(filter, userId);
                return Ok(trendData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load campaign performance trend", error = ex.Message });
            }
        }

        /// <summary>
        /// Get filtered campaign performance trend data with POST request for complex filter data
        /// </summary>
        [HttpPost("filtered-campaign-performance-trend")]
        public async Task<IActionResult> GetFilteredCampaignPerformanceTrend([FromBody] DashboardFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var trendData = await _dashboardService.GetCampaignPerformanceTrendAsync(filter, userId);
                return Ok(trendData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load filtered campaign performance trend", error = ex.Message });
            }
        }

        /// <summary>
        /// Get top performing campaigns
        /// </summary>
        [HttpGet("top-campaigns")]
        public async Task<IActionResult> GetTopCampaigns([FromQuery] int limit = 10, [FromQuery] int? timeRangeDays = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var campaigns = await _dashboardService.GetTopCampaignsAsync(limit, userId, timeRangeDays);
                return Ok(campaigns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load top campaigns", error = ex.Message });
            }
        }

        /// <summary>
        /// Get filtered top performing campaigns with advanced filtering and sorting options
        /// </summary>
        [HttpPost("filtered-top-campaigns")]
        public async Task<IActionResult> GetFilteredTopCampaigns([FromBody] CampaignPerformanceFilter filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _dashboardService.GetFilteredTopCampaignsAsync(filter, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load filtered campaigns", error = ex.Message });
            }
        }

        /// <summary>
        /// Get filtered top performing campaigns with query parameters
        /// </summary>
        [HttpGet("filtered-top-campaigns")]
        public async Task<IActionResult> GetFilteredTopCampaignsQuery(
            [FromQuery] int minimumSent = 100,
            [FromQuery] string sortBy = "ReplyRate",
            [FromQuery] bool sortDescending = true,
            [FromQuery] int limit = 10,
            [FromQuery] string? period = null,
            [FromQuery] bool useCompositeScore = false,
            [FromQuery] double? minimumReplyRate = null,
            [FromQuery] double? maxBounceRate = null,
            [FromQuery] string? statuses = null,
            [FromQuery] int? timeRangeDays = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var filter = new CampaignPerformanceFilter
                {
                    MinimumSent = minimumSent,
                    SortBy = sortBy,
                    SortDescending = sortDescending,
                    Limit = limit,
                    Period = period,
                    TimeRangeDays = timeRangeDays,
                    UseCompositeScore = useCompositeScore,
                    MinimumReplyRate = minimumReplyRate,
                    MaximumBounceRate = maxBounceRate,
                    Statuses = !string.IsNullOrEmpty(statuses) 
                        ? statuses.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string>()
                };
                
                var result = await _dashboardService.GetFilteredTopCampaignsAsync(filter, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load filtered campaigns", error = ex.Message });
            }
        }

        /// <summary>
        /// Get top performing clients
        /// </summary>
        [HttpGet("top-clients")]
        public async Task<IActionResult> GetTopClients([FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var clients = await _dashboardService.GetTopClientsAsync(limit, userId);
                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load top clients", error = ex.Message });
            }
        }

        /// <summary>
        /// Get email account summary and health metrics
        /// </summary>
        [HttpGet("email-accounts-summary")]
        public async Task<IActionResult> GetEmailAccountsSummary()
        {
            try
            {
                var userId = GetCurrentUserId();
                var summary = await _dashboardService.GetEmailAccountSummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load email accounts summary", error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent activities across all entities
        /// </summary>
        [HttpGet("recent-activities")]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                var activities = await _dashboardService.GetRecentActivitiesAsync(limit, userId);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load recent activities", error = ex.Message });
            }
        }

        /// <summary>
        /// Get dashboard stats with custom date range
        /// </summary>
        [HttpPost("stats")]
        public async Task<IActionResult> GetStatsWithFilter([FromBody] DashboardFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var overview = await _dashboardService.GetDashboardOverviewAsync(userId, filter);
                
                // Return just the stats portion for filtered requests
                return Ok(new
                {
                    stats = overview.Stats,
                    performanceTrend = overview.PerformanceTrend,
                    emailAccountSummary = overview.EmailAccountSummary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load filtered stats", error = ex.Message });
            }
        }

        /// <summary>
        /// Get real-time dashboard metrics (for auto-refresh)
        /// </summary>
        [HttpGet("realtime")]
        public async Task<IActionResult> GetRealtimeMetrics()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get just the essential real-time metrics
                var emailAccountTask = _dashboardService.GetEmailAccountSummaryAsync(userId);
                var recentActivitiesTask = _dashboardService.GetRecentActivitiesAsync(5, userId);

                await Task.WhenAll(emailAccountTask, recentActivitiesTask);

                return Ok(new
                {
                    emailAccountSummary = await emailAccountTask,
                    recentActivities = await recentActivitiesTask,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load realtime metrics", error = ex.Message });
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private bool IsAdminUser()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value == "Admin";
        }
    }
}