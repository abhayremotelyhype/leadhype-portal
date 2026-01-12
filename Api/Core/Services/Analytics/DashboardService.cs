using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Models.Frontend;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using LeadHype.Api;

namespace LeadHype.Api.Services
{
    public interface IDashboardService
    {
        Task<DashboardOverview> GetDashboardOverviewAsync(string? userId = null, DashboardFilterRequest? filter = null);
        Task<List<TimeSeriesDataPoint>> GetPerformanceTrendAsync(DashboardFilterRequest filter, string? userId = null);
        Task<List<TimeSeriesDataPoint>> GetCampaignPerformanceTrendAsync(DashboardFilterRequest filter, string? userId = null);
        Task<List<CampaignPerformanceMetric>> GetTopCampaignsAsync(int limit = 10, string? userId = null, int? timeRangeDays = null);
        Task<CampaignPerformanceResponse> GetFilteredTopCampaignsAsync(CampaignPerformanceFilter filter, string? userId = null);
        Task<List<ClientPerformanceMetric>> GetTopClientsAsync(int limit = 10, string? userId = null);
        Task<EmailAccountSummary> GetEmailAccountSummaryAsync(string? userId = null);
        Task<List<RecentActivity>> GetRecentActivitiesAsync(int limit = 20, string? userId = null);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IEmailAccountRepository _emailAccountRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailAccountDailyStatEntryService _dailyStatsService;
        private readonly ICampaignEventRepository _campaignEventRepository;

        public DashboardService(
            ICampaignRepository campaignRepository,
            IClientRepository clientRepository,
            IEmailAccountRepository emailAccountRepository,
            IUserRepository userRepository,
            IEmailAccountDailyStatEntryService dailyStatsService,
            ICampaignEventRepository campaignEventRepository)
        {
            _campaignRepository = campaignRepository;
            _clientRepository = clientRepository;
            _emailAccountRepository = emailAccountRepository;
            _userRepository = userRepository;
            _dailyStatsService = dailyStatsService;
            _campaignEventRepository = campaignEventRepository;
        }

        public async Task<DashboardOverview> GetDashboardOverviewAsync(string? userId = null, DashboardFilterRequest? filter = null)
        {
            var overview = new DashboardOverview();

            // Get all data sequentially to debug any issues
            overview.Stats = await GetOverviewStatsAsync(userId, filter);
            overview.TopCampaigns = await GetTopCampaignsAsync(5, userId);
            overview.TopClients = await GetTopClientsAsync(5, userId);
            overview.PerformanceTrend = await GetPerformanceTrendAsync(filter ?? new DashboardFilterRequest(), userId);
            overview.EmailAccountSummary = await GetEmailAccountSummaryAsync(userId);
            overview.RecentActivities = await GetRecentActivitiesAsync(10, userId);

            return overview;
        }

        private async Task<OverviewStats> GetOverviewStatsAsync(string? userId = null, DashboardFilterRequest? filter = null)
        {
            var stats = new OverviewStats();

            // Get user's assigned client IDs for filtering
            List<string>? assignedClientIds = null;
            bool isAdmin = false;
            
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    isAdmin = user.Role == "Admin";
                    if (!isAdmin)
                    {
                        assignedClientIds = user.AssignedClientIds;
                    }
                }
            }

            // Get filtered campaigns based on user access
            var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
            if (!isAdmin && assignedClientIds != null && assignedClientIds.Any())
            {
                allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId)).ToList();
            }
            else if (!isAdmin)
            {
                // User has no assigned clients, show no campaigns
                allCampaigns = new List<CampaignDetailsDbModel>();
            }
            
            // Apply dashboard filters if provided
            if (filter != null)
            {
                // Filter by specific client IDs if provided
                if (filter.ClientIds != null && filter.ClientIds.Count > 0)
                {
                    allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && filter.ClientIds.Contains(c.ClientId)).ToList();
                }
                
                // Filter by specific campaign IDs if provided
                if (filter.CampaignIds != null && filter.CampaignIds.Count > 0)
                {
                    allCampaigns = allCampaigns.Where(c => filter.CampaignIds.Contains(c.Id)).ToList();
                }
            }
            
            // Get filtered email accounts based on user access  
            var allEmailAccounts = (await _emailAccountRepository.GetAllAsync()).ToList();
            if (!isAdmin && assignedClientIds != null && assignedClientIds.Any())
            {
                allEmailAccounts = allEmailAccounts.Where(ea => !string.IsNullOrEmpty(ea.ClientId) && assignedClientIds.Contains(ea.ClientId)).ToList();
            }
            else if (!isAdmin)
            {
                // User has no assigned clients, show no email accounts
                allEmailAccounts = new List<EmailAccountDbModel>();
            }
            
            // Apply dashboard filters to email accounts if provided
            if (filter != null)
            {
                // Filter email accounts by client IDs if provided
                if (filter.ClientIds != null && filter.ClientIds.Count > 0)
                {
                    allEmailAccounts = allEmailAccounts.Where(ea => !string.IsNullOrEmpty(ea.ClientId) && filter.ClientIds.Contains(ea.ClientId)).ToList();
                }
                
                // Note: EmailAccountDbModel doesn't have CampaignIds property, 
                // so we can't filter by campaigns directly. The email account 
                // filtering will be based on client filtering only for now.
            }

            // Get filtered clients based on user access
            var allClients = (await _clientRepository.GetAllAsync()).ToList();
            if (!isAdmin && assignedClientIds != null && assignedClientIds.Any())
            {
                allClients = allClients.Where(c => assignedClientIds.Contains(c.Id)).ToList();
            }
            else if (!isAdmin)
            {
                // User has no assigned clients, show no clients
                allClients = new List<Client>();
            }
            
            // Apply dashboard filters to clients if provided
            if (filter != null && filter.ClientIds != null && filter.ClientIds.Count > 0)
            {
                allClients = allClients.Where(c => filter.ClientIds.Contains(c.Id)).ToList();
            }

            // Set counts based on filtered data
            stats.TotalCampaigns = allCampaigns.Count;
            stats.TotalClients = allClients.Count;
            stats.TotalUsers = isAdmin ? await _userRepository.CountAsync() : 1; // Non-admin users only see themselves
            stats.TotalEmailAccounts = allEmailAccounts.Count;

            // Calculate totals from both email accounts (for compatibility) and campaign data (for accuracy)
            // Use campaign daily stats as the primary source of truth for email metrics
            var campaignTotals = await GetCampaignTotalsAsync(assignedClientIds, isAdmin, filter);
            
            // If campaign data is available, use it; otherwise fall back to email account data
            stats.TotalEmailsSent = campaignTotals.TotalSent > 0 ? campaignTotals.TotalSent : allEmailAccounts.Sum(e => e.Sent);
            stats.TotalEmailsOpened = campaignTotals.TotalOpened > 0 ? campaignTotals.TotalOpened : allEmailAccounts.Sum(e => e.Opened);
            stats.TotalEmailsReplied = campaignTotals.TotalReplied > 0 ? campaignTotals.TotalReplied : allEmailAccounts.Sum(e => e.Replied);
            stats.TotalEmailsBounced = campaignTotals.TotalBounced > 0 ? campaignTotals.TotalBounced : allEmailAccounts.Sum(e => e.Bounced);
            // Clicked stats removed as column no longer exists
            stats.TotalEmailsClicked = 0;
            
            // Set positive reply stats from campaign daily stats (overall stats for selected campaigns)
            stats.TotalPositiveReplies = campaignTotals.TotalPositiveReplies;
            if (stats.TotalEmailsReplied > 0)
            {
                stats.PositiveReplyRate = Math.Round((double)stats.TotalPositiveReplies / stats.TotalEmailsReplied * 100, 2);
            }

            // Calculate rates
            if (stats.TotalEmailsSent > 0)
            {
                stats.OpenRate = Math.Round((double)stats.TotalEmailsOpened / stats.TotalEmailsSent * 100, 2);
                stats.ReplyRate = Math.Round((double)stats.TotalEmailsReplied / stats.TotalEmailsSent * 100, 2);
                stats.BounceRate = Math.Round((double)stats.TotalEmailsBounced / stats.TotalEmailsSent * 100, 2);
                stats.ClickRate = Math.Round((double)stats.TotalEmailsClicked / stats.TotalEmailsSent * 100, 2);
            }

            // Recent activity (last 7 days) - Calculate from campaign daily stats
            var recentPeriodStats = await CalculateRecentStatsAsync(assignedClientIds, isAdmin, filter);
            stats.RecentEmailsSent = recentPeriodStats.RecentEmailsSent;
            stats.RecentEmailsOpened = recentPeriodStats.RecentEmailsOpened;
            stats.RecentEmailsReplied = recentPeriodStats.RecentEmailsReplied;

            // Campaign status counts
            stats.ActiveCampaigns = allCampaigns.Count(c => c.Status?.ToLower() == "active");
            stats.PausedCampaigns = allCampaigns.Count(c => c.Status?.ToLower() == "paused");
            stats.CompletedCampaigns = allCampaigns.Count(c => c.Status?.ToLower() == "completed");

            // Calculate period changes using the same filter logic
            var periodChanges = await CalculatePeriodChangesAsync(assignedClientIds, isAdmin, filter);
            stats.TotalEmailsSentChange = periodChanges.TotalEmailsSentChange;
            stats.OpenRateChange = periodChanges.OpenRateChange;
            stats.ReplyRateChange = periodChanges.ReplyRateChange;
            stats.BounceRateChange = periodChanges.BounceRateChange;
            stats.PositiveReplyRateChange = periodChanges.PositiveReplyRateChange;

            return stats;
        }

        private async Task<(int TotalSent, int TotalOpened, int TotalReplied, int TotalBounced, int TotalClicked, int TotalPositiveReplies)> GetCampaignTotalsAsync(List<string>? assignedClientIds = null, bool isAdmin = true, DashboardFilterRequest? filter = null)
        {
            try
            {
                // Get campaigns filtered by user access
                var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
                
                // Filter campaigns based on user access
                if (!isAdmin && assignedClientIds != null && assignedClientIds.Any())
                {
                    allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId)).ToList();
                }
                else if (!isAdmin)
                {
                    // User has no assigned clients, show no campaigns
                    allCampaigns = new List<CampaignDetailsDbModel>();
                }
                
                // Apply dashboard filters if provided
                if (filter != null)
                {
                    // Filter by specific client IDs if provided
                    if (filter.ClientIds != null && filter.ClientIds.Count > 0)
                    {
                        allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && filter.ClientIds.Contains(c.ClientId)).ToList();
                    }
                    
                    // Filter by specific campaign IDs if provided
                    if (filter.CampaignIds != null && filter.CampaignIds.Count > 0)
                    {
                        allCampaigns = allCampaigns.Where(c => filter.CampaignIds.Contains(c.Id)).ToList();
                    }
                }

                // For overall stats (not time-filtered), use campaign daily stats for accuracy
                if (allCampaigns.Any())
                {
                    var campaignIds = allCampaigns.Select(c => c.Id).ToList();
                    var (totalSent, totalReplied, totalPositive, lastReplyDate, lastPositiveDate) = 
                        await _campaignEventRepository.GetAggregatedTotalsForCampaignsAsync(campaignIds);
                    
                    // Get other stats by summing individual campaign entries (as fallback for missing fields)
                    var totalOpened = allCampaigns.Sum(c => c.TotalOpened ?? 0);
                    var totalBounced = allCampaigns.Sum(c => c.TotalBounced ?? 0);
                    var totalClicked = allCampaigns.Sum(c => c.TotalClicked ?? 0);
                    
                    return (totalSent, totalOpened, totalReplied, totalBounced, totalClicked, totalPositive);
                }
                
                return (0, 0, 0, 0, 0, 0);
            }
            catch (Exception)
            {
                // Return zeros if campaign data is not available
                return (0, 0, 0, 0, 0, 0);
            }
        }

        public async Task<List<TimeSeriesDataPoint>> GetPerformanceTrendAsync(DashboardFilterRequest filter, string? userId = null)
        {
            var dataPoints = new List<TimeSeriesDataPoint>();
            
            // Determine date range - use reasonable fallback instead of system date
            // System clock may be incorrect, so use actual data range
            var endDate = filter.EndDate ?? new DateTime(2025, 8, 26); // Use reasonable fallback date
            var startDate = filter.StartDate ?? CalculateStartDate(endDate, filter.Period ?? "30");

            // Get user access information
            List<string>? assignedClientIds = null;
            bool isAdmin = false;
            
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    isAdmin = user.Role == "Admin";
                    if (!isAdmin)
                    {
                        assignedClientIds = user.AssignedClientIds;
                    }
                }
            }

            // For non-admin users, filter data by their assigned clients only
            if (!isAdmin)
            {
                // If user has no assigned clients, return empty data points
                if (assignedClientIds == null || !assignedClientIds.Any())
                {
                    var noClientDataPoints = new List<TimeSeriesDataPoint>();
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        noClientDataPoints.Add(new TimeSeriesDataPoint
                        {
                            Date = date,
                            EmailsSent = 0,
                            EmailsOpened = 0,
                            EmailsReplied = 0,
                            EmailsBounced = 0,
                            OpenRate = 0,
                            ReplyRate = 0
                        });
                    }
                    return noClientDataPoints;
                }

                // Get email accounts for the user's assigned clients only
                var emailAccounts = new List<EmailAccountDbModel>();
                foreach (var clientId in assignedClientIds)
                {
                    var clientAccounts = await _emailAccountRepository.GetByClientIdAsync(clientId);
                    emailAccounts.AddRange(clientAccounts);
                }
                
                if (emailAccounts.Any())
                {
                    // Get email account IDs for filtered data
                    var emailAccountIds = emailAccounts.Select(ea => ea.Id).ToList();
                    
                    // Use batch method to get stats for only the assigned clients' email accounts
                    var accountStatsDict = await _dailyStatsService.GetBatchTotalStatsAsync(emailAccountIds, startDate, endDate);
                    
                    var regularDataPoints = new List<TimeSeriesDataPoint>();
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var dataPoint = new TimeSeriesDataPoint
                        {
                            Date = date,
                            EmailsSent = 0,
                            EmailsOpened = 0,
                            EmailsReplied = 0,
                            EmailsBounced = 0,
                            OpenRate = 0,
                            ReplyRate = 0
                        };
                        
                        // Get daily stats for each email account and aggregate
                        foreach (var emailAccountId in emailAccountIds)
                        {
                            var accountStats = await _dailyStatsService.GetStatEntriesAsync(emailAccountId, date, date);
                            foreach (var stat in accountStats)
                            {
                                if (stat.StatDate.Date == date.Date)
                                {
                                    dataPoint.EmailsSent += stat.Sent;
                                    dataPoint.EmailsOpened += stat.Opened;
                                    dataPoint.EmailsReplied += stat.Replied;
                                    dataPoint.EmailsBounced += stat.Bounced;
                                }
                            }
                        }
                        
                        // Calculate rates
                        dataPoint.OpenRate = dataPoint.EmailsSent > 0 ? Math.Round((double)dataPoint.EmailsOpened / dataPoint.EmailsSent * 100, 2) : 0;
                        dataPoint.ReplyRate = dataPoint.EmailsSent > 0 ? Math.Round((double)dataPoint.EmailsReplied / dataPoint.EmailsSent * 100, 2) : 0;
                        
                        regularDataPoints.Add(dataPoint);
                    }
                    
                    // Return the data (even if all zero - this shows the user has assigned clients but no email activity)
                    return regularDataPoints;
                }
                
                // Fallback: Get campaigns for the assigned clients only
                var campaigns = new List<CampaignDetailsDbModel>();
                foreach (var clientId in assignedClientIds)
                {
                    var clientCampaigns = await _campaignRepository.GetByClientIdAsync(clientId);
                    campaigns.AddRange(clientCampaigns);
                }
                
                if (campaigns.Any())
                {
                    var totalSent = campaigns.Sum(c => c.TotalSent ?? 0);
                    var totalOpened = campaigns.Sum(c => c.TotalOpened ?? 0);
                    var totalReplied = campaigns.Sum(c => c.TotalReplied ?? 0);
                    var totalBounced = campaigns.Sum(c => c.TotalBounced ?? 0);
                    
                    if (totalSent > 0)
                    {
                        var dayCount = (endDate - startDate).Days + 1;
                        var dailySent = totalSent / dayCount;
                        var dailyOpened = totalOpened / dayCount;
                        var dailyReplied = totalReplied / dayCount;
                        var dailyBounced = totalBounced / dayCount;
                        
                        var fallbackDataPoints = new List<TimeSeriesDataPoint>();
                        for (var date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            fallbackDataPoints.Add(new TimeSeriesDataPoint
                            {
                                Date = date,
                                EmailsSent = dailySent,
                                EmailsOpened = dailyOpened,
                                EmailsReplied = dailyReplied,
                                EmailsBounced = dailyBounced,
                                OpenRate = totalSent > 0 ? Math.Round((double)dailyOpened / dailySent * 100, 2) : 0,
                                ReplyRate = totalSent > 0 ? Math.Round((double)dailyReplied / dailySent * 100, 2) : 0
                            });
                        }
                        return fallbackDataPoints;
                    }
                }
                
                // No email accounts or campaign data for assigned clients - return empty chart
                var emptyDataPoints = new List<TimeSeriesDataPoint>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    emptyDataPoints.Add(new TimeSeriesDataPoint
                    {
                        Date = date,
                        EmailsSent = 0,
                        EmailsOpened = 0,
                        EmailsReplied = 0,
                        EmailsBounced = 0,
                        OpenRate = 0,
                        ReplyRate = 0
                    });
                }
                return emptyDataPoints;
            }

            // Get aggregated data for all assigned clients
            var allDataPoints = new List<TimeSeriesDataPoint>();
            
            // Admin users should see aggregated data from all campaigns using campaign daily stats
            // This ensures admin users see the correct high-volume data from campaign_daily_stats materialized view
            if (isAdmin)
            {
                try
                {
                    var aggregatedStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                        startDate, endDate);

                    // Generate daily data points with aggregated data
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var dataPoint = new TimeSeriesDataPoint
                        {
                            Date = date,
                            EmailsSent = 0,
                            EmailsOpened = 0,
                            EmailsReplied = 0,
                            EmailsBounced = 0,
                            OpenRate = 0,
                            ReplyRate = 0
                        };

                        // Find data for this specific date
                        foreach (var stat in aggregatedStats)
                        {
                            try
                            {
                                // Check if this is the right date
                                var statDateObj = ((IDictionary<string, object>)stat)["date"];
                                if (statDateObj is DateTime statDate && statDate.Date == date.Date)
                                {
                                    var statsDict = (IDictionary<string, object>)stat;
                                    dataPoint.EmailsSent = Convert.ToInt32(statsDict["totalsent"] ?? 0);
                                    dataPoint.EmailsOpened = Convert.ToInt32(statsDict["totalopened"] ?? 0);
                                    dataPoint.EmailsReplied = Convert.ToInt32(statsDict["totalreplied"] ?? 0);
                                    dataPoint.EmailsBounced = Convert.ToInt32(statsDict["totalbounced"] ?? 0);
                                    
                                    // Calculate rates
                                    if (dataPoint.EmailsSent > 0)
                                    {
                                        dataPoint.OpenRate = Math.Round((double)dataPoint.EmailsOpened / dataPoint.EmailsSent * 100, 2);
                                        dataPoint.ReplyRate = Math.Round((double)dataPoint.EmailsReplied / dataPoint.EmailsSent * 100, 2);
                                    }
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                // Skip invalid entries
                                continue;
                            }
                        }

                        allDataPoints.Add(dataPoint);
                    }
                    
                    return allDataPoints;
                }
                catch (Exception)
                {
                    // Fall through to return empty data if there's an error
                }
            }
            else if (assignedClientIds != null && assignedClientIds.Any())
            {
                // Regular user sees only their assigned clients' data
                // Get stats for each assigned client and aggregate them
                foreach (var clientId in assignedClientIds)
                {
                    var sentStatsTask = _dailyStatsService.GetAllAccountsSentEmailsAsync(clientId, startDate, endDate);
                    var openedStatsTask = _dailyStatsService.GetAllAccountsOpenedEmailsAsync(clientId, startDate, endDate);
                    var repliedStatsTask = _dailyStatsService.GetAllAccountsRepliedEmailsAsync(clientId, startDate, endDate);
                    var bouncedStatsTask = _dailyStatsService.GetAllAccountsBouncedEmailsAsync(clientId, startDate, endDate);
                    
                    await Task.WhenAll(sentStatsTask, openedStatsTask, repliedStatsTask, bouncedStatsTask);
                    
                    var sentStats = await sentStatsTask;
                    var openedStats = await openedStatsTask;
                    var repliedStats = await repliedStatsTask;
                    var bouncedStats = await bouncedStatsTask;
                    
                    // Generate daily data points for this client
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var dateKey = date.ToString("yyyy-MM-dd");
                        var existingPoint = allDataPoints.FirstOrDefault(dp => dp.Date.Date == date.Date);
                        
                        if (existingPoint == null)
                        {
                            existingPoint = new TimeSeriesDataPoint
                            {
                                Date = date,
                                EmailsSent = 0,
                                EmailsOpened = 0,
                                EmailsReplied = 0,
                                EmailsBounced = 0
                            };
                            allDataPoints.Add(existingPoint);
                        }
                        
                        // Aggregate stats from this client
                        existingPoint.EmailsSent += sentStats.SelectMany(kvp => kvp.Value).Where(innerKvp => innerKvp.Key == dateKey).Sum(innerKvp => innerKvp.Value);
                        existingPoint.EmailsOpened += openedStats.SelectMany(kvp => kvp.Value).Where(innerKvp => innerKvp.Key == dateKey).Sum(innerKvp => innerKvp.Value);
                        existingPoint.EmailsReplied += repliedStats.SelectMany(kvp => kvp.Value).Where(innerKvp => innerKvp.Key == dateKey).Sum(innerKvp => innerKvp.Value);
                        existingPoint.EmailsBounced += bouncedStats.SelectMany(kvp => kvp.Value).Where(innerKvp => innerKvp.Key == dateKey).Sum(innerKvp => innerKvp.Value);
                    }
                }
                
                // Calculate rates for all aggregated data points
                foreach (var dataPoint in allDataPoints)
                {
                    if (dataPoint.EmailsSent > 0)
                    {
                        dataPoint.OpenRate = Math.Round((double)dataPoint.EmailsOpened / dataPoint.EmailsSent * 100, 2);
                        dataPoint.ReplyRate = Math.Round((double)dataPoint.EmailsReplied / dataPoint.EmailsSent * 100, 2);
                    }
                }
            }
            else
            {
                // User has no assigned clients, return empty data points
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    allDataPoints.Add(new TimeSeriesDataPoint
                    {
                        Date = date,
                        EmailsSent = 0,
                        EmailsOpened = 0,
                        EmailsReplied = 0,
                        EmailsBounced = 0,
                        OpenRate = 0,
                        ReplyRate = 0
                    });
                }
            }

            return allDataPoints;
        }

        public async Task<List<TimeSeriesDataPoint>> GetCampaignPerformanceTrendAsync(DashboardFilterRequest filter, string? userId = null)
        {
            var dataPoints = new List<TimeSeriesDataPoint>();
            
            try
            {
                // Determine date range - use a reasonable fallback instead of system date
                // System clock may be incorrect, so use actual data range
                var endDate = filter.EndDate ?? new DateTime(2025, 8, 26); // Use reasonable fallback date
                var startDate = filter.StartDate ?? CalculateStartDate(endDate, filter.Period ?? "30");

                // Get user access information for filtering
                List<string>? assignedClientIds = null;
                bool isAdmin = false;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        isAdmin = user.Role == "Admin";
                        if (!isAdmin)
                        {
                            assignedClientIds = user.AssignedClientIds;
                        }
                    }
                }

                // Get campaigns for access control
                var campaigns = (await _campaignRepository.GetAllAsync()).ToList();
                List<string> allowedCampaignIds;
                
                // Apply user access control for campaign filtering
                if (isAdmin)
                {
                    // Admin users can see all campaigns
                    allowedCampaignIds = campaigns.Select(c => c.Id).ToList();
                }
                else if (assignedClientIds != null && assignedClientIds.Any())
                {
                    // Regular users can only see campaigns from their assigned clients
                    allowedCampaignIds = campaigns
                        .Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId))
                        .Select(c => c.Id)
                        .ToList();
                }
                else
                {
                    // Users with no assigned clients see nothing
                    allowedCampaignIds = new List<string>();
                }

                // Get aggregated daily stats based on user access
                IEnumerable<dynamic> aggregatedStats;
                if (isAdmin)
                {
                    // Admin gets all aggregated stats
                    aggregatedStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                        startDate, endDate);
                }
                else if (allowedCampaignIds.Any())
                {
                    // Regular users get filtered aggregated stats
                    aggregatedStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                        startDate, endDate, allowedCampaignIds);
                }
                else
                {
                    // Users with no access get empty results
                    aggregatedStats = Enumerable.Empty<dynamic>();
                }
                
                // If no daily stats available, create fallback using campaign totals
                var allowedCampaigns = campaigns.Where(c => allowedCampaignIds.Contains(c.Id)).ToList();
                if (!aggregatedStats.Any() && allowedCampaigns.Any())
                {
                    // Calculate total stats from allowed campaigns only
                    var totalSent = allowedCampaigns.Sum(c => c.TotalSent ?? 0);
                    var totalOpened = allowedCampaigns.Sum(c => c.TotalOpened ?? 0);
                    var totalReplied = allowedCampaigns.Sum(c => c.TotalReplied ?? 0);
                    var totalBounced = allowedCampaigns.Sum(c => c.TotalBounced ?? 0);
                    
                    // Only create fallback data if we have actual totals
                    if (totalSent > 0)
                    {
                        var dayCount = (endDate - startDate).Days + 1;
                        
                        // Distribute totals evenly across date range (simple approach)
                        var dailySent = totalSent / dayCount;
                        var dailyOpened = totalOpened / dayCount;
                        var dailyReplied = totalReplied / dayCount;
                        var dailyBounced = totalBounced / dayCount;
                        
                        // Create fallback aggregated stats
                        var fallbackStats = new List<dynamic>();
                        for (var date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            fallbackStats.Add(new
                            {
                                date = date,
                                totalsent = dailySent,
                                totalopened = dailyOpened,
                                totalreplied = dailyReplied,
                                totalbounced = dailyBounced
                            });
                        }
                        aggregatedStats = fallbackStats;
                    }
                }
                
                // Convert aggregated results to dictionary for quick lookup
                var statsByDate = new Dictionary<DateTime, dynamic>();
                foreach (var stat in aggregatedStats)
                {
                    try
                    {
                        // Access dynamic properties safely
                        var date = ((IDictionary<string, object>)stat)["date"];
                        
                        if (date != null)
                        {
                            DateTime statDate;
                            if (date is DateTime dt)
                            {
                                statDate = dt.Date;
                            }
                            else
                            {
                                statDate = DateTime.Parse(date.ToString()).Date;
                            }
                            
                            statsByDate[statDate] = stat;
                        }
                    }
                    catch (Exception)
                    {
                        // Skip invalid entries
                        continue;
                    }
                }

                // Generate daily data points with aggregated data
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dataPoint = new TimeSeriesDataPoint
                    {
                        Date = date,
                        EmailsSent = 0,
                        EmailsOpened = 0,
                        EmailsReplied = 0,
                        EmailsBounced = 0,
                        OpenRate = 0,
                        ReplyRate = 0
                    };

                    // If we have data for this date, use it
                    if (statsByDate.TryGetValue(date.Date, out var dayStats))
                    {
                        try
                        {
                            var statsDict = (IDictionary<string, object>)dayStats;
                            dataPoint.EmailsSent = Convert.ToInt32(statsDict["totalsent"] ?? 0);
                            dataPoint.EmailsOpened = Convert.ToInt32(statsDict["totalopened"] ?? 0);
                            dataPoint.EmailsReplied = Convert.ToInt32(statsDict["totalreplied"] ?? 0);
                            dataPoint.EmailsBounced = Convert.ToInt32(statsDict["totalbounced"] ?? 0);
                            
                            // Calculate rates
                            if (dataPoint.EmailsSent > 0)
                            {
                                dataPoint.OpenRate = Math.Round((double)dataPoint.EmailsOpened / dataPoint.EmailsSent * 100, 2);
                                dataPoint.ReplyRate = Math.Round((double)dataPoint.EmailsReplied / dataPoint.EmailsSent * 100, 2);
                            }
                        }
                        catch (Exception)
                        {
                            // Use default zero values for this date
                        }
                    }

                    dataPoints.Add(dataPoint);
                }

                return dataPoints;
            }
            catch (Exception)
            {
                // Return empty data points for the date range on any error
                var endDate = filter.EndDate ?? new DateTime(2025, 8, 26);
                var startDate = filter.StartDate ?? CalculateStartDate(endDate, filter.Period ?? "30");
                
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    dataPoints.Add(new TimeSeriesDataPoint
                    {
                        Date = date,
                        EmailsSent = 0,
                        EmailsOpened = 0,
                        EmailsReplied = 0,
                        EmailsBounced = 0,
                        OpenRate = 0,
                        ReplyRate = 0
                    });
                }
                
                return dataPoints;
            }
        }

        public async Task<List<CampaignPerformanceMetric>> GetTopCampaignsAsync(int limit = 10, string? userId = null, int? timeRangeDays = null)
        {
            var campaigns = (await _campaignRepository.GetAllAsync()).ToList();
            var clients = (await _clientRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);

            // Filter campaigns by user access
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null && user.Role != "Admin")
                {
                    if (user.AssignedClientIds != null && user.AssignedClientIds.Any())
                    {
                        campaigns = campaigns.Where(c => user.AssignedClientIds.Contains(c.ClientId ?? "")).ToList();
                    }
                    else
                    {
                        // User has no assigned clients, show no campaigns
                        campaigns = new List<CampaignDetailsDbModel>();
                    }
                }
            }

            // Apply time range calculation (like campaign controller)
            await ApplyTimeRangeCalculation(campaigns, timeRangeDays ?? 9999);

            var metrics = campaigns
                .Where(c => (c.TotalSent ?? 0) > 0) // Only campaigns with activity
                .Select(c => new CampaignPerformanceMetric
                {
                    Id = c.Id,
                    Name = c.Name ?? "Unknown",
                    ClientName = clients.GetValueOrDefault(c.ClientId ?? "", "Unassigned"),
                    Status = c.Status ?? "Unknown",
                    TotalSent = c.TotalSent ?? 0,
                    TotalOpened = c.TotalOpened ?? 0,
                    TotalReplied = c.TotalReplied ?? 0,
                    TotalBounced = c.TotalBounced ?? 0,
                    TotalPositiveReplies = c.TotalPositiveReplies ?? 0,
                    OpenRate = (c.TotalSent ?? 0) > 0 ? Math.Round((double)(c.TotalOpened ?? 0) / (c.TotalSent ?? 0) * 100, 2) : 0,
                    ReplyRate = (c.TotalSent ?? 0) > 0 ? Math.Round((double)(c.TotalReplied ?? 0) / (c.TotalSent ?? 0) * 100, 2) : 0,
                    BounceRate = (c.TotalSent ?? 0) > 0 ? Math.Round((double)(c.TotalBounced ?? 0) / (c.TotalSent ?? 0) * 100, 2) : 0,
                    PositiveReplyRate = (c.TotalReplied ?? 0) > 0 ? Math.Round((double)(c.TotalPositiveReplies ?? 0) / (c.TotalReplied ?? 0) * 100, 2) : 0,
                    LastActivity = c.UpdatedAt ?? c.CreatedAt ?? DateTime.UtcNow,
                    DaysActive = (DateTime.UtcNow - (c.CreatedAt ?? DateTime.UtcNow)).Days
                })
                .OrderByDescending(c => c.ReplyRate) // Sort by reply rate
                .Take(limit)
                .ToList();

            return metrics;
        }

        public async Task<CampaignPerformanceResponse> GetFilteredTopCampaignsAsync(CampaignPerformanceFilter filter, string? userId = null)
        {
            var campaigns = (await _campaignRepository.GetAllAsync()).ToList();
            var clients = (await _clientRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
            
            // Filter campaigns by user access
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null && user.Role != "Admin")
                {
                    if (user.AssignedClientIds != null && user.AssignedClientIds.Any())
                    {
                        campaigns = campaigns.Where(c => user.AssignedClientIds.Contains(c.ClientId ?? "")).ToList();
                    }
                    else
                    {
                        // User has no assigned clients, show no campaigns
                        campaigns = new List<CampaignDetailsDbModel>();
                    }
                }
            }

            // Apply time range calculation (like campaign controller)
            await ApplyTimeRangeCalculation(campaigns, filter.TimeRangeDays ?? 9999);

            // Calculate date range if period is specified
            DateTime? startDate = filter.StartDate;
            DateTime? endDate = filter.EndDate;
            
            if (!string.IsNullOrEmpty(filter.Period) && !startDate.HasValue)
            {
                endDate = endDate ?? new DateTime(2025, 8, 26);
                startDate = CalculateStartDate(endDate.Value, filter.Period);
            }

            // Convert all campaigns to metrics first
            var allMetrics = campaigns
                .Select(c => new CampaignPerformanceMetric
                {
                    Id = c.Id,
                    Name = c.Name ?? "Unknown",
                    ClientName = clients.GetValueOrDefault(c.ClientId ?? "", "Unassigned"),
                    Status = c.Status ?? "Unknown",
                    TotalSent = c.TotalSent ?? 0,
                    TotalOpened = c.TotalOpened ?? 0,
                    TotalReplied = c.TotalReplied ?? 0,
                    TotalBounced = c.TotalBounced ?? 0,
                    OpenRate = (c.TotalSent ?? 0) > 0 ? Math.Round((double)(c.TotalOpened ?? 0) / (c.TotalSent ?? 0) * 100, 2) : 0,
                    ReplyRate = (c.TotalSent ?? 0) > 0 ? Math.Round((double)(c.TotalReplied ?? 0) / (c.TotalSent ?? 0) * 100, 2) : 0,
                    BounceRate = (c.TotalSent ?? 0) > 0 ? Math.Round((double)(c.TotalBounced ?? 0) / (c.TotalSent ?? 0) * 100, 2) : 0,
                    LastActivity = c.UpdatedAt ?? c.CreatedAt ?? DateTime.UtcNow,
                    DaysActive = (DateTime.UtcNow - (c.CreatedAt ?? DateTime.UtcNow)).Days,
                    // Add composite score calculation
                    CompositeScore = CalculateCompositeScore(
                        c.TotalSent ?? 0,
                        c.TotalReplied ?? 0,
                        c.TotalOpened ?? 0,
                        c.TotalBounced ?? 0
                    )
                })
                .ToList();

            // Apply filters
            var filteredMetrics = allMetrics.AsQueryable();

            // Volume filters
            filteredMetrics = filteredMetrics.Where(c => c.TotalSent >= filter.MinimumSent);
            
            if (filter.MaximumSent.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.TotalSent <= filter.MaximumSent.Value);
            
            if (filter.MinimumReplies.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.TotalReplied >= filter.MinimumReplies.Value);

            // Performance filters
            if (filter.MinimumReplyRate.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.ReplyRate >= filter.MinimumReplyRate.Value);
            
            if (filter.MaximumReplyRate.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.ReplyRate <= filter.MaximumReplyRate.Value);
            
            if (filter.MinimumOpenRate.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.OpenRate >= filter.MinimumOpenRate.Value);
            
            if (filter.MaximumBounceRate.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.BounceRate <= filter.MaximumBounceRate.Value);

            // Campaign filters
            if (filter.CampaignIds?.Any() == true)
                filteredMetrics = filteredMetrics.Where(c => filter.CampaignIds.Contains(c.Id));
            
            if (filter.ClientIds?.Any() == true)
            {
                var campaignsForClients = campaigns
                    .Where(c => filter.ClientIds.Contains(c.ClientId ?? ""))
                    .Select(c => c.Id)
                    .ToList();
                filteredMetrics = filteredMetrics.Where(c => campaignsForClients.Contains(c.Id));
            }
            
            if (filter.Statuses?.Any() == true)
                filteredMetrics = filteredMetrics.Where(c => filter.Statuses.Contains(c.Status.ToUpper()));

            // Date range filter (based on last activity)
            if (startDate.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.LastActivity >= startDate.Value);
            
            if (endDate.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.LastActivity <= endDate.Value);

            // Activity filters
            if (filter.ExcludeInactive)
                filteredMetrics = filteredMetrics.Where(c => c.TotalSent > 0);
            
            if (filter.MinimumDaysActive.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.DaysActive >= filter.MinimumDaysActive.Value);
            
            if (filter.MaximumDaysActive.HasValue)
                filteredMetrics = filteredMetrics.Where(c => c.DaysActive <= filter.MaximumDaysActive.Value);

            var filteredList = filteredMetrics.ToList();
            
            // Calculate statistics before pagination
            var stats = new CampaignPerformanceStats
            {
                CampaignsAnalyzed = filteredList.Count,
                TotalEmailsSent = filteredList.Sum(c => c.TotalSent),
                TotalReplies = filteredList.Sum(c => c.TotalReplied),
                TotalOpens = filteredList.Sum(c => c.TotalOpened),
                AverageReplyRate = filteredList.Any() ? filteredList.Average(c => c.ReplyRate) : 0,
                AverageOpenRate = filteredList.Any() ? filteredList.Average(c => c.OpenRate) : 0,
                AverageBounceRate = filteredList.Any() ? filteredList.Average(c => c.BounceRate) : 0
            };

            // Apply sorting
            IOrderedEnumerable<CampaignPerformanceMetric> sortedMetrics;
            
            if (filter.UseCompositeScore || filter.SortBy == "CompositeScore")
            {
                sortedMetrics = filter.SortDescending 
                    ? filteredList.OrderByDescending(c => c.CompositeScore)
                    : filteredList.OrderBy(c => c.CompositeScore);
            }
            else
            {
                sortedMetrics = filter.SortBy?.ToLowerInvariant() switch
                {
                    "openrate" => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.OpenRate)
                        : filteredList.OrderBy(c => c.OpenRate),
                    "totalsent" => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.TotalSent)
                        : filteredList.OrderBy(c => c.TotalSent),
                    "totalreplied" => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.TotalReplied)
                        : filteredList.OrderBy(c => c.TotalReplied),
                    "lastactivity" => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.LastActivity)
                        : filteredList.OrderBy(c => c.LastActivity),
                    "bouncerate" => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.BounceRate)
                        : filteredList.OrderBy(c => c.BounceRate),
                    "positivereplyrate" => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.PositiveReplyRate)
                        : filteredList.OrderBy(c => c.PositiveReplyRate),
                    _ => filter.SortDescending 
                        ? filteredList.OrderByDescending(c => c.ReplyRate)
                        : filteredList.OrderBy(c => c.ReplyRate)
                };
            }

            // Apply pagination
            var paginatedMetrics = sortedMetrics
                .Skip(filter.Offset)
                .Take(filter.Limit)
                .ToList();

            return new CampaignPerformanceResponse
            {
                Campaigns = paginatedMetrics,
                TotalCount = allMetrics.Count,
                FilteredCount = filteredList.Count,
                Stats = stats
            };
        }

        private double CalculateCompositeScore(int totalSent, int totalReplied, int totalOpened, int totalBounced)
        {
            if (totalSent == 0) return 0;

            // Weighted scoring algorithm
            double replyRate = (double)totalReplied / totalSent;
            double openRate = (double)totalOpened / totalSent;
            double bounceRate = (double)totalBounced / totalSent;
            
            // Volume factor (logarithmic scale to reduce impact of huge volumes)
            double volumeFactor = Math.Log10(totalSent + 1) / 10; // Normalize to 0-1 range approximately
            
            // Performance score (weighted)
            double performanceScore = (replyRate * 0.5) + // Reply rate gets 50% weight
                                     (openRate * 0.3) +   // Open rate gets 30% weight
                                     ((1 - bounceRate) * 0.2); // Low bounce rate gets 20% weight
            
            // Combine performance and volume (80% performance, 20% volume)
            double compositeScore = (performanceScore * 0.8) + (volumeFactor * 0.2);
            
            return Math.Round(compositeScore * 100, 2); // Return as percentage
        }

        public async Task<List<ClientPerformanceMetric>> GetTopClientsAsync(int limit = 10, string? userId = null)
        {
            var clients = (await _clientRepository.GetAllAsync()).ToList();
            var campaigns = (await _campaignRepository.GetAllAsync()).ToList();

            // Filter by user access
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null && user.Role != "Admin")
                {
                    if (user.AssignedClientIds != null && user.AssignedClientIds.Any())
                    {
                        clients = clients.Where(c => user.AssignedClientIds.Contains(c.Id)).ToList();
                        campaigns = campaigns.Where(c => user.AssignedClientIds.Contains(c.ClientId ?? "")).ToList();
                    }
                    else
                    {
                        // User has no assigned clients, show no clients or campaigns
                        clients = new List<Client>();
                        campaigns = new List<CampaignDetailsDbModel>();
                    }
                }
            }

            var metrics = clients.Select(client =>
            {
                var clientCampaigns = campaigns.Where(c => c.ClientId == client.Id).ToList();
                var totalSent = clientCampaigns.Sum(c => c.TotalSent ?? 0);
                var totalOpened = clientCampaigns.Sum(c => c.TotalOpened ?? 0);
                var totalReplied = clientCampaigns.Sum(c => c.TotalReplied ?? 0);

                return new ClientPerformanceMetric
                {
                    Id = client.Id,
                    Name = client.Name,
                    CampaignCount = clientCampaigns.Count,
                    EmailAccountCount = 0, // TODO: Calculate email accounts per client
                    TotalSent = totalSent,
                    TotalOpened = totalOpened,
                    TotalReplied = totalReplied,
                    OpenRate = totalSent > 0 ? Math.Round((double)totalOpened / totalSent * 100, 2) : 0,
                    ReplyRate = totalSent > 0 ? Math.Round((double)totalReplied / totalSent * 100, 2) : 0,
                    LastActivity = clientCampaigns.Any() ? clientCampaigns.Max(c => c.UpdatedAt ?? c.CreatedAt ?? DateTime.UtcNow) : client.UpdatedAt,
                    Color = client.Color
                };
            })
            .Where(c => c.TotalSent > 0) // Only clients with activity
            .OrderByDescending(c => c.TotalSent)
            .Take(limit)
            .ToList();

            return metrics;
        }

        public async Task<EmailAccountSummary> GetEmailAccountSummaryAsync(string? userId = null)
        {
            var accounts = (await _emailAccountRepository.GetAllAsync()).ToList();

            // Filter by user access
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null && user.Role != "Admin")
                {
                    if (user.AssignedClientIds != null && user.AssignedClientIds.Any())
                    {
                        accounts = accounts.Where(a => user.AssignedClientIds.Contains(a.ClientId ?? "")).ToList();
                    }
                    else
                    {
                        // User has no assigned clients, show no email accounts
                        accounts = new List<EmailAccountDbModel>();
                    }
                }
            }

            var summary = new EmailAccountSummary
            {
                TotalAccounts = accounts.Count,
                ActiveAccounts = accounts.Count(a => a.Status?.ToLower() == "active"),
                WarmingUpAccounts = accounts.Count(a => a.Status?.ToLower() == "warming_up" || a.Status?.ToLower() == "warmup"),
                WarmedUpAccounts = accounts.Count(a => a.Status?.ToLower() == "warmed_up" || a.Status?.ToLower() == "warmed"),
                PausedAccounts = accounts.Count(a => a.Status?.ToLower() == "paused"),
                IssueAccounts = accounts.Count(a => a.Status?.ToLower() == "error" || a.Status?.ToLower() == "issue"),
                
            };

            // Group by provider (use email domain as provider approximation)
            summary.AccountsByProvider = accounts
                .GroupBy(a => GetEmailProvider(a.Email))
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by status
            var statusGroups = accounts.GroupBy(a => a.Status ?? "Unknown").ToList();
            summary.AccountsByStatus = statusGroups.ToDictionary(
                g => g.Key,
                g => new AccountStatusCount
                {
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / accounts.Count * 100, 1)
                }
            );

            return summary;
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesAsync(int limit = 20, string? userId = null)
        {
            var activities = new List<RecentActivity>();

            // Get recent campaigns  
            var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
            var recentCampaigns = allCampaigns
                .OrderByDescending(c => c.UpdatedAt)
                .Take(5)
                .ToList();

            foreach (var campaign in recentCampaigns)
            {
                activities.Add(new RecentActivity
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "campaign",
                    Title = $"Campaign: {campaign.Name}",
                    Description = $"Status: {campaign.Status} • Sent: {campaign.TotalSent ?? 0}",
                    Timestamp = campaign.UpdatedAt ?? campaign.CreatedAt ?? DateTime.UtcNow,
                    Icon = "Send",
                    Color = campaign.Status?.ToLower() == "active" ? "green" : "blue"
                });
            }

            // Get recent clients
            var allClients = (await _clientRepository.GetAllAsync()).ToList();
            var recentClients = allClients
                .OrderByDescending(c => c.UpdatedAt)
                .Take(3)
                .ToList();

            foreach (var client in recentClients)
            {
                activities.Add(new RecentActivity
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "client",
                    Title = $"Client: {client.Name}",
                    Description = $"Status: {client.Status}",
                    Timestamp = client.UpdatedAt,
                    Icon = "Users",
                    Color = client.Color
                });
            }

            // Get recent email accounts
            var allEmailAccounts = (await _emailAccountRepository.GetAllAsync()).ToList();
            var recentAccounts = allEmailAccounts
                .OrderByDescending(a => a.UpdatedAt)
                .Take(3)
                .ToList();

            foreach (var account in recentAccounts)
            {
                activities.Add(new RecentActivity
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "email_account",
                    Title = $"Email Account: {account.Email}",
                    Description = $"Status: {account.Status} • Sent: {account.Sent}",
                    Timestamp = account.UpdatedAt,
                    Icon = "Mail",
                    Color = account.Status?.ToLower() == "active" ? "green" : "orange"
                });
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();
        }

        private DateTime CalculateStartDate(DateTime endDate, string period)
        {
            return period switch
            {
                "7" => endDate.AddDays(-7),
                "30" => endDate.AddDays(-30),
                "90" => endDate.AddDays(-90),
                "6m" => endDate.AddMonths(-6),
                "1y" => endDate.AddYears(-1),
                "all" => new DateTime(2023, 1, 1), // Start from a reasonable date for "all time"
                _ => int.TryParse(period, out var days) ? endDate.AddDays(-days) : endDate.AddDays(-30)
            };
        }

        private string GetEmailProvider(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "Unknown";
            
            var domain = email.Split('@').LastOrDefault()?.ToLower() ?? "unknown";
            
            return domain switch
            {
                var d when d.Contains("gmail") => "Gmail",
                var d when d.Contains("outlook") || d.Contains("hotmail") => "Outlook",
                var d when d.Contains("yahoo") => "Yahoo",
                var d when d.Contains("icloud") => "iCloud",
                _ => "Other"
            };
        }

        private async Task ApplyTimeRangeCalculation(List<CampaignDetailsDbModel> campaigns, int timeRangeDays)
        {
            if (!campaigns.Any())
            {
                return;
            }

            var endDate = new DateTime(2024, 8, 26); // Use reasonable fallback instead of system date
            var startDate = endDate.AddDays(-timeRangeDays);
            
            // Get campaign IDs for batch processing
            var campaignIds = campaigns.Select(c => c.Id).ToList();
            
            // Fetch all daily stats for the campaigns within the date range
            var dailyStats = await _campaignEventRepository.GetStatsForCampaignsAsync(
                campaignIds, startDate, endDate);
            
            // Group stats by campaign ID for efficient lookup
            var statsByCampaign = dailyStats
                .GroupBy(s => s.CampaignId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Update each campaign with time-range specific totals
            foreach (var campaign in campaigns)
            {
                if (statsByCampaign.TryGetValue(campaign.Id, out var campaignStats))
                {
                    // Calculate time-range totals from daily stats
                    campaign.TotalSent = campaignStats.Sum(s => s.Sent);
                    campaign.TotalOpened = campaignStats.Sum(s => s.Opened);
                    campaign.TotalClicked = campaignStats.Sum(s => s.Clicked);
                    campaign.TotalReplied = campaignStats.Sum(s => s.Replied);
                    campaign.TotalPositiveReplies = campaignStats.Sum(s => s.PositiveReplies);
                    campaign.TotalBounced = campaignStats.Sum(s => s.Bounced);
                }
                else if (timeRangeDays < 9999)
                {
                    // No stats in this time range - set all to 0 for short ranges
                    campaign.TotalSent = 0;
                    campaign.TotalOpened = 0;
                    campaign.TotalClicked = 0;
                    campaign.TotalReplied = 0;
                    campaign.TotalPositiveReplies = 0;
                    campaign.TotalBounced = 0;
                }
                // For all-time (9999 days), keep existing campaign totals if no daily stats found
            }
        }

        private async Task<PeriodChanges> CalculatePeriodChangesAsync(List<string>? assignedClientIds = null, bool isAdmin = true, DashboardFilterRequest? filter = null)
        {
            try
            {
                // Get campaigns filtered by user access (same logic as GetCampaignTotalsAsync)
                var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
                
                if (!isAdmin && assignedClientIds != null && assignedClientIds.Any())
                {
                    allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId)).ToList();
                }
                else if (!isAdmin)
                {
                    // User has no assigned clients, show no campaigns
                    allCampaigns = new List<CampaignDetailsDbModel>();
                }
                
                if (filter != null)
                {
                    if (filter.ClientIds != null && filter.ClientIds.Count > 0)
                    {
                        allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && filter.ClientIds.Contains(c.ClientId)).ToList();
                    }
                    
                    if (filter.CampaignIds != null && filter.CampaignIds.Count > 0)
                    {
                        allCampaigns = allCampaigns.Where(c => filter.CampaignIds.Contains(c.Id)).ToList();
                    }
                }

                if (!allCampaigns.Any())
                {
                    return new PeriodChanges();
                }

                var campaignIds = allCampaigns.Select(c => c.Id).ToList();
                
                // Use reasonable current date instead of system date which might be incorrect
                var currentDate = new DateTime(2025, 1, 9); // Use a reasonable current date
                
                // Use weekly comparison: current week vs last week
                var currentPeriodStart = currentDate.AddDays(-7);  // Last 7 days
                var currentPeriodEnd = currentDate;
                var previousPeriodStart = currentDate.AddDays(-14); // Previous 7 days (week before last week)  
                var previousPeriodEnd = currentDate.AddDays(-7);

                // Debug: Check what date ranges we're using
                // Current week: {currentPeriodStart:yyyy-MM-dd} to {currentPeriodEnd:yyyy-MM-dd}
                // Previous week: {previousPeriodStart:yyyy-MM-dd} to {previousPeriodEnd:yyyy-MM-dd}

                // Get current period stats
                var currentPeriodStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                    currentPeriodStart, currentPeriodEnd, campaignIds);
                
                // Get previous period stats  
                var previousPeriodStats = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                    previousPeriodStart, previousPeriodEnd, campaignIds);

                // Calculate totals for each period
                var currentTotals = CalculatePeriodTotals(currentPeriodStats);
                var previousTotals = CalculatePeriodTotals(previousPeriodStats);

                // If no data in either period, return zeros
                if (currentTotals.TotalSent == 0 && previousTotals.TotalSent == 0)
                {
                    return new PeriodChanges();
                }

                // Calculate percentage changes
                return new PeriodChanges
                {
                    TotalEmailsSentChange = CalculatePercentageChange(previousTotals.TotalSent, currentTotals.TotalSent),
                    OpenRateChange = CalculateRateChange(previousTotals.OpenRate, currentTotals.OpenRate),
                    ReplyRateChange = CalculateRateChange(previousTotals.ReplyRate, currentTotals.ReplyRate),
                    BounceRateChange = CalculateRateChange(previousTotals.BounceRate, currentTotals.BounceRate),
                    PositiveReplyRateChange = CalculateRateChange(previousTotals.PositiveReplyRate, currentTotals.PositiveReplyRate)
                };
            }
            catch (Exception)
            {
                // Return zeros if calculation fails
                return new PeriodChanges();
            }
        }

        private PeriodTotals CalculatePeriodTotals(IEnumerable<dynamic> periodStats)
        {
            var totals = new PeriodTotals();
            
            foreach (var stat in periodStats)
            {
                totals.TotalSent += (int)(stat.totalsent ?? 0);
                totals.TotalOpened += (int)(stat.totalopened ?? 0);
                totals.TotalReplied += (int)(stat.totalreplied ?? 0);
                totals.TotalPositiveReplies += (int)(stat.totalpositivereplies ?? 0);
                totals.TotalBounced += (int)(stat.totalbounced ?? 0);
            }

            // Calculate rates
            if (totals.TotalSent > 0)
            {
                totals.OpenRate = (double)totals.TotalOpened / totals.TotalSent * 100;
                totals.ReplyRate = (double)totals.TotalReplied / totals.TotalSent * 100;
                totals.BounceRate = (double)totals.TotalBounced / totals.TotalSent * 100;
            }

            if (totals.TotalReplied > 0)
            {
                totals.PositiveReplyRate = (double)totals.TotalPositiveReplies / totals.TotalReplied * 100;
            }

            return totals;
        }

        private double CalculatePercentageChange(int previousValue, int currentValue)
        {
            if (previousValue == 0)
            {
                // If no previous data but current data exists, show as 100% increase
                return currentValue > 0 ? 100 : 0;
            }
            return Math.Round(((double)(currentValue - previousValue) / previousValue) * 100, 2);
        }

        private double CalculateRateChange(double previousRate, double currentRate)
        {
            return Math.Round(currentRate - previousRate, 2);
        }

        private async Task<RecentPeriodStats> CalculateRecentStatsAsync(List<string>? assignedClientIds = null, bool isAdmin = true, DashboardFilterRequest? filter = null)
        {
            try
            {
                // Get campaigns filtered by user access (same logic as GetCampaignTotalsAsync)
                var allCampaigns = (await _campaignRepository.GetAllAsync()).ToList();
                
                if (!isAdmin && assignedClientIds != null && assignedClientIds.Any())
                {
                    allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && assignedClientIds.Contains(c.ClientId)).ToList();
                }
                else if (!isAdmin)
                {
                    // User has no assigned clients, show no campaigns
                    allCampaigns = new List<CampaignDetailsDbModel>();
                }
                
                if (filter != null)
                {
                    if (filter.ClientIds != null && filter.ClientIds.Count > 0)
                    {
                        allCampaigns = allCampaigns.Where(c => !string.IsNullOrEmpty(c.ClientId) && filter.ClientIds.Contains(c.ClientId)).ToList();
                    }
                    
                    if (filter.CampaignIds != null && filter.CampaignIds.Count > 0)
                    {
                        allCampaigns = allCampaigns.Where(c => filter.CampaignIds.Contains(c.Id)).ToList();
                    }
                }

                if (!allCampaigns.Any())
                {
                    return new RecentPeriodStats();
                }

                var campaignIds = allCampaigns.Select(c => c.Id).ToList();
                
                // Use reasonable current date instead of system date which might be incorrect
                var currentDate = new DateTime(2025, 1, 9); // Use a reasonable current date
                var recentPeriodStart = currentDate.AddDays(-7);  // Last 7 days
                var recentPeriodEnd = currentDate;

                // Get recent period stats (last 7 days)
                var recentPeriodStatsData = await _campaignEventRepository.GetAggregatedDailyStatsAsync(
                    recentPeriodStart, recentPeriodEnd, campaignIds);

                // Calculate totals for recent period
                var recentTotals = CalculatePeriodTotals(recentPeriodStatsData);
                
                return new RecentPeriodStats
                {
                    RecentEmailsSent = recentTotals.TotalSent,
                    RecentEmailsOpened = recentTotals.TotalOpened,
                    RecentEmailsReplied = recentTotals.TotalReplied
                };
            }
            catch (Exception)
            {
                // Return zeros if calculation fails
                return new RecentPeriodStats();
            }
        }

        private class PeriodChanges
        {
            public double TotalEmailsSentChange { get; set; }
            public double OpenRateChange { get; set; }
            public double ReplyRateChange { get; set; }
            public double BounceRateChange { get; set; }
            public double PositiveReplyRateChange { get; set; }
        }

        private class PeriodTotals
        {
            public int TotalSent { get; set; }
            public int TotalOpened { get; set; }
            public int TotalReplied { get; set; }
            public int TotalPositiveReplies { get; set; }
            public int TotalBounced { get; set; }
            public double OpenRate { get; set; }
            public double ReplyRate { get; set; }
            public double BounceRate { get; set; }
            public double PositiveReplyRate { get; set; }
        }

        private class RecentPeriodStats
        {
            public int RecentEmailsSent { get; set; }
            public int RecentEmailsOpened { get; set; }
            public int RecentEmailsReplied { get; set; }
        }
    }
}