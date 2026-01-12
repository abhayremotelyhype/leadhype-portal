using Dapper;
using LeadHype.Api.Core.Database;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using Microsoft.Extensions.Logging;
using System.Data;

namespace LeadHype.Api.Core.Services.Analytics;

public interface IUserStatsService
{
    Task<UserStatsCollectionResponse> GetAllUserStatsAsync(int page = 1, int pageSize = 20, string sortBy = "username", bool sortDescending = false, string? roleFilter = null, DateTime? startDate = null, DateTime? endDate = null, string? statusFilter = null, string? searchQuery = null);
    Task<UserStatsResponse?> GetUserStatsAsync(string userId);
}

public class UserStatsService : IUserStatsService
{
    private readonly IUserRepository _userRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignDailyStatEntryRepository _dailyStatsRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IDbConnectionService _connectionService;
    private readonly ILogger<UserStatsService> _logger;

    public UserStatsService(
        IUserRepository userRepository,
        IClientRepository clientRepository,
        ICampaignRepository campaignRepository,
        ICampaignDailyStatEntryRepository dailyStatsRepository,
        IEmailAccountRepository emailAccountRepository,
        IDbConnectionService connectionService,
        ILogger<UserStatsService> logger)
    {
        _userRepository = userRepository;
        _clientRepository = clientRepository;
        _campaignRepository = campaignRepository;
        _dailyStatsRepository = dailyStatsRepository;
        _emailAccountRepository = emailAccountRepository;
        _connectionService = connectionService;
        _logger = logger;
    }

    public async Task<UserStatsCollectionResponse> GetAllUserStatsAsync(int page = 1, int pageSize = 20, string sortBy = "username", bool sortDescending = false, string? roleFilter = null, DateTime? startDate = null, DateTime? endDate = null, string? statusFilter = null, string? searchQuery = null)
    {
        try
        {
            // Ensure valid pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100); // Max 100 items per page

            var allUsers = await _userRepository.GetAllAsync();
            var allClients = await _clientRepository.GetAllAsync();
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var allEmailAccounts = await _emailAccountRepository.GetAllAsync();

            // Apply filters
            var filteredUsers = allUsers.AsQueryable();

            // Role filter
            if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
            {
                filteredUsers = filteredUsers.Where(u => u.Role == roleFilter);
            }

            // Status filter
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                if (statusFilter == "Active")
                {
                    filteredUsers = filteredUsers.Where(u => u.IsActive);
                }
                else if (statusFilter == "Inactive")
                {
                    filteredUsers = filteredUsers.Where(u => !u.IsActive);
                }
            }

            // Search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchLower = searchQuery.ToLower();
                filteredUsers = filteredUsers.Where(u => 
                    u.Username.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
            }

            var users = filteredUsers.ToList();

            // Build user statistics for ALL users first (needed for engagement sorting)
            var userStats = new List<UserStatsResponse>();
            
            using var connection = await _connectionService.GetConnectionAsync();

            foreach (var user in users)
            {
                var userInfo = new LeadHype.Api.Core.Models.API.Responses.UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    HasApiKey = !string.IsNullOrEmpty(user.ApiKey),
                    ApiKeyCreatedAt = user.ApiKeyCreatedAt
                };

                // Calculate assigned clients and campaigns
                var assignedClientIds = user.Role == UserRoles.Admin 
                    ? allClients.Select(c => c.Id).ToList()
                    : user.AssignedClientIds ?? new List<string>();

                userInfo.AssignedClientCount = assignedClientIds.Count;

                // Get campaigns for assigned clients
                var accessibleCampaigns = allCampaigns.Where(c => assignedClientIds.Contains(c.ClientId)).ToList();
                userInfo.AccessibleCampaignCount = accessibleCampaigns.Count;
                userInfo.ActiveCampaignCount = accessibleCampaigns.Count(c => c.Status?.ToUpperInvariant() == "ACTIVE");

                // Get email accounts for assigned clients
                userInfo.AccessibleEmailAccountCount = allEmailAccounts.Count(ea => assignedClientIds.Contains(ea.ClientId));

                // Get engagement statistics for user's campaigns
                var accessibleCampaignIds = accessibleCampaigns.Select(c => c.Id).ToList();
                var engagementStats = await GetUserEngagementStatsAsync(connection, accessibleCampaignIds, startDate, endDate);
                var replyTiming = await GetUserReplyTimingAsync(connection, accessibleCampaignIds);

                userStats.Add(new UserStatsResponse
                {
                    User = userInfo,
                    Stats = engagementStats,
                    Timing = replyTiming
                });
            }

            // Apply sorting to userStats
            userStats = sortBy.ToLower() switch
            {
                "username" => sortDescending ? userStats.OrderByDescending(u => u.User.Username).ToList() : userStats.OrderBy(u => u.User.Username).ToList(),
                "email" => sortDescending ? userStats.OrderByDescending(u => u.User.Email).ToList() : userStats.OrderBy(u => u.User.Email).ToList(),
                "role" => sortDescending ? userStats.OrderByDescending(u => u.User.Role).ToList() : userStats.OrderBy(u => u.User.Role).ToList(),
                "createdat" => sortDescending ? userStats.OrderByDescending(u => u.User.CreatedAt).ToList() : userStats.OrderBy(u => u.User.CreatedAt).ToList(),
                "lastloginat" => sortDescending ? userStats.OrderByDescending(u => u.User.LastLoginAt ?? DateTime.MinValue).ToList() : userStats.OrderBy(u => u.User.LastLoginAt ?? DateTime.MinValue).ToList(),
                "lastreplyat" => sortDescending ? userStats.OrderByDescending(u => u.Timing.LastReplyAt ?? DateTime.MinValue).ToList() : userStats.OrderBy(u => u.Timing.LastReplyAt ?? DateTime.MinValue).ToList(),
                // Engagement metrics sorting
                "totalsent" => sortDescending ? userStats.OrderByDescending(u => u.Stats.TotalSent).ToList() : userStats.OrderBy(u => u.Stats.TotalSent).ToList(),
                "totalreplies" => sortDescending ? userStats.OrderByDescending(u => u.Stats.TotalReplies).ToList() : userStats.OrderBy(u => u.Stats.TotalReplies).ToList(),
                "positivereplies" => sortDescending ? userStats.OrderByDescending(u => u.Stats.PositiveReplies).ToList() : userStats.OrderBy(u => u.Stats.PositiveReplies).ToList(),
                "emailsperreply" => sortDescending ? userStats.OrderByDescending(u => u.Stats.EmailsPerReply).ToList() : userStats.OrderBy(u => u.Stats.EmailsPerReply).ToList(),
                "emailsperpositivereply" => sortDescending ? userStats.OrderByDescending(u => u.Stats.EmailsPerPositiveReply).ToList() : userStats.OrderBy(u => u.Stats.EmailsPerPositiveReply).ToList(),
                "positivereplypercentage" => sortDescending ? userStats.OrderByDescending(u => u.Stats.PositiveReplyPercentage).ToList() : userStats.OrderBy(u => u.Stats.PositiveReplyPercentage).ToList(),
                // Resource metrics sorting
                "assignedclientcount" => sortDescending ? userStats.OrderByDescending(u => u.User.AssignedClientCount).ToList() : userStats.OrderBy(u => u.User.AssignedClientCount).ToList(),
                "accessiblecampaigncount" => sortDescending ? userStats.OrderByDescending(u => u.User.AccessibleCampaignCount).ToList() : userStats.OrderBy(u => u.User.AccessibleCampaignCount).ToList(),
                "activecampaigncount" => sortDescending ? userStats.OrderByDescending(u => u.User.ActiveCampaignCount).ToList() : userStats.OrderBy(u => u.User.ActiveCampaignCount).ToList(),
                "accessibleemailaccountcount" => sortDescending ? userStats.OrderByDescending(u => u.User.AccessibleEmailAccountCount).ToList() : userStats.OrderBy(u => u.User.AccessibleEmailAccountCount).ToList(),
                _ => sortDescending ? userStats.OrderByDescending(u => u.User.Username).ToList() : userStats.OrderBy(u => u.User.Username).ToList()
            };

            // Calculate pagination after sorting
            var totalCount = userStats.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var paginatedUserStats = userStats
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Calculate aggregated statistics for all users (not just current page)
            var aggregatedStats = await GetAggregatedUserStatsAsync(connection, allUsers.ToList(), allClients.ToList(), allCampaigns.ToList(), startDate, endDate);
            var aggregatedTiming = await GetAggregatedUserTimingAsync(connection, allUsers.ToList(), allClients.ToList(), allCampaigns.ToList());

            // Calculate user status summary
            var userStatusSummary = new UserStatusSummary
            {
                TotalUsers = allUsers.Count(),
                ActiveUsers = allUsers.Count(u => u.IsActive),
                InactiveUsers = allUsers.Count(u => !u.IsActive),
                AdminUsers = allUsers.Count(u => u.Role == UserRoles.Admin),
                RegularUsers = allUsers.Count(u => u.Role == UserRoles.User),
                UsersWithApiKeys = allUsers.Count(u => !string.IsNullOrEmpty(u.ApiKey)),
                UsersLoggedInLast30Days = allUsers.Count(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value > DateTime.UtcNow.AddDays(-30))
            };

            var response = new UserStatsCollectionResponse
            {
                Users = paginatedUserStats,
                TotalCount = totalCount,
                GeneratedAt = DateTime.UtcNow,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                },
                AggregatedStats = aggregatedStats,
                AggregatedTiming = aggregatedTiming,
                UserStatusSummary = userStatusSummary
            };

            _logger.LogInformation("Successfully retrieved {UserCount} user statistics (page {Page} of {TotalPages})",
                paginatedUserStats.Count, page, totalPages);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user statistics");
            throw;
        }
    }

    public async Task<UserStatsResponse?> GetUserStatsAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var allClients = await _clientRepository.GetAllAsync();
            var allCampaigns = await _campaignRepository.GetAllAsync();
            var allEmailAccounts = await _emailAccountRepository.GetAllAsync();

            var userInfo = new LeadHype.Api.Core.Models.API.Responses.UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                HasApiKey = !string.IsNullOrEmpty(user.ApiKey),
                ApiKeyCreatedAt = user.ApiKeyCreatedAt
            };

            // Calculate assigned clients and campaigns
            var assignedClientIds = user.Role == UserRoles.Admin 
                ? allClients.Select(c => c.Id).ToList()
                : user.AssignedClientIds ?? new List<string>();

            userInfo.AssignedClientCount = assignedClientIds.Count;

            // Get campaigns for assigned clients
            var accessibleCampaigns = allCampaigns.Where(c => assignedClientIds.Contains(c.ClientId)).ToList();
            userInfo.AccessibleCampaignCount = accessibleCampaigns.Count;
            userInfo.ActiveCampaignCount = accessibleCampaigns.Count(c => c.Status?.ToUpperInvariant() == "ACTIVE");

            // Get email accounts for assigned clients
            userInfo.AccessibleEmailAccountCount = allEmailAccounts.Count(ea => assignedClientIds.Contains(ea.ClientId));

            using var connection = await _connectionService.GetConnectionAsync();

            // Get engagement statistics for user's campaigns
            var accessibleCampaignIds = accessibleCampaigns.Select(c => c.Id).ToList();
            var engagementStats = await GetUserEngagementStatsAsync(connection, accessibleCampaignIds, null, null);
            var replyTiming = await GetUserReplyTimingAsync(connection, accessibleCampaignIds);

            return new UserStatsResponse
            {
                User = userInfo,
                Stats = engagementStats,
                Timing = replyTiming
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user statistics for user {UserId}", userId);
            throw;
        }
    }

    private async Task<UserEngagementStats> GetUserEngagementStatsAsync(IDbConnection connection, List<string> campaignIds, DateTime? startDate, DateTime? endDate)
    {
        if (!campaignIds.Any())
        {
            return new UserEngagementStats();
        }

        int totalSent, totalReplies, positiveReplies;

        // Use repository method if no date filters (most efficient)
        if (!startDate.HasValue && !endDate.HasValue)
        {
            var (sent, replies, positive, _, _) = 
                await _dailyStatsRepository.GetAggregatedTotalsForCampaignsAsync(campaignIds);
            
            totalSent = sent;
            totalReplies = replies;
            positiveReplies = positive;
        }
        else
        {
            // Use custom query with date filters
            var dateFilter = "";
            var parameters = new DynamicParameters();
            parameters.Add("@CampaignIds", campaignIds.ToArray());

            if (startDate.HasValue)
            {
                dateFilter += " AND event_date >= @StartDate";
                parameters.Add("@StartDate", startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                dateFilter += " AND event_date <= @EndDate";
                parameters.Add("@EndDate", endDate.Value.Date);
            }

            var query = $@"
                SELECT 
                    COALESCE(SUM(sent), 0) AS TotalSent,
                    COALESCE(SUM(replied), 0) AS TotalReplies,
                    COALESCE(SUM(positive_replies), 0) AS PositiveReplies
                FROM campaign_daily_stat_entries
                WHERE campaign_id = ANY(@CampaignIds){dateFilter}";

            var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(query, parameters);

            totalSent = (int)(stats?.TotalSent ?? 0);
            totalReplies = (int)(stats?.TotalReplies ?? 0);
            positiveReplies = (int)(stats?.PositiveReplies ?? 0);
        }

        var emailsPerReply = totalReplies > 0 ? (double)totalSent / totalReplies : 0;
        var emailsPerPositiveReply = positiveReplies > 0 ? (double)totalSent / positiveReplies : 0;
        var repliesPerPositiveReply = positiveReplies > 0 ? (double)totalReplies / positiveReplies : 0;
        var positiveReplyPercentage = totalReplies > 0 ? (double)positiveReplies / totalReplies * 100 : 0;
        var replyRate = totalSent > 0 ? (double)totalReplies / totalSent * 100 : 0;

        return new UserEngagementStats
        {
            TotalSent = totalSent,
            TotalReplies = totalReplies,
            PositiveReplies = positiveReplies,
            EmailsPerReply = Math.Round(emailsPerReply, 2),
            EmailsPerPositiveReply = Math.Round(emailsPerPositiveReply, 2),
            RepliesPerPositiveReply = Math.Round(repliesPerPositiveReply, 2),
            PositiveReplyPercentage = Math.Round(positiveReplyPercentage, 2),
            ReplyRate = Math.Round(replyRate, 2)
        };
    }

    private async Task<UserReplyTiming> GetUserReplyTimingAsync(IDbConnection connection, List<string> campaignIds)
    {
        if (!campaignIds.Any())
        {
            return new UserReplyTiming();
        }

        // Use repository method for consistency
        var (_, _, _, lastReplyAt, lastPositiveReplyAt) = 
            await _dailyStatsRepository.GetAggregatedTotalsForCampaignsAsync(campaignIds);

        return new UserReplyTiming
        {
            LastReplyAt = lastReplyAt,
            LastPositiveReplyAt = lastPositiveReplyAt,
            LastReplyRelative = lastReplyAt.HasValue ? GetRelativeTime(lastReplyAt.Value) : null,
            LastPositiveReplyRelative = lastPositiveReplyAt.HasValue ? GetRelativeTime(lastPositiveReplyAt.Value) : null
        };
    }

    private async Task<UserEngagementStats> GetAggregatedUserStatsAsync(IDbConnection connection, List<User> allUsers, List<Client> allClients, List<CampaignDetailsDbModel> allCampaigns, DateTime? startDate, DateTime? endDate)
    {
        var allCampaignIds = allCampaigns.Select(c => c.Id).ToList();
        return await GetUserEngagementStatsAsync(connection, allCampaignIds, startDate, endDate);
    }

    private async Task<UserReplyTiming> GetAggregatedUserTimingAsync(IDbConnection connection, List<User> allUsers, List<Client> allClients, List<CampaignDetailsDbModel> allCampaigns)
    {
        var allCampaignIds = allCampaigns.Select(c => c.Id).ToList();
        return await GetUserReplyTimingAsync(connection, allCampaignIds);
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalDays >= 365)
        {
            var years = (int)(timeSpan.TotalDays / 365);
            return $"{years} year{(years == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalDays >= 30)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return $"{months} month{(months == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }
        return "Just now";
    }
}