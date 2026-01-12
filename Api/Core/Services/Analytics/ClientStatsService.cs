using Dapper;
using LeadHype.Api.Core.Database;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Models;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Services
{
    public interface IClientStatsService
    {
        Task<ClientStatsCollectionResponse> GetAllClientStatsAsync(int page = 1, int pageSize = 20, string sortBy = "name", bool sortDescending = false, string[]? clientIds = null, DateTime? startDate = null, DateTime? endDate = null, string? clientStatus = null, string? filterByUserId = null);
        Task<ClientStatsResponse?> GetClientStatsAsync(string clientId);
    }

    public class ClientStatsService : IClientStatsService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ICampaignRepository _campaignRepository;
        private readonly ICampaignDailyStatEntryRepository _dailyStatsRepository;
        private readonly ICampaignEventRepository _campaignEventRepository;
        private readonly IEmailAccountRepository _emailAccountRepository;
        private readonly ILeadEmailHistoryRepository _leadEmailHistoryRepository;
        private readonly IDbConnectionService _connectionService;
        private readonly ILogger<ClientStatsService> _logger;

        public ClientStatsService(
            IClientRepository clientRepository,
            ICampaignRepository campaignRepository,
            ICampaignDailyStatEntryRepository dailyStatsRepository,
            ICampaignEventRepository campaignEventRepository,
            IEmailAccountRepository emailAccountRepository,
            ILeadEmailHistoryRepository leadEmailHistoryRepository,
            IDbConnectionService connectionService,
            ILogger<ClientStatsService> logger)
        {
            _clientRepository = clientRepository;
            _campaignRepository = campaignRepository;
            _dailyStatsRepository = dailyStatsRepository;
            _campaignEventRepository = campaignEventRepository;
            _emailAccountRepository = emailAccountRepository;
            _leadEmailHistoryRepository = leadEmailHistoryRepository;
            _connectionService = connectionService;
            _logger = logger;
        }

        public async Task<ClientStatsCollectionResponse> GetAllClientStatsAsync(int page = 1, int pageSize = 20, string sortBy = "name", bool sortDescending = false, string[]? clientIds = null, DateTime? startDate = null, DateTime? endDate = null, string? clientStatus = null, string? filterByUserId = null)
        {
            try
            {
                // Ensure valid pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100); // Max 100 items per page

                var clients = await _clientRepository.GetAllAsync();
                
                // Filter clients by clientIds if provided
                if (clientIds != null && clientIds.Length > 0)
                {
                    clients = clients.Where(c => clientIds.Contains(c.Id)).ToList();
                }
                
                // Filter clients by status if provided
                if (!string.IsNullOrEmpty(clientStatus))
                {
                    clients = clients.Where(c => string.Equals(c.Status, clientStatus, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                // Filter clients by user if provided (show only clients assigned to this user)
                if (!string.IsNullOrEmpty(filterByUserId))
                {
                    _logger.LogInformation("Filtering clients by user ID: {FilterByUserId}", filterByUserId);
                    
                    // Get the user's assigned client IDs from the users table
                    var userAssignedClientIds = await GetUserAssignedClientIds(filterByUserId);
                    _logger.LogInformation("Found {ClientIdCount} assigned client IDs for user {FilterByUserId}", 
                        userAssignedClientIds.Count, filterByUserId);
                    
                    if (userAssignedClientIds.Count == 0)
                    {
                        _logger.LogWarning("User {FilterByUserId} has no assigned clients - will show empty result", filterByUserId);
                    }
                    else
                    {
                        _logger.LogInformation("User {FilterByUserId} has {ClientCount} assigned clients: {FirstFew}...", 
                            filterByUserId, userAssignedClientIds.Count, 
                            string.Join(", ", userAssignedClientIds.Take(5)));
                    }
                    
                    // Filter to only clients assigned to this user
                    var originalClientCount = clients.Count();
                    clients = clients.Where(c => userAssignedClientIds.Contains(c.Id)).ToList();
                    _logger.LogInformation("Filtered clients from {OriginalCount} to {FilteredCount} for user {FilterByUserId}", 
                        originalClientCount, clients.Count(), filterByUserId);
                }
                
                var clientStats = new List<ClientStatsResponse>();

                foreach (var client in clients)
                {
                    var stats = await CalculateClientStatsAsync(client, startDate, endDate);
                    clientStats.Add(stats);
                }

                // Apply sorting based on parameters
                clientStats = ApplySorting(clientStats, sortBy, sortDescending);

                // Calculate aggregated stats for ALL clients (efficiently)
                var (aggregatedStats, aggregatedTiming) = await CalculateAggregatedStatsAsync(clients, startDate, endDate);
                
                // Calculate client status summary
                var clientStatusSummary = await CalculateClientStatusSummaryAsync(clients);

                // Apply pagination
                var totalCount = clientStats.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var skip = (page - 1) * pageSize;
                var pagedClients = clientStats.Skip(skip).Take(pageSize).ToList();

                return new ClientStatsCollectionResponse
                {
                    Clients = pagedClients,
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
                    ClientStatusSummary = clientStatusSummary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all client stats");
                throw;
            }
        }

        public async Task<ClientStatsResponse?> GetClientStatsAsync(string clientId)
        {
            try
            {
                var client = await _clientRepository.GetByIdAsync(clientId);
                if (client == null)
                    return null;

                return await CalculateClientStatsAsync(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting client stats for client {clientId}");
                throw;
            }
        }

        private async Task<ClientStatsResponse> CalculateClientStatsAsync(Client client, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Get all campaigns for this client
            var campaigns = await _campaignRepository.GetByClientIdAsync(client.Id);

            // Get all email accounts for this client
            var emailAccounts = await _emailAccountRepository.GetByClientIdAsync(client.Id);

            var totalSent = 0;
            var totalReplies = 0;
            var positiveReplies = 0;
            DateTime? lastReplyAt = null;
            DateTime? lastPositiveReplyAt = null;
            DateTime? lastContactedAt = null;

            if (campaigns.Any())
            {
                // Get campaign_id integers (numeric IDs used in campaign_events)
                var campaignIdIntegers = campaigns.Select(c => c.CampaignId).ToList();
                var campaignIdStrings = campaignIdIntegers.Select(id => id.ToString()).ToList();

                // Efficiently get last contacted date from lead_email_history
                lastContactedAt = await _leadEmailHistoryRepository.GetLastContactedDateForCampaignsAsync(campaignIdIntegers);

                // Use campaign events repository for stats (event-sourced system)
                var (totalSentResult, totalRepliedResult, totalPositiveResult, lastReplyDate, lastPositiveDate) =
                    await _campaignEventRepository.GetAggregatedTotalsForCampaignsAsync(campaignIdStrings);

                totalSent = totalSentResult;
                totalReplies = totalRepliedResult;
                positiveReplies = totalPositiveResult;
                lastReplyAt = lastReplyDate;
                lastPositiveReplyAt = lastPositiveDate;
            }

            // Calculate metrics
            var emailsPerReply = totalReplies > 0 ? (double)totalSent / totalReplies : 0;
            var emailsPerPositiveReply = positiveReplies > 0 ? (double)totalSent / positiveReplies : 0;
            var repliesPerPositiveReply = positiveReplies > 0 ? (double)totalReplies / positiveReplies : 0;
            var positiveReplyPercentage = totalReplies > 0 ? (double)positiveReplies / totalReplies * 100 : 0;
            var replyRate = totalSent > 0 ? (double)totalReplies / totalSent * 100 : 0;

            return new ClientStatsResponse
            {
                Client = new ClientInfo
                {
                    Id = client.Id,
                    Name = client.Name,
                    Company = client.Company,
                    Color = client.Color,
                    Status = client.Status,
                    CampaignCount = campaigns.Count(),
                    ActiveCampaignCount = campaigns.Count(c => c.Status?.ToLowerInvariant() == "active"),
                    EmailAccountCount = emailAccounts.Count()
                },
                Stats = new EngagementStats
                {
                    TotalSent = totalSent,
                    TotalReplies = totalReplies,
                    PositiveReplies = positiveReplies,
                    EmailsPerReply = Math.Round(emailsPerReply, 2),
                    EmailsPerPositiveReply = Math.Round(emailsPerPositiveReply, 2),
                    RepliesPerPositiveReply = Math.Round(repliesPerPositiveReply, 2),
                    PositiveReplyPercentage = Math.Round(positiveReplyPercentage, 2),
                    ReplyRate = Math.Round(replyRate, 2)
                },
                Timing = new ReplyTiming
                {
                    LastReplyAt = lastReplyAt,
                    LastPositiveReplyAt = lastPositiveReplyAt,
                    LastContactedAt = lastContactedAt,
                    LastReplyRelative = lastReplyAt.HasValue ? GetRelativeTimeString(lastReplyAt.Value) : null,
                    LastPositiveReplyRelative = lastPositiveReplyAt.HasValue ? GetRelativeTimeString(lastPositiveReplyAt.Value) : null,
                    LastContactedRelative = lastContactedAt.HasValue ? GetRelativeTimeString(lastContactedAt.Value) : null
                }
            };
        }

        private static string GetRelativeTimeString(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";

            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";

            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";

            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

        private static List<ClientStatsResponse> ApplySorting(List<ClientStatsResponse> clientStats, string sortBy, bool sortDescending)
        {
            var normalizedSortBy = sortBy?.ToLowerInvariant() ?? "name";
            
            return normalizedSortBy switch
            {
                "name" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.Name).ToList()
                    : clientStats.OrderBy(s => s.Client.Name).ToList(),
                "company" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.Company ?? string.Empty).ToList()
                    : clientStats.OrderBy(s => s.Client.Company ?? string.Empty).ToList(),
                "status" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.Status).ToList()
                    : clientStats.OrderBy(s => s.Client.Status).ToList(),
                "totalsent" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.TotalSent).ToList()
                    : clientStats.OrderBy(s => s.Stats.TotalSent).ToList(),
                "totalreplies" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.TotalReplies).ToList()
                    : clientStats.OrderBy(s => s.Stats.TotalReplies).ToList(),
                "positivereplies" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.PositiveReplies).ToList()
                    : clientStats.OrderBy(s => s.Stats.PositiveReplies).ToList(),
                "emailsperreply" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.EmailsPerReply).ToList()
                    : clientStats.OrderBy(s => s.Stats.EmailsPerReply).ToList(),
                "emailsperpositivereply" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.EmailsPerPositiveReply).ToList()
                    : clientStats.OrderBy(s => s.Stats.EmailsPerPositiveReply).ToList(),
                "repliesperpositivereply" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.RepliesPerPositiveReply).ToList()
                    : clientStats.OrderBy(s => s.Stats.RepliesPerPositiveReply).ToList(),
                "positivereplypercentage" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Stats.PositiveReplyPercentage).ToList()
                    : clientStats.OrderBy(s => s.Stats.PositiveReplyPercentage).ToList(),
                "lastreply" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Timing.LastReplyAt ?? DateTime.MinValue).ThenBy(s => s.Client.Name).ToList()
                    : clientStats.OrderBy(s => s.Timing.LastReplyAt ?? DateTime.MaxValue).ThenBy(s => s.Client.Name).ToList(),
                "lastpositivereply" => sortDescending
                    ? clientStats.OrderByDescending(s => s.Timing.LastPositiveReplyAt ?? DateTime.MinValue).ThenBy(s => s.Client.Name).ToList()
                    : clientStats.OrderBy(s => s.Timing.LastPositiveReplyAt ?? DateTime.MaxValue).ThenBy(s => s.Client.Name).ToList(),
                "lastcontacted" => sortDescending
                    ? clientStats.OrderByDescending(s => s.Timing.LastContactedAt ?? DateTime.MinValue).ThenBy(s => s.Client.Name).ToList()
                    : clientStats.OrderBy(s => s.Timing.LastContactedAt ?? DateTime.MaxValue).ThenBy(s => s.Client.Name).ToList(),
                "campaigncount" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.CampaignCount).ToList()
                    : clientStats.OrderBy(s => s.Client.CampaignCount).ToList(),
                "emailaccountcount" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.EmailAccountCount).ToList()
                    : clientStats.OrderBy(s => s.Client.EmailAccountCount).ToList(),
                "activecampaigncount" => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.ActiveCampaignCount).ToList()
                    : clientStats.OrderBy(s => s.Client.ActiveCampaignCount).ToList(),
                _ => sortDescending 
                    ? clientStats.OrderByDescending(s => s.Client.Name).ToList()
                    : clientStats.OrderBy(s => s.Client.Name).ToList()
            };
        }

        /// <summary>
        /// Efficiently calculate aggregated stats for all clients using database queries
        /// instead of calculating individual client stats and then aggregating
        /// </summary>
        private async Task<(EngagementStats aggregatedStats, ReplyTiming aggregatedTiming)> CalculateAggregatedStatsAsync(
            IEnumerable<Client> clients, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var clientIds = clients.Select(c => c.Id).ToList();
                if (!clientIds.Any())
                {
                    return (new EngagementStats(), new ReplyTiming());
                }

                // Get all campaigns for these clients
                var allCampaigns = new List<CampaignDetailsDbModel>();
                foreach (var clientId in clientIds)
                {
                    var campaigns = await _campaignRepository.GetByClientIdAsync(clientId);
                    allCampaigns.AddRange(campaigns);
                }

                // Get campaign IDs for queries
                var campaignIdIntegers = allCampaigns.Select(c => c.CampaignId).ToList();
                if (!campaignIdIntegers.Any())
                {
                    return (new EngagementStats(), new ReplyTiming());
                }
                var campaignIdStrings = campaignIdIntegers.Select(id => id.ToString()).ToList();

                // Use campaign events repository for stats (event-sourced system)
                var (totalSent, totalReplies, positiveReplies, latestReplyAt, latestPositiveReplyAt) =
                    await _campaignEventRepository.GetAggregatedTotalsForCampaignsAsync(campaignIdStrings);

                // Get last contacted date from lead email history for all campaigns
                var latestContactedAt = await _leadEmailHistoryRepository.GetLastContactedDateForCampaignsAsync(campaignIdIntegers);

                // Calculate performance metrics
                var emailsPerReply = totalReplies > 0 ? (double)totalSent / totalReplies : 0;
                var emailsPerPositiveReply = positiveReplies > 0 ? (double)totalSent / positiveReplies : 0;
                var repliesPerPositiveReply = positiveReplies > 0 ? (double)totalReplies / positiveReplies : 0;
                var positiveReplyPercentage = totalReplies > 0 ? ((double)positiveReplies / totalReplies) * 100 : 0;
                var replyRate = totalSent > 0 ? ((double)totalReplies / totalSent) * 100 : 0;

                var aggregatedStats = new EngagementStats
                {
                    TotalSent = totalSent,
                    TotalReplies = totalReplies,
                    PositiveReplies = positiveReplies,
                    EmailsPerReply = emailsPerReply,
                    EmailsPerPositiveReply = emailsPerPositiveReply,
                    RepliesPerPositiveReply = repliesPerPositiveReply,
                    PositiveReplyPercentage = positiveReplyPercentage,
                    ReplyRate = replyRate
                };

                var aggregatedTiming = new ReplyTiming
                {
                    LastReplyAt = latestReplyAt,
                    LastPositiveReplyAt = latestPositiveReplyAt,
                    LastContactedAt = latestContactedAt,
                    LastReplyRelative = latestReplyAt.HasValue ? GetRelativeTime(latestReplyAt.Value) : null,
                    LastPositiveReplyRelative = latestPositiveReplyAt.HasValue ? GetRelativeTime(latestPositiveReplyAt.Value) : null,
                    LastContactedRelative = latestContactedAt.HasValue ? GetRelativeTime(latestContactedAt.Value) : null
                };

                return (aggregatedStats, aggregatedTiming);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating aggregated stats");
                return (new EngagementStats(), new ReplyTiming());
            }
        }

        /// <summary>
        /// Helper method to get relative time string (e.g., "3 days ago")
        /// </summary>
        private static string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            return timeSpan.TotalDays switch
            {
                < 1 => $"{(int)timeSpan.TotalHours}h ago",
                < 7 => $"{(int)timeSpan.TotalDays}d ago",
                < 30 => $"{(int)(timeSpan.TotalDays / 7)}w ago",
                < 365 => $"{(int)(timeSpan.TotalDays / 30)}mo ago",
                _ => $"{(int)(timeSpan.TotalDays / 365)}y ago"
            };
        }

        /// <summary>
        /// Calculate client status summary showing active vs inactive client counts and campaign totals
        /// </summary>
        private async Task<ClientStatusSummary> CalculateClientStatusSummaryAsync(IEnumerable<Client> clients)
        {
            var clientList = clients.ToList();
            var activeClients = clientList.Count(c => c.Status?.ToLowerInvariant() == "active");
            var totalClients = clientList.Count;
            var inactiveClients = totalClients - activeClients;

            // Get all campaigns for these clients to calculate campaign totals
            var allCampaigns = new List<CampaignDetailsDbModel>();
            foreach (var client in clientList)
            {
                var campaigns = await _campaignRepository.GetByClientIdAsync(client.Id);
                allCampaigns.AddRange(campaigns);
            }

            var totalCampaigns = allCampaigns.Count;
            var activeCampaigns = allCampaigns.Count(c => c.Status?.ToLowerInvariant() == "active");

            return new ClientStatusSummary
            {
                ActiveClients = activeClients,
                InactiveClients = inactiveClients,
                TotalClients = totalClients,
                TotalCampaigns = totalCampaigns,
                ActiveCampaigns = activeCampaigns
            };
        }

        private async Task<HashSet<string>> GetUserAssignedClientIds(string userId)
        {
            using var connection = await _connectionService.GetConnectionAsync();
            
            // Get the assigned_client_ids JSONB array from the users table
            var assignedClientIds = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT assigned_client_ids::text FROM users WHERE id = @UserId",
                new { UserId = userId });
            
            if (string.IsNullOrEmpty(assignedClientIds) || assignedClientIds == "[]")
            {
                return new HashSet<string>();
            }
            
            try
            {
                // Parse the JSON array and extract client IDs
                var clientIdArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(assignedClientIds);
                return new HashSet<string>(clientIdArray ?? Array.Empty<string>());
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse assigned_client_ids JSON for user {UserId}: {Json}", userId, assignedClientIds);
                return new HashSet<string>();
            }
        }
    }
}