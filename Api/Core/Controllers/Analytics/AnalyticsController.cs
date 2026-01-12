using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Core.Models.Frontend;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Services;
using LeadHype.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeadHype.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly ICampaignDailyStatEntryRepository _campaignDailyStatsRepository;
    private readonly ICampaignEventRepository _campaignEventRepository;
    private readonly IEmailAccountDailyStatEntryRepository _emailAccountDailyStatsRepository;
    private readonly ILogger<AnalyticsController> _logger;
    private readonly IAuthService _authService;

    public AnalyticsController(
        ICampaignRepository campaignRepository,
        IClientRepository clientRepository,
        IEmailAccountRepository emailAccountRepository,
        ICampaignDailyStatEntryRepository campaignDailyStatsRepository,
        ICampaignEventRepository campaignEventRepository,
        IEmailAccountDailyStatEntryRepository emailAccountDailyStatsRepository,
        ILogger<AnalyticsController> logger,
        IAuthService authService)
    {
        _campaignRepository = campaignRepository;
        _clientRepository = clientRepository;
        _emailAccountRepository = emailAccountRepository;
        _campaignDailyStatsRepository = campaignDailyStatsRepository;
        _campaignEventRepository = campaignEventRepository;
        _emailAccountDailyStatsRepository = emailAccountDailyStatsRepository;
        _logger = logger;
        _authService = authService;
    }

    private async Task<List<string>?> GetUserAssignedClientIds()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (userRole == UserRoles.Admin)
        {
            return null; // Admin sees all data
        }
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return new List<string>();
        
        var user = await _authService.GetUserByIdAsync(userId);
        return user?.AssignedClientIds ?? new List<string>();
    }
    
    // Helper method to calculate recent stats (last 7 days) efficiently
    private static int GetRecentStats(List<CampaignDailyStatEntry> allTimeStats, Func<CampaignDailyStatEntry, int> selector)
    {
        var recentDate = DateTime.UtcNow.Date.AddDays(-7);
        return allTimeStats
            .Where(s => s.StatDate >= recentDate)
            .Sum(selector);
    }

    [HttpGet("dashboard")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<ActionResult> GetAnalyticsDashboard([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string period = "30d")
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse date range
            DateTime? start = null;
            DateTime? end = null;
            
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startParsed))
                start = startParsed.Date;
            
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endParsed))
                end = endParsed.Date.AddDays(1).AddTicks(-1); // End of day
            
            // Get campaigns with role-based filtering (optimized - only get what we need)
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var campaigns = allCampaigns.ToList();
            
            if (assignedClientIds != null && assignedClientIds.Any()) // Regular user filtering
            {
                campaigns = campaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                campaigns = new List<CampaignDetailsDbModel>();
            }
            
            // Get all clients for user (for analytics breakdown)
            var allClients = await _clientRepository.GetAllAsync();
            var clients = allClients.ToList();
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                clients = clients.Where(c => assignedClientIds.Contains(c.Id ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                clients = new List<Client>();
            }
            
            // Get email accounts with role-based filtering
            var allEmailAccounts = await _emailAccountRepository.GetAllAsync();
            var emailAccounts = allEmailAccounts.ToList();
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                emailAccounts = emailAccounts.Where(ea => assignedClientIds.Contains(ea.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                emailAccounts = new List<EmailAccountDbModel>();
            }
            
            // Calculate time ranges for comparison
            var now = DateTime.UtcNow.Date;
            var currentPeriodStart = start ?? now.AddDays(-30);
            var currentPeriodEnd = end ?? now;
            var periodDays = (currentPeriodEnd - currentPeriodStart).Days;
            var previousPeriodStart = currentPeriodStart.AddDays(-periodDays);
            var previousPeriodEnd = currentPeriodStart.AddDays(-1);
            
            // Get all-time campaign stats using the event-sourced system
            var campaignIds = campaigns.Select(c => c.Id).ToList();
            var campaignIdStrings = campaigns.Select(c => c.CampaignId.ToString()).ToList();

            // Debug logging
            _logger.LogInformation($"[DEBUG] Processing {campaigns.Count} campaigns for main dashboard");

            // Use campaign events repository instead of empty daily stats table
            var (currentSent, currentReplied, currentPositiveReplies, lastReplyDate, lastPositiveReplyDate) =
                await _campaignEventRepository.GetAggregatedTotalsForCampaignsAsync(campaignIdStrings);

            // Get all-time stats from campaign events (opened, bounced, clicked)
            var allCampaignStats = campaignIdStrings.Any()
                ? await _campaignEventRepository.GetStatsForCampaignsAsync(
                    campaignIdStrings, DateTime.MinValue, DateTime.MaxValue)
                : Enumerable.Empty<CampaignStatsNew>();

            var currentOpened = allCampaignStats.Sum(s => s.Opened);
            var currentBounced = allCampaignStats.Sum(s => s.Bounced);
            var currentClicked = allCampaignStats.Sum(s => s.Clicked);

            // Debug logging for the calculated stats
            _logger.LogInformation($"[DEBUG] Main Dashboard Stats - Sent: {currentSent}, Opened: {currentOpened}, Replied: {currentReplied}, Positive: {currentPositiveReplies}");
            
            // Calculate metrics
            var totalCampaigns = campaigns.Count;
            var activeCampaigns = campaigns.Count(c => c.Status?.ToLower() == "active");
            var pausedCampaigns = campaigns.Count(c => c.Status?.ToLower() == "pause");
            var completedCampaigns = campaigns.Count(c => c.Status?.ToLower() == "completed");
            
            var totalEmailAccounts = emailAccounts.Count;
            var activeClients = clients.Count;
            
            // Calculate rates
            var replyRate = currentSent > 0 ? (double)currentReplied / currentSent * 100 : 0;
            var openRate = currentSent > 0 ? (double)currentOpened / currentSent * 100 : 0;
            var bounceRate = currentSent > 0 ? (double)currentBounced / currentSent * 100 : 0;
            var clickRate = currentSent > 0 ? (double)currentClicked / currentSent * 100 : 0;
            var positiveReplyRate = currentReplied > 0 ? (double)currentPositiveReplies / currentReplied * 100 : 0;
            
            // Calculate week-over-week changes
            var campaignGrowth = 0.0;
            var emailAccountGrowth = 0.0;
            var clientGrowth = 0.0;
            var replyRateChange = 0.0;
            var positiveReplyRateChange = 0.0;
            var openRateChange = 0.0;
            var bounceRateChange = 0.0;
            var totalEmailsSentChange = 0.0;
            
            // Calculate current week vs previous week comparison
            var currentWeekStart = DateTime.UtcNow.Date.AddDays(-7);
            var currentWeekEnd = DateTime.UtcNow.Date;
            var previousWeekStart = currentWeekStart.AddDays(-7);
            var previousWeekEnd = currentWeekStart;
            
            if (campaignIds.Any())
            {
                var currentWeekStats = await _campaignDailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
                    campaignIds, currentWeekStart, currentWeekEnd);
                var previousWeekStats = await _campaignDailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
                    campaignIds, previousWeekStart, previousWeekEnd);
                
                var currentWeekTotals = currentWeekStats.GroupBy(x => 1).Select(g => new {
                    Sent = g.Sum(s => s.Sent),
                    Opened = g.Sum(s => s.Opened),
                    Replied = g.Sum(s => s.Replied),
                    PositiveReplies = g.Sum(s => s.PositiveReplies)
                }).FirstOrDefault() ?? new { Sent = 0, Opened = 0, Replied = 0, PositiveReplies = 0 };
                
                var previousWeekTotals = previousWeekStats.GroupBy(x => 1).Select(g => new {
                    Sent = g.Sum(s => s.Sent),
                    Opened = g.Sum(s => s.Opened), 
                    Replied = g.Sum(s => s.Replied),
                    PositiveReplies = g.Sum(s => s.PositiveReplies)
                }).FirstOrDefault() ?? new { Sent = 0, Opened = 0, Replied = 0, PositiveReplies = 0 };
                
                // Calculate percentage changes
                if (previousWeekTotals.Sent > 0)
                    totalEmailsSentChange = ((double)(currentWeekTotals.Sent - previousWeekTotals.Sent) / previousWeekTotals.Sent) * 100;
                
                // Calculate rate changes
                var currentOpenRate = currentWeekTotals.Sent > 0 ? (double)currentWeekTotals.Opened / currentWeekTotals.Sent : 0;
                var previousOpenRate = previousWeekTotals.Sent > 0 ? (double)previousWeekTotals.Opened / previousWeekTotals.Sent : 0;
                if (previousOpenRate > 0)
                    openRateChange = ((currentOpenRate - previousOpenRate) / previousOpenRate) * 100;
                
                var currentReplyRate = currentWeekTotals.Sent > 0 ? (double)currentWeekTotals.Replied / currentWeekTotals.Sent : 0;
                var previousReplyRate = previousWeekTotals.Sent > 0 ? (double)previousWeekTotals.Replied / previousWeekTotals.Sent : 0;
                if (previousReplyRate > 0)
                    replyRateChange = ((currentReplyRate - previousReplyRate) / previousReplyRate) * 100;
                
                var currentPositiveReplyRate = currentWeekTotals.Replied > 0 ? (double)currentWeekTotals.PositiveReplies / currentWeekTotals.Replied : 0;
                var previousPositiveReplyRate = previousWeekTotals.Replied > 0 ? (double)previousWeekTotals.PositiveReplies / previousWeekTotals.Replied : 0;
                if (previousPositiveReplyRate > 0)
                    positiveReplyRateChange = ((currentPositiveReplyRate - previousPositiveReplyRate) / previousPositiveReplyRate) * 100;
            }
            
            // Parse period for trends
            var days = period switch
            {
                "1d" => 1,
                "3d" => 3,
                "7d" => 7,
                "14d" => 14,
                "30d" => 30,
                "60d" => 60,
                "90d" => 90,
                "180d" => 180,
                "365d" => 365,
                "all" => 1825, // 5 years for "all time"
                _ => 30
            };
            
            var trendEndDate = end ?? DateTime.UtcNow.Date;
            var trendStartDate = start ?? trendEndDate.AddDays(-days);
            
            // Get performance trends
            var trendStats = await _campaignDailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
                campaignIds, trendStartDate, trendEndDate);
            
            var performanceTrends = trendStats
                .GroupBy(s => s.StatDate)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    sent = g.Sum(s => s.Sent),
                    opened = g.Sum(s => s.Opened),
                    replied = g.Sum(s => s.Replied),
                    bounced = g.Sum(s => s.Bounced),
                    replyRate = g.Sum(s => s.Sent) > 0 ? Math.Round((double)g.Sum(s => s.Replied) / g.Sum(s => s.Sent) * 100, 2) : 0,
                    openRate = g.Sum(s => s.Sent) > 0 ? Math.Round((double)g.Sum(s => s.Opened) / g.Sum(s => s.Sent) * 100, 2) : 0,
                    bounceRate = g.Sum(s => s.Sent) > 0 ? Math.Round((double)g.Sum(s => s.Bounced) / g.Sum(s => s.Sent) * 100, 2) : 0
                })
                .Where(t => t.sent > 0 || t.opened > 0 || t.replied > 0) // Only include days with actual activity
                .OrderBy(t => t.date)
                .ToList();
            
            // Skip email account stats if there are too many accounts (performance optimization)
            List<EmailAccountDailyStatEntry> emailAccountStats = new List<EmailAccountDailyStatEntry>();
            if (emailAccounts.Count <= 100)
            {
                emailAccountStats = (await _emailAccountDailyStatsRepository.GetByEmailAccountIdsAndDateRangeAsync(
                    emailAccounts.Take(20).Select(ea => ea.Id).ToList(), trendStartDate, trendEndDate)).ToList();
            }
            
            var emailAccountPerformance = emailAccountStats
                .GroupBy(s => s.EmailAccountId)
                .Select(g =>
                {
                    var emailAccount = emailAccounts.FirstOrDefault(ea => ea.Id == g.Key);
                    var totalSent = g.Sum(s => s.Sent);
                    var totalReplied = g.Sum(s => s.Replied);
                    return new
                    {
                        emailAccountId = g.Key,
                        email = emailAccount?.Email ?? "Unknown",
                        name = emailAccount?.Name ?? "Unnamed",
                        sent = totalSent,
                        replied = totalReplied,
                        replyRate = totalSent > 0 ? Math.Round((double)totalReplied / totalSent * 100, 2) : 0
                    };
                })
                .Where(p => p.sent > 0)
                .OrderByDescending(p => p.sent)
                .Take(10)
                .ToList();
            
            // Get client comparison (limit to active clients)
            var clientComparison = new List<object>();
            foreach (var client in clients.Take(10))
            {
                var clientCampaigns = campaigns.Where(c => c.ClientId == client.Id).ToList();
                if (clientCampaigns.Any())
                {
                    var clientCampaignIdStrings = clientCampaigns.Select(c => c.CampaignId.ToString()).ToList();
                    var clientStats = allCampaignStats.Where(s => clientCampaignIdStrings.Contains(s.CampaignId)).ToList();

                    if (clientStats.Any())
                    {
                        var clientSent = clientStats.Sum(s => s.Sent);
                        var clientReplied = clientStats.Sum(s => s.Replied);

                        clientComparison.Add(new
                        {
                            clientId = client.Id,
                            clientName = client.Name,
                            clientColor = client.Color,
                            campaigns = clientCampaigns.Count,
                            activeCampaigns = clientCampaigns.Count(c => c.Status?.ToLower() == "active"),
                            sent = clientSent,
                            replyRate = clientSent > 0 ? Math.Round((double)clientReplied / clientSent * 100, 2) : 0
                        });
                    }
                }
            }
            
            return Ok(new
            {
                stats = new
                {
                    totalCampaigns,
                    activeCampaigns,
                    pausedCampaigns,
                    completedCampaigns,
                    totalEmailAccounts,
                    totalClients = activeClients,
                    totalUsers = 1, // This would need proper user count
                    totalEmailsSent = currentSent,
                    totalEmailsSentChange = Math.Round(totalEmailsSentChange, 1),
                    totalEmailsOpened = currentOpened,
                    totalEmailsReplied = currentReplied,
                    totalEmailsBounced = currentBounced,
                    totalEmailsClicked = currentClicked,
                    // Recent stats are not applicable with event-sourced data without dates
                    recentEmailsSent = 0,
                    recentEmailsOpened = 0,
                    recentEmailsReplied = 0,
                    totalPositiveReplies = currentPositiveReplies,
                    openRate = Math.Round(openRate, 1),
                    replyRate = Math.Round(replyRate, 1),
                    bounceRate = Math.Round(bounceRate, 1),
                    clickRate = Math.Round(clickRate, 1),
                    positiveReplyRate = Math.Round(positiveReplyRate, 1),
                    openRateChange = Math.Round(openRateChange, 1),
                    replyRateChange = Math.Round(replyRateChange, 1),
                    bounceRateChange = Math.Round(bounceRateChange, 1),
                    positiveReplyRateChange = Math.Round(positiveReplyRateChange, 1),
                    periodDays,
                    currentPeriod = new { start = currentPeriodStart, end = currentPeriodEnd },
                    previousPeriod = new { start = previousPeriodStart, end = previousPeriodEnd }
                },
                topCampaigns = new List<object>(), // Empty for now - would need implementation
                topClients = clientComparison.OrderByDescending(c => ((dynamic)c).sent).Take(5).ToList(),
                performanceTrend = performanceTrends,
                emailAccountSummary = new
                {
                    totalAccounts = totalEmailAccounts,
                    activeAccounts = emailAccounts.Count(ea => ea.Status?.ToLower() == "active"),
                    warmingUpAccounts = emailAccounts.Count(ea => ea.Status?.ToLower() == "warming"),
                    warmedUpAccounts = emailAccounts.Count(ea => ea.Status?.ToLower() == "warmed"),
                    pausedAccounts = emailAccounts.Count(ea => ea.Status?.ToLower() == "inactive" || ea.Status?.ToLower() == "paused"),
                    issueAccounts = emailAccounts.Count(ea => ea.Status?.ToLower() == "error" || ea.Status?.ToLower() == "blocked"),
                    accountsByProvider = emailAccounts
                        .Where(ea => !string.IsNullOrEmpty(ea.Email))
                        .GroupBy(ea => ea.Email.Split('@').LastOrDefault()?.Split('.').LastOrDefault()?.ToLower() ?? "unknown")
                        .ToDictionary(g => g.Key, g => g.Count()),
                    accountsByStatus = emailAccounts
                        .GroupBy(ea => ea.Status?.ToLower() ?? "unknown")
                        .ToDictionary(g => g.Key, g => new
                        {
                            count = g.Count(),
                            percentage = Math.Round((double)g.Count() / Math.Max(totalEmailAccounts, 1) * 100, 1)
                        })
                },
                recentActivities = new List<object>() // Empty for now - would need implementation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching analytics overview");
            return StatusCode(500, new { message = "Error fetching analytics overview" });
        }
    }

    [HttpPost("dashboard/filtered-overview")]
    public async Task<ActionResult> GetFilteredAnalyticsDashboard([FromBody] DashboardFilterRequest filterRequest)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Use all-time data since date filters were removed from frontend
            
            // Get campaigns with role-based filtering and request filters
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var campaigns = allCampaigns.ToList();
            
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                campaigns = campaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                campaigns = new List<CampaignDetailsDbModel>();
            }
            
            // Apply client filter from request if provided
            if (filterRequest.ClientIds != null && filterRequest.ClientIds.Any())
            {
                campaigns = campaigns.Where(c => filterRequest.ClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            
            // Apply campaign filter from request if provided
            if (filterRequest.CampaignIds != null && filterRequest.CampaignIds.Any())
            {
                campaigns = campaigns.Where(c => filterRequest.CampaignIds.Contains(c.Id)).ToList();
            }
            
            // Get all clients for user
            var allClients = await _clientRepository.GetAllAsync();
            var clients = allClients.ToList();
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                clients = clients.Where(c => assignedClientIds.Contains(c.Id ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                clients = new List<Client>();
            }
            
            // Apply client filter to clients list too
            if (filterRequest.ClientIds != null && filterRequest.ClientIds.Any())
            {
                clients = clients.Where(c => filterRequest.ClientIds.Contains(c.Id ?? "")).ToList();
            }
            
            // Get email accounts with role-based filtering
            var allEmailAccounts = await _emailAccountRepository.GetAllAsync();
            var emailAccounts = allEmailAccounts.ToList();
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                emailAccounts = emailAccounts.Where(ea => assignedClientIds.Contains(ea.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                emailAccounts = new List<EmailAccountDbModel>();
            }
            
            // Get all-time campaign stats using aggregated totals
            var campaignIds = campaigns.Select(c => c.Id).ToList();
            
            // Debug logging
            _logger.LogInformation($"[DEBUG] Processing {campaigns.Count} campaigns after filtering");
            _logger.LogInformation($"[DEBUG] Campaign IDs count: {campaignIds.Count}");
            
            var (totalEmailsSent, totalEmailsReplied, totalPositiveReplies, lastReplyDate, lastPositiveReplyDate) = 
                await _campaignDailyStatsRepository.GetAggregatedTotalsForCampaignsAsync(campaignIds);
            
            // For other stats, we need to get them by summing up individual campaign entries
            // This is less efficient but needed for opened, bounced, and clicked counts
            var allTimeStats = new List<CampaignDailyStatEntry>();
            foreach (var campaignId in campaignIds)
            {
                var campaignStats = await _campaignDailyStatsRepository.GetByCampaignIdAsync(campaignId);
                allTimeStats.AddRange(campaignStats);
            }
            
            var totalEmailsOpened = allTimeStats.Sum(s => s.Opened);
            var totalEmailsBounced = allTimeStats.Sum(s => s.Bounced);
            var totalEmailsClicked = allTimeStats.Sum(s => s.Clicked);
            
            // Debug logging for the calculated stats
            _logger.LogInformation($"[DEBUG] Stats - Sent: {totalEmailsSent}, Opened: {totalEmailsOpened}, Replied: {totalEmailsReplied}, Positive: {totalPositiveReplies}");
            
            // For changes, set to 0 since we're using all-time data
            var prevTotalEmailsSent = 0;
            var prevTotalEmailsOpened = 0;
            var prevTotalEmailsReplied = 0;
            var prevTotalPositiveReplies = 0;
            
            // Calculate rates and changes
            var openRate = totalEmailsSent > 0 ? Math.Round((double)totalEmailsOpened / totalEmailsSent * 100, 2) : 0;
            var replyRate = totalEmailsSent > 0 ? Math.Round((double)totalEmailsReplied / totalEmailsSent * 100, 2) : 0;
            var bounceRate = totalEmailsSent > 0 ? Math.Round((double)totalEmailsBounced / totalEmailsSent * 100, 2) : 0;
            var clickRate = totalEmailsSent > 0 ? Math.Round((double)totalEmailsClicked / totalEmailsSent * 100, 2) : 0;
            var positiveReplyRate = totalEmailsReplied > 0 ? Math.Round((double)totalPositiveReplies / totalEmailsReplied * 100, 2) : 0;
            
            // Set all rate changes to 0 since we're using all-time data (no previous period comparison)
            var openRateChange = 0.0;
            var replyRateChange = 0.0;
            var positiveReplyRateChange = 0.0;
            
            // Calculate recent activity (last 7 days) for "recent" stats display
            var recentStart = DateTime.UtcNow.Date.AddDays(-7);
            var recentEnd = DateTime.UtcNow.Date;
            var recentStats = await _campaignDailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
                campaignIds, recentStart, recentEnd);
            var recentEmailsSent = recentStats.Sum(s => s.Sent);
            var recentEmailsOpened = recentStats.Sum(s => s.Opened);
            var recentEmailsReplied = recentStats.Sum(s => s.Replied);
            
            // Campaign status counts (filter based on selected campaigns)
            var activeCampaigns = campaigns.Count(c => c.Status?.ToLower() == "active");
            var pausedCampaigns = campaigns.Count(c => c.Status?.ToLower() == "paused");
            var completedCampaigns = campaigns.Count(c => c.Status?.ToLower() == "completed");
            
            return Ok(new
            {
                stats = new
                {
                    totalEmailAccounts = emailAccounts.Count,
                    totalCampaigns = campaigns.Count,
                    totalClients = clients.Count,
                    totalUsers = 1, // This would need proper implementation based on user management
                    totalEmailsSent,
                    totalEmailsOpened,
                    totalEmailsReplied,
                    totalEmailsBounced,
                    totalEmailsClicked,
                    openRate,
                    replyRate,
                    bounceRate,
                    clickRate,
                    openRateChange,
                    replyRateChange,
                    bounceRateChange = 0.0, // Would need previous period bounce rate calculation
                    recentEmailsSent,
                    recentEmailsOpened,
                    recentEmailsReplied,
                    activeCampaigns,
                    pausedCampaigns,
                    completedCampaigns,
                    totalPositiveReplies,
                    positiveReplyRate,
                    positiveReplyRateChange
                },
                topCampaigns = new List<object>(),
                topClients = new List<object>(),
                performanceTrend = new List<object>(),
                emailAccountSummary = new
                {
                    totalAccounts = emailAccounts.Count,
                    activeAccounts = emailAccounts.Count(ea => ea.Status?.ToLower() == "active"),
                    accountsByStatus = emailAccounts
                        .GroupBy(ea => ea.Status?.ToLower() ?? "unknown")
                        .ToDictionary(g => g.Key, g => new
                        {
                            count = g.Count(),
                            percentage = Math.Round((double)g.Count() / Math.Max(emailAccounts.Count, 1) * 100, 1)
                        }),
                    accountsByProvider = emailAccounts
                        .Where(ea => !string.IsNullOrEmpty(ea.Email))
                        .GroupBy(ea => ea.Email.Split('@').LastOrDefault()?.ToLower() ?? "unknown")
                        .ToDictionary(g => g.Key, g => g.Count())
                },
                recentActivities = new List<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching filtered analytics overview");
            return StatusCode(500, new { message = "Error fetching filtered analytics overview" });
        }
    }

    [HttpGet("dashboard/performance-trend")]
    public async Task<ActionResult> GetDashboardPerformanceTrend([FromQuery] string period = "30")
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse period
            var days = period switch
            {
                "7" => 7,
                "30" => 30,
                "90" => 90,
                "6m" => 180,
                "1y" => 365,
                "9999" => 3650, // 10 years for all-time data
                "all" => 3650,
                _ => 30
            };
            
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);
            
            // Get campaigns with role-based filtering
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var campaigns = allCampaigns.ToList();
            
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                campaigns = campaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                campaigns = new List<CampaignDetailsDbModel>();
            }
            
            var campaignIdStrings = campaigns.Select(c => c.CampaignId.ToString()).ToList();

            // Get daily stats from campaign events for the period
            var dailyStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                startDate, endDate, campaignIdStrings);

            // Map to trend data format
            var trendData = dailyStats
                .Select(s => new
                {
                    date = s.Date.ToString("yyyy-MM-dd"),
                    emailsSent = s.TotalSent,
                    emailsOpened = s.TotalOpened,
                    emailsReplied = s.TotalReplied,
                    emailsBounced = s.TotalBounced,
                    positiveReplies = s.TotalPositiveReplies,
                    replyRate = s.TotalSent > 0 ? Math.Round((double)s.TotalReplied / s.TotalSent * 100, 2) : 0,
                    openRate = s.TotalSent > 0 ? Math.Round((double)s.TotalOpened / s.TotalSent * 100, 2) : 0,
                    bounceRate = s.TotalSent > 0 ? Math.Round((double)s.TotalBounced / s.TotalSent * 100, 2) : 0,
                    positiveReplyRate = s.TotalReplied > 0 ? Math.Round((double)s.TotalPositiveReplies / s.TotalReplied * 100, 2) : 0
                })
                .Where(t => t.emailsSent > 0 || t.emailsOpened > 0 || t.emailsReplied > 0) // Only include days with actual activity
                .OrderBy(t => t.date)
                .ToList();
            
            return Ok(trendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard performance trends");
            return StatusCode(500, new { message = "Error fetching dashboard performance trends" });
        }
    }

    [HttpPost("dashboard/filtered-performance-trend")]
    public async Task<ActionResult> GetFilteredDashboardPerformanceTrend([FromBody] object filterRequest)
    {
        // For now, just return the same as the regular performance trend
        // In the future, this could handle filtering by clients, campaigns, etc.
        return await GetDashboardPerformanceTrend("30");
    }

    [HttpGet("dashboard/campaign-performance-trend")]
    public async Task<ActionResult> GetDashboardCampaignPerformanceTrend([FromQuery] string period = "30")
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse period
            var days = period switch
            {
                "7" => 7,
                "30" => 30,
                "90" => 90,
                "6m" => 180,
                "1y" => 365,
                "9999" => 3650, // 10 years for all-time data
                "all" => 3650,
                _ => 30
            };
            
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);
            
            // Get campaigns with role-based filtering
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var campaigns = allCampaigns.ToList();
            
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                campaigns = campaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                campaigns = new List<CampaignDetailsDbModel>();
            }

            var campaignIdStrings = campaigns.Select(c => c.CampaignId.ToString()).ToList();

            // Get daily stats from campaign events for the period
            var dailyStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                startDate, endDate, campaignIdStrings);

            // Map to trend data format (time-series data for campaign performance)
            var campaignTrendData = dailyStats
                .Select(s => new
                {
                    date = s.Date.ToString("yyyy-MM-dd"),
                    emailsSent = s.TotalSent,
                    emailsOpened = s.TotalOpened,
                    emailsReplied = s.TotalReplied,
                    emailsBounced = s.TotalBounced,
                    positiveReplies = s.TotalPositiveReplies,
                    replyRate = s.TotalSent > 0 ? Math.Round((double)s.TotalReplied / s.TotalSent * 100, 2) : 0,
                    openRate = s.TotalSent > 0 ? Math.Round((double)s.TotalOpened / s.TotalSent * 100, 2) : 0,
                    bounceRate = s.TotalSent > 0 ? Math.Round((double)s.TotalBounced / s.TotalSent * 100, 2) : 0,
                    positiveReplyRate = s.TotalReplied > 0 ? Math.Round((double)s.TotalPositiveReplies / s.TotalReplied * 100, 2) : 0
                })
                .Where(t => t.emailsSent > 0 || t.emailsOpened > 0 || t.emailsReplied > 0) // Only include days with actual activity
                .OrderBy(t => t.date)
                .ToList();

            return Ok(campaignTrendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard campaign performance trends");
            return StatusCode(500, new { message = "Error fetching dashboard campaign performance trends" });
        }
    }

    [HttpPost("dashboard/filtered-campaign-performance-trend")]
    public async Task<ActionResult> GetFilteredDashboardCampaignPerformanceTrend([FromBody] DashboardFilterRequest filterRequest)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse period
            var days = filterRequest.Period switch
            {
                "7" => 7,
                "30" => 30,
                "90" => 90,
                "6m" => 180,
                "1y" => 365,
                "9999" => 3650, // 10 years for all-time data
                "all" => 3650,
                _ => 30
            };
            
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);
            
            // Get campaigns with role-based and request filtering
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var campaigns = allCampaigns.ToList();
            
            // Apply role-based filtering
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                campaigns = campaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                campaigns = new List<CampaignDetailsDbModel>();
            }
            
            // Apply client filter from request if provided
            if (filterRequest.ClientIds != null && filterRequest.ClientIds.Any())
            {
                campaigns = campaigns.Where(c => filterRequest.ClientIds.Contains(c.ClientId ?? "")).ToList();
            }

            var campaignIdStrings = campaigns.Select(c => c.CampaignId.ToString()).ToList();

            // Get daily stats from campaign events for the period
            var dailyStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                startDate, endDate, campaignIdStrings);

            // Map to trend data format (time-series data for campaign performance)
            var campaignTrendData = dailyStats
                .Select(s => new
                {
                    date = s.Date.ToString("yyyy-MM-dd"),
                    emailsSent = s.TotalSent,
                    emailsOpened = s.TotalOpened,
                    emailsReplied = s.TotalReplied,
                    emailsBounced = s.TotalBounced,
                    positiveReplies = s.TotalPositiveReplies,
                    replyRate = s.TotalSent > 0 ? Math.Round((double)s.TotalReplied / s.TotalSent * 100, 2) : 0,
                    openRate = s.TotalSent > 0 ? Math.Round((double)s.TotalOpened / s.TotalSent * 100, 2) : 0,
                    bounceRate = s.TotalSent > 0 ? Math.Round((double)s.TotalBounced / s.TotalSent * 100, 2) : 0,
                    positiveReplyRate = s.TotalReplied > 0 ? Math.Round((double)s.TotalPositiveReplies / s.TotalReplied * 100, 2) : 0
                })
                .Where(t => t.emailsSent > 0 || t.emailsOpened > 0 || t.emailsReplied > 0) // Only include days with actual activity
                .OrderBy(t => t.date)
                .ToList();
            
            return Ok(campaignTrendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching filtered dashboard campaign performance trends");
            return StatusCode(500, new { message = "Error fetching filtered dashboard campaign performance trends" });
        }
    }

    [HttpGet("performance-trends")]
    public async Task<ActionResult> GetPerformanceTrends(
        [FromQuery] string period = "30d",
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse period
            var days = period switch
            {
                "7d" => 7,
                "30d" => 30,
                "90d" => 90,
                _ => 30
            };
            
            var endDateParsed = !string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endParsedValue) 
                ? endParsedValue.Date 
                : DateTime.UtcNow.Date;
            var startDateParsed = !string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startParsedValue) 
                ? startParsedValue.Date 
                : endDateParsed.AddDays(-days);
            
            // Get campaigns with role-based filtering
            var allCampaignsQuery = await _campaignRepository.GetAllAsync();
            var campaigns = allCampaignsQuery.ToList();
            
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                campaigns = campaigns.Where(c => assignedClientIds.Contains(c.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                campaigns = new List<CampaignDetailsDbModel>();
            }
            
            var campaignIds = campaigns.Select(c => c.Id).ToList();
            
            // Get daily stats for the period
            var dailyStats = await _campaignDailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
                campaignIds, startDateParsed, endDateParsed);
            
            // Group by date and sum up metrics
            var trendData = dailyStats
                .GroupBy(s => s.StatDate)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    emailsSent = g.Sum(s => s.Sent),
                    emailsOpened = g.Sum(s => s.Opened),
                    emailsReplied = g.Sum(s => s.Replied),
                    emailsBounced = g.Sum(s => s.Bounced),
                    replyRate = g.Sum(s => s.Sent) > 0 ? Math.Round((double)g.Sum(s => s.Replied) / g.Sum(s => s.Sent) * 100, 2) : 0,
                    openRate = g.Sum(s => s.Sent) > 0 ? Math.Round((double)g.Sum(s => s.Opened) / g.Sum(s => s.Sent) * 100, 2) : 0,
                    bounceRate = g.Sum(s => s.Sent) > 0 ? Math.Round((double)g.Sum(s => s.Bounced) / g.Sum(s => s.Sent) * 100, 2) : 0
                })
                .Where(t => t.emailsSent > 0 || t.emailsOpened > 0 || t.emailsReplied > 0) // Only include days with actual activity
                .OrderBy(t => t.date)
                .ToList();
            
            return Ok(trendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching performance trends");
            return StatusCode(500, new { message = "Error fetching performance trends" });
        }
    }

    [HttpGet("email-account-performance")]
    public async Task<ActionResult> GetEmailAccountPerformance(
        [FromQuery] string period = "30d",
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int? limit = 10)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse period
            var days = period switch
            {
                "7d" => 7,
                "30d" => 30,
                "90d" => 90,
                _ => 30
            };
            
            var endDateParsed = !string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endParsedValue) 
                ? endParsedValue.Date 
                : DateTime.UtcNow.Date;
            var startDateParsed = !string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startParsedValue) 
                ? startParsedValue.Date 
                : endDateParsed.AddDays(-days);
            
            // Get email accounts with role-based filtering
            var emailAccounts = await _emailAccountRepository.GetAllAsync();
            
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                // Filter email accounts by assigned clients
                emailAccounts = emailAccounts.Where(ea => assignedClientIds.Contains(ea.ClientId ?? "")).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                emailAccounts = new List<EmailAccountDbModel>();
            }
            
            // Get email account daily stats
            var emailAccountIds = emailAccounts.Select(ea => ea.Id).ToList();
            var emailAccountStats = await _emailAccountDailyStatsRepository.GetByEmailAccountIdsAndDateRangeAsync(
                emailAccountIds, startDateParsed, endDateParsed);
            
            // Aggregate stats by email account
            var performanceData = emailAccountStats
                .GroupBy(s => s.EmailAccountId)
                .Select(g =>
                {
                    var emailAccount = emailAccounts.FirstOrDefault(ea => ea.Id == g.Key);
                    var totalSent = g.Sum(s => s.Sent);
                    var totalOpened = g.Sum(s => s.Opened);
                    var totalReplied = g.Sum(s => s.Replied);
                    var totalBounced = g.Sum(s => s.Bounced);
                    
                    return new
                    {
                        emailAccountId = g.Key,
                        email = emailAccount?.Email ?? "Unknown",
                        name = emailAccount?.Name ?? "Unnamed",
                        sent = totalSent,
                        opened = totalOpened,
                        replied = totalReplied,
                        bounced = totalBounced,
                        replyRate = totalSent > 0 ? Math.Round((double)totalReplied / totalSent * 100, 2) : 0,
                        openRate = totalSent > 0 ? Math.Round((double)totalOpened / totalSent * 100, 2) : 0,
                        bounceRate = totalSent > 0 ? Math.Round((double)totalBounced / totalSent * 100, 2) : 0
                    };
                })
                .OrderByDescending(p => p.sent)
                .Take(limit ?? 10)
                .ToList();
            
            return Ok(performanceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching email account performance");
            return StatusCode(500, new { message = "Error fetching email account performance" });
        }
    }

    [HttpGet("client-comparison")]
    public async Task<ActionResult> GetClientComparison(
        [FromQuery] string period = "30d",
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Parse period
            var days = period switch
            {
                "7d" => 7,
                "30d" => 30,
                "90d" => 90,
                _ => 30
            };
            
            var endDateParsed = !string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endParsedValue) 
                ? endParsedValue.Date 
                : DateTime.UtcNow.Date;
            var startDateParsed = !string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startParsedValue) 
                ? startParsedValue.Date 
                : endDateParsed.AddDays(-days);
            
            // Get clients with role-based filtering
            var clients = await _clientRepository.GetAllAsync();
            if (assignedClientIds != null && assignedClientIds.Any())
            {
                clients = clients.Where(c => assignedClientIds.Contains(c.Id)).ToList();
            }
            else if (assignedClientIds != null) // Empty list means user has no assigned clients
            {
                clients = new List<Client>();
            }
            
            var clientComparison = new List<object>();
            
            foreach (var client in clients)
            {
                // Get campaigns for this client
                var clientCampaignsQuery = await _campaignRepository.GetByClientIdAsync(client.Id);
                var clientCampaigns = clientCampaignsQuery.ToList();
                var campaignIds = clientCampaigns.Select(c => c.Id).ToList();
                
                if (campaignIds.Any())
                {
                    // Get stats for client campaigns
                    var clientStats = await _campaignDailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
                        campaignIds, startDateParsed, endDateParsed);
                    
                    var totalSent = clientStats.Sum(s => s.Sent);
                    var totalOpened = clientStats.Sum(s => s.Opened);
                    var totalReplied = clientStats.Sum(s => s.Replied);
                    var totalBounced = clientStats.Sum(s => s.Bounced);
                    
                    clientComparison.Add(new
                    {
                        clientId = client.Id,
                        clientName = client.Name,
                        clientColor = client.Color,
                        campaigns = clientCampaigns.Count,
                        activeCampaigns = clientCampaigns.Count(c => c.Status?.ToLower() == "active"),
                        sent = totalSent,
                        opened = totalOpened,
                        replied = totalReplied,
                        bounced = totalBounced,
                        replyRate = totalSent > 0 ? Math.Round((double)totalReplied / totalSent * 100, 2) : 0,
                        openRate = totalSent > 0 ? Math.Round((double)totalOpened / totalSent * 100, 2) : 0,
                        bounceRate = totalSent > 0 ? Math.Round((double)totalBounced / totalSent * 100, 2) : 0
                    });
                }
            }
            
            return Ok(clientComparison.OrderByDescending(c => ((dynamic)c).sent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client comparison");
            return StatusCode(500, new { message = "Error fetching client comparison" });
        }
    }
}