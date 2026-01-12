using System.Data;
using System.Globalization;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Models;
using LeadHype.Api.Services;
using LeadHype.Api.Core.Database;
using Dapper;
using LeadHype.Api.Core.Models.API.Smartlead;
using LeadHype.Api.Core.Models.ExternalApi;
using LeadHype.Api.Core.Services.ExternalApi;
using Newtonsoft.Json;

namespace LeadHype.Api;

/// <summary>
/// Service responsible for synchronizing data from Smartlead API to the local database
/// </summary>
public class SmartleadSyncService
{
    #region Constructor and Dependencies

    public SmartleadSyncService(
        IEmailAccountRepository emailAccountRepository,
        ICampaignRepository campaignRepository,
        ISettingsRepository settingsRepository,
        ILogger<SmartleadSyncService> logger,
        IEmailAccountDailyStatEntryService dailyStatsService,
        IEmailAccountDailyStatEntryRepository dailyStatEntryRepository,
        IEmailAccountStatsDateRepository emailAccountStatsDateRepository,
        ICampaignEventRepository campaignEventRepository,
        IDbConnectionService connectionService,
        IEmailTemplateRepository emailTemplateRepository,
        ILeadConversationRepository leadConversationRepository,
        ILeadEmailHistoryRepository leadEmailHistoryRepository,
        IRevReplyClassificationService revReplyClassificationService,
        IClassifiedEmailRepository classifiedEmailRepository)
    {
        _apiService = new SmartleadApiService("c38e38e8-d7b6-4c00-836d-3437678ef4d9_dtw3zft");
        _emailAccountRepository = emailAccountRepository;
        _campaignRepository = campaignRepository;
        _settingsRepository = settingsRepository;
        _logger = logger;
        _dailyStatsService = dailyStatsService;
        _dailyStatEntryRepository = dailyStatEntryRepository;
        _emailAccountStatsDateRepository = emailAccountStatsDateRepository;
        _campaignEventRepository = campaignEventRepository;
        _connectionService = connectionService;
        _emailTemplateRepository = emailTemplateRepository;
        _leadConversationRepository = leadConversationRepository;
        _leadEmailHistoryRepository = leadEmailHistoryRepository;
        _revReplyClassificationService = revReplyClassificationService;
        _classifiedEmailRepository = classifiedEmailRepository;
    }

    #endregion

    #region Private Fields

    private readonly SmartleadApiService _apiService;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<SmartleadSyncService> _logger;
    private readonly IEmailAccountDailyStatEntryService _dailyStatsService;
    private readonly IEmailAccountDailyStatEntryRepository _dailyStatEntryRepository;
    private readonly ICampaignEventRepository _campaignEventRepository;
    private readonly IEmailAccountStatsDateRepository _emailAccountStatsDateRepository;
    private readonly IDbConnectionService _connectionService;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ILeadConversationRepository _leadConversationRepository;
    private readonly ILeadEmailHistoryRepository _leadEmailHistoryRepository;
    private readonly IRevReplyClassificationService _revReplyClassificationService;
    private readonly IClassifiedEmailRepository _classifiedEmailRepository;

    #endregion

    #region Public Methods

    public async Task Fetch()
    {
        while (true)
        {
            try
            {
                _logger.LogInformation("Starting Smartlead sync cycle");

                await FetchEmailAccounts();
                await FetchEmailAccountsFromCampaigns();
                await UpdateCampaignCounts();
                await FetchCampaignTemplates();
                await FetchCampaignLeads();
                await AnalyzeLeadConversationsWithRevReply();

                _logger.LogInformation("Completed Smartlead sync cycle successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _logger.LogError(ex, "Error in Fetch");
            }

            _logger.LogInformation("Waiting 2 hours before next sync cycle");
            await Task.Delay(TimeSpan.FromHours(2));
        }
    }

    #endregion

    #region Email Account Management

    private async Task FetchEmailAccounts()
    {
        const string adminUuid = "c38e38e8-d7b6-4c00-836d-3437678ef4d9";

        try
        {
            _logger.LogInformation("Starting email accounts fetch");

            // Always fetch and update email accounts basic info (this is lightweight)
            await FetchAndUpdateEmailAccounts(adminUuid);

            // Check if we have any accounts at all
            var accountCount = await _emailAccountRepository.CountAsync();
            if (accountCount == 0)
            {
                _logger.LogWarning("No email accounts found in database after fetch, skipping stats updates");
                return;
            }

            // Always check for new dates to fetch for email stats (they're historical and don't change)
            // The email_account_stats_dates table tracks which dates we've already fetched
            await FetchEmailStatsForAccounts(adminUuid);

            // Get only accounts that need warmup stats update (every 24 hours per account)
            // This is optimized to fetch only the accounts needing updates directly from the database
            var accountsNeedingWarmupStats =
                (await _emailAccountRepository.GetAccountsNeedingWarmupUpdateAsync(adminUuid, 1440)).ToList();

            if (accountsNeedingWarmupStats.Any())
            {
                await FetchWarmupStatsForAccounts(accountsNeedingWarmupStats);
            }
            else
            {
                _logger.LogInformation("No email accounts need warmup stats update at this time");
            }

            _logger.LogInformation("Completed email accounts fetch successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchEmailAccounts");
            throw;
        }
    }

    private async Task FetchAndUpdateEmailAccounts(string adminUuid)
    {
        _logger.LogInformation("Fetching email account details from Smartlead API");

        List<EmailAccountDetails> emailAccountDetails = _apiService.FetchEmailAccountDetails();

        if (!emailAccountDetails.Any())
        {
            _logger.LogWarning("No email accounts returned from API");
            return;
        }

        // Get existing accounts from database
        List<EmailAccountDbModel> existingAccounts =
            (await _emailAccountRepository.GetByAdminUuidAsync(adminUuid)).ToList();

        int updatedCount = 0;
        int createdCount = 0;
        int skippedCount = 0;

        foreach (var accountDetail in emailAccountDetails)
        {
            // Skip accounts with invalid email data
            if (string.IsNullOrEmpty(accountDetail.FromEmail))
            {
                _logger.LogWarning($"Skipping email account {accountDetail.Id} - missing email address");
                skippedCount++;
                continue;
            }

            var existingAccount = existingAccounts.FirstOrDefault(e => e.Id == accountDetail.Id);

            if (existingAccount != null)
            {
                // Update existing account
                existingAccount.Email = accountDetail.FromEmail;
                existingAccount.Name = accountDetail.FromName ?? accountDetail.FromEmail ?? "Unknown";
                existingAccount.Status = NormalizeEmailAccountStatus(accountDetail.WarmupDetails?.Status ?? "");
                existingAccount.UpdatedAt = accountDetail.UpdatedAt;

                await _emailAccountRepository.UpdateAsync(existingAccount);
                updatedCount++;
            }
            else
            {
                // Create new account
                var newAccount = new EmailAccountDbModel
                {
                    AdminUuid = adminUuid,
                    Id = accountDetail.Id,
                    Email = accountDetail.FromEmail,
                    Name = accountDetail.FromName ?? accountDetail.FromEmail ?? "Unknown",
                    Status = NormalizeEmailAccountStatus(accountDetail.WarmupDetails?.Status ?? ""),
                    CreatedAt = accountDetail.CreatedAt,
                    UpdatedAt = accountDetail.UpdatedAt
                };

                await _emailAccountRepository.CreateAsync(newAccount);
                createdCount++;
            }
        }

        _logger.LogInformation("Email accounts processed: {Total} total, {Created} created, {Updated} updated, {Skipped} skipped",
            emailAccountDetails.Count, createdCount, updatedCount, skippedCount);
    }

    private async Task FetchEmailStatsForAccounts(string adminUuid)
    {
        DateTime startDate = new DateTime(2023, 6, 1);
        DateTime endDate = DateTime.UtcNow;

        // Generate list of dates to check
        List<DateTime> dateRange = GetDateRangeAsDateTime(startDate, endDate);

        // Optimization: Check which dates already have complete data for ALL accounts
        // Only fetch recent dates (last 7 days) + any missing historical dates
        DateTime recentCutoff = DateTime.UtcNow.AddDays(-7).Date;

        // Always fetch recent dates (they might have updated data)
        // For older dates, check if we already have data to avoid unnecessary API calls
        List<DateTime> filteredDateRange;

        if (dateRange.Count > 100) // Only optimize for large date ranges
        {
            var recentDates = dateRange.Where(d => d >= recentCutoff).ToList();
            var oldDates = dateRange.Where(d => d < recentCutoff).ToList();

            // For old dates, check if we have any data
            var datesWithData = new HashSet<DateTime>();
            if (oldDates.Any())
            {
                var existingStats = await _dailyStatEntryRepository.GetByAdminAndDateRangeAsync(
                    adminUuid,
                    oldDates.First(),
                    oldDates.Last()
                );

                foreach (var stat in existingStats)
                {
                    datesWithData.Add(stat.StatDate.Date);
                }
            }

            // Only fetch old dates that don't have data yet
            var oldDatesToFetch = oldDates.Where(d => !datesWithData.Contains(d)).ToList();

            filteredDateRange = recentDates.Concat(oldDatesToFetch).OrderBy(d => d).ToList();

            _logger.LogInformation(
                "Optimized date range: {Total} total dates, {Recent} recent, {Historical} historical missing, {Skipped} skipped",
                dateRange.Count, recentDates.Count, oldDatesToFetch.Count, oldDates.Count - oldDatesToFetch.Count
            );
        }
        else
        {
            filteredDateRange = dateRange;
        }

        if (!filteredDateRange.Any())
        {
            _logger.LogInformation("No new dates to fetch email stats for");
            return;
        }

        // Note: Callback removed - email_account_stats_dates table no longer exists (removed in migration 007)
        // Stats are now tracked directly in email_account_daily_stat_entries table
        _apiService.OnEmailStatsSuccessfullyFetched = null;

        // Fetch email stats for all filtered dates
        _logger.LogInformation("Fetching email stats for {Count} dates", filteredDateRange.Count);
        List<EmailHealthMetric>? allEmailHealthMetrics = _apiService.FetchEmailStats(filteredDateRange);

        _logger.LogInformation("API returned {Count} email health metrics", allEmailHealthMetrics?.Count ?? 0);

        if (allEmailHealthMetrics == null || !allEmailHealthMetrics.Any())
        {
            _logger.LogWarning("No email health metrics returned from API");
            return;
        }

        // Group metrics by email for more efficient processing
        var metricsByEmail = allEmailHealthMetrics.GroupBy(m => m.FromEmail);

        // Process each account's metrics individually
        _logger.LogInformation("Processing email stats for accounts");
        int processedAccounts = 0;
        int processedMetrics = 0;
        var accountsToUpdate = new List<EmailAccountDbModel>();

        foreach (var emailGroup in metricsByEmail)
        {
            string email = emailGroup.Key;

            // Fetch the specific account from database (one at a time)
            EmailAccountDbModel? account = await _emailAccountRepository.GetByEmailAsync(email);

            if (account == null)
            {
                _logger.LogDebug("No account found for email {Email}, skipping", email);
                continue;
            }

            processedAccounts++;

            // Process all metrics for this specific account
            foreach (EmailHealthMetric metric in emailGroup)
            {
                processedMetrics++;

                // Log some details about the stats being processed
                _logger.LogDebug("Processing stats for {Email}: Sent={Sent}, Opened={Opened}, Date={Date}",
                    metric.FromEmail, metric.Sent, metric.Opened,
                    metric.DateTime.ToString("yyyy-MM-dd"));

                // Update daily stats using the service (stats go to daily stats table only)
                await _dailyStatsService.UpdateDailyStatsAsync(
                    account.Id,
                    adminUuid,
                    metric.DateTime.Date,
                    metric.Sent,
                    metric.Opened,
                    metric.Replied,
                    metric.Bounced
                );
            }

            // Recalculate account totals from daily stats (source of truth)
            // This makes the operation idempotent - no matter how many times we run it,
            // the totals will always be correct based on daily stats
            var aggregatedStats = await _dailyStatEntryRepository.GetAggregatedStatsByEmailAccountAsync(
                account.Id,
                DateTime.Parse("2023-06-01"), // Start from when data collection began
                DateTime.UtcNow
            );

            if (aggregatedStats != null)
            {
                account.Sent = aggregatedStats.Sent;
                account.Opened = aggregatedStats.Opened;
                account.Replied = aggregatedStats.Replied;
                account.Bounced = aggregatedStats.Bounced;
            }

            accountsToUpdate.Add(account);
            _logger.LogDebug("Processed {Count} metrics for account {Email}, recalculated totals: Sent={Sent}, Opened={Opened}",
                emailGroup.Count(), email, account.Sent, account.Opened);
        }

        // Update all accounts in batch
        foreach (var account in accountsToUpdate)
        {
            await _emailAccountRepository.UpdateAsync(account);
        }

        _logger.LogInformation(
            "Processed {MetricCount} email metrics for {AccountCount} accounts and updated their totals",
            processedMetrics, processedAccounts);
    }

    private async Task FetchWarmupStatsForAccounts(List<EmailAccountDbModel> accountsToUpdate)
    {
        _logger.LogInformation("Fetching warmup statistics for {Count} accounts", accountsToUpdate.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var account in accountsToUpdate)
        {
            try
            {
                // Fetch warmup stats from API using the email account ID
                var warmupStats = _apiService.FetchEmailWarmupStats(account.Id.ToString());

                if (warmupStats is { StatsByDate: not null })
                {
                    // Calculate totals from the daily stats (matching the old implementation)
                    int totalSent = warmupStats.StatsByDate.Sum(s => s.SentCount);
                    int totalReplied = warmupStats.StatsByDate.Sum(s => s.ReplyCount);
                    int totalSavedFromSpam = warmupStats.StatsByDate.Sum(s => s.SaveFromSpamCount);
                    int.TryParse(warmupStats.SpamCount, out int totalSpam);

                    // Update the email account with warmup stats
                    account.WarmupSent = totalSent;
                    account.WarmupReplied = totalReplied;
                    account.WarmupSpamCount = totalSpam;
                    account.WarmupSavedFromSpam = totalSavedFromSpam;
                    account.WarmupUpdateDateTime = DateTime.UtcNow;
                    account.UpdatedAt = DateTime.UtcNow;

                    await _emailAccountRepository.UpdateAsync(account);
                    successCount++;

                    _logger.LogDebug(
                        "Updated warmup stats for {Email}: Sent={Sent}, Replied={Replied}, SavedFromSpam={Saved}",
                        account.Email, totalSent, totalReplied, totalSavedFromSpam);
                }
                else
                {
                    // No warmup data returned, but still update the timestamp
                    account.WarmupUpdateDateTime = DateTime.UtcNow;
                    await _emailAccountRepository.UpdateAsync(account);

                    _logger.LogDebug("No warmup data returned for account {Email} (ID: {AccountId})",
                        account.Email, account.Id);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                // Still update timestamp even on failure to prevent constant retries
                account.WarmupUpdateDateTime = DateTime.UtcNow;
                await _emailAccountRepository.UpdateAsync(account);

                _logger.LogError(ex, "Error fetching warmup stats for account {Email} (ID: {AccountId})",
                    account.Email, account.Id);
            }
        }

        _logger.LogInformation(
            "Completed fetching warmup statistics for {TotalCount} accounts: {SuccessCount} successful, {FailureCount} failed",
            accountsToUpdate.Count, successCount, failureCount);
    }

    #endregion

    #region Campaign Management

    private async Task FetchEmailAccountsFromCampaigns()
    {
        const string adminUuid = "c38e38e8-d7b6-4c00-836d-3437678ef4d9";

        try
        {
            _logger.LogInformation("Starting campaign fetch and email account association");

            // Fetch campaigns from API
            var campaignSummaries = _apiService.FetchCampaigns();
            if (!campaignSummaries.Any())
            {
                _logger.LogInformation("No campaigns found from API");
                return;
            }

            _logger.LogInformation("Fetched {Count} campaigns from API", campaignSummaries.Count);

            foreach (var campaign in campaignSummaries)
            {
                if (!campaign.Id.HasValue) continue;

                var campaignId = campaign.Id.Value;
                _logger.LogInformation("Processing campaign {CampaignId} ({Name})", campaignId, campaign.Name);

                // Check if a campaign exists
                var existingCampaigns = await _campaignRepository.GetByCampaignIdAsync(campaignId);
                var existingCampaign = existingCampaigns.FirstOrDefault();

                if (existingCampaign == null)
                {
                    // Create new campaign
                    var newCampaign = new CampaignDetailsDbModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        AdminUuid = adminUuid,
                        CampaignId = campaignId,
                        Name = campaign.Name,
                        Status = campaign.Status,
                        CreatedAt = campaign.CreatedAt,
                        UpdatedAt = campaign.UpdatedAt
                    };

                    await _campaignRepository.CreateAsync(newCampaign);
                    existingCampaign = newCampaign;
                    _logger.LogInformation("Created new campaign {CampaignId}", campaignId);
                }

                // Skip if campaign was recently updated (avoid unnecessary API calls)
                if (existingCampaign.LastUpdatedAt.HasValue &&
                    DateTime.UtcNow.Subtract(existingCampaign.LastUpdatedAt.Value) <= TimeSpan.FromMinutes(1440))
                {
                    _logger.LogDebug("Skipping campaign {CampaignId} - recently updated", campaignId);
                    continue;
                }

                // Fetch email accounts associated with this campaign
                var emailIds = _apiService.FetchEmailAccountsOfCampaign(campaignId.ToString());
                if (emailIds != null && emailIds.Any())
                {
                    existingCampaign.EmailIds = emailIds;
                    _logger.LogInformation("Campaign {CampaignId} has {Count} email accounts", campaignId,
                        emailIds.Count);
                }

                // Fetch and process detailed campaign stats
                await ProcessCampaignStats(existingCampaign, campaignId, adminUuid);

                // Update the campaign record
                existingCampaign.Name = campaign.Name;
                existingCampaign.Status = campaign.Status;
                existingCampaign.UpdatedAt = campaign.UpdatedAt;
                existingCampaign.LastUpdatedAt = DateTime.UtcNow;
                await _campaignRepository.UpdateAsync(existingCampaign);
            }

            _logger.LogInformation("Completed campaign fetch and email account association successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchEmailAccountsFromCampaigns");
        }
    }

    private async Task ProcessCampaignStats(CampaignDetailsDbModel campaign, int campaignId, string adminUuid)
    {
        try
        {
            _logger.LogInformation("Starting stats processing for campaign {CampaignId} ({Name})", campaignId, campaign.Name);

            var campaignIdStr = campaignId.ToString();

            // Fetch individual email stats for the campaign
            var stats = _apiService.FetchCampaignStats(campaignIdStr);
            _logger.LogInformation("Campaign {CampaignId} ({Name}): Fetched {Count} individual email stats from API",
                campaignId, campaign.Name, stats.Count);

            // Fetch leads data
            var leads = _apiService.FetchLeads(campaignIdStr);
            _logger.LogInformation("Campaign {CampaignId} ({Name}): Fetched {Count} leads from API",
                campaignId, campaign.Name, leads.Count);
            var leadsStarted = leads.Count(lead => lead.Status.Equals("Started", StringComparison.OrdinalIgnoreCase));
            var leadsCompleted =
                leads.Count(lead => lead.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
            var leadsInProgress =
                leads.Count(lead => lead.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase));
            var leadsBlocked = leads.Count(lead => lead.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase));

            // Process daily stats and store directly in PostgreSQL (no dictionaries)
            // Group stats by date for efficient processing
            var dailyStatsByDate = stats
                .GroupBy(s => s.SentTime.Date)
                .ToDictionary(g => g.Key, g => new
                {
                    Sent = g.Count(),
                    Opened = g.Count(s => s.OpenTime.HasValue),
                    Clicked = g.Count(s => s.ClickTime.HasValue),
                    Replied = g.Count(s => s.ReplyTime.HasValue)
                });

            // Store events using new event-sourced system
            var events = new List<CampaignEvent>();
            foreach (var dailyEntry in dailyStatsByDate)
            {
                var eventDate = dailyEntry.Key;
                var campaignIdString = campaignId.ToString();

                // Create individual events for each metric type
                if (dailyEntry.Value.Sent > 0)
                    events.Add(new CampaignEvent
                    {
                        CampaignId = campaignIdString, EventType = "sent", EventDate = eventDate,
                        EventCount = dailyEntry.Value.Sent
                    });

                if (dailyEntry.Value.Opened > 0)
                    events.Add(new CampaignEvent
                    {
                        CampaignId = campaignIdString, EventType = "opened", EventDate = eventDate,
                        EventCount = dailyEntry.Value.Opened
                    });

                if (dailyEntry.Value.Clicked > 0)
                    events.Add(new CampaignEvent
                    {
                        CampaignId = campaignIdString, EventType = "clicked", EventDate = eventDate,
                        EventCount = dailyEntry.Value.Clicked
                    });

                if (dailyEntry.Value.Replied > 0)
                    events.Add(new CampaignEvent
                    {
                        CampaignId = campaignIdString, EventType = "replied", EventDate = eventDate,
                        EventCount = dailyEntry.Value.Replied
                    });
            }

            // Bulk insert events
            if (events.Any())
            {
                await _campaignEventRepository.AddBulkEventsAsync(events);
            }

            // Fetch and store positive reply stats directly to PostgreSQL
            var totalPositiveReplyCount = 0;
            try
            {
                var positiveReply = _apiService.FetchDaybyDayPositiveReplyStats(campaignIdStr,
                    new DateTime(2023, 1, 1), DateTime.UtcNow);

                if (positiveReply?.Success == true && positiveReply.Data?.DayWiseStats != null)
                {
                    int year = 2023;
                    foreach (var stat in positiveReply.Data.DayWiseStats)
                    {
                        if (int.TryParse(stat.EmailEngagementMetrics.PositiveReplied.ToString(),
                                out int positiveReplied))
                        {
                            string dateString = $"{stat.Date} {year}";
                            if (DateOnly.TryParseExact(dateString, "dd MMM yyyy",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None, out DateOnly date))
                            {
                                totalPositiveReplyCount += positiveReplied;

                                // Add positive reply event using new event system
                                if (positiveReplied > 0)
                                {
                                    await _campaignEventRepository.AddEventAsync(
                                        campaignId.ToString(),
                                        "positive_reply",
                                        positiveReplied,
                                        null,
                                        new Dictionary<string, object>
                                        {
                                            { "event_date", date.ToDateTime(TimeOnly.MinValue) },
                                            { "source", "smartlead_positive_reply_api" }
                                        }
                                    );
                                }

                                if (stat.Date.Equals("31 Dec", StringComparison.OrdinalIgnoreCase))
                                    year++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch positive reply stats for campaign {CampaignId}", campaignId);
            }

            _logger.LogInformation("Campaign {CampaignId}: Stored daily stats directly to PostgreSQL", campaignId);

            // Recalculate campaign totals from database (source of truth)
            // This makes the operation idempotent - totals will always be correct
            // even if the sync is run multiple times
            using var connection = await _connectionService.GetConnectionAsync();
            const string totalsSql = @"
                SELECT
                    SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END) as total_sent,
                    SUM(CASE WHEN event_type = 'opened' THEN event_count ELSE 0 END) as total_opened,
                    SUM(CASE WHEN event_type = 'clicked' THEN event_count ELSE 0 END) as total_clicked,
                    SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END) as total_replied,
                    SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END) as total_positive_replies,
                    SUM(CASE WHEN event_type = 'bounced' THEN event_count ELSE 0 END) as total_bounced
                FROM campaign_events
                WHERE campaign_id = @CampaignId";

            var totalsResult = await connection.QueryFirstOrDefaultAsync<dynamic>(totalsSql, new { CampaignId = campaignIdStr });

            campaign.TotalSent = (int)(totalsResult?.total_sent ?? 0);
            campaign.TotalOpened = (int)(totalsResult?.total_opened ?? 0);
            campaign.TotalClicked = (int)(totalsResult?.total_clicked ?? 0);
            campaign.TotalReplied = (int)(totalsResult?.total_replied ?? 0);
            campaign.TotalPositiveReplies = (int)(totalsResult?.total_positive_replies ?? 0);
            campaign.TotalBounced = (int)(totalsResult?.total_bounced ?? 0);
            campaign.TotalLeads = leads.Count;

            _logger.LogInformation("Campaign {CampaignId}: Recalculated totals from database - Sent={Sent}, Opened={Opened}, Replied={Replied}, PositiveReplies={PositiveReplies}",
                campaignId, campaign.TotalSent, campaign.TotalOpened, campaign.TotalReplied, campaign.TotalPositiveReplies);

            _logger.LogInformation("Completed stats processing for campaign {CampaignId} ({Name})", campaignId, campaign.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing campaign stats for campaign {CampaignId}", campaignId);
        }
    }

    #endregion

    #region Campaign Templates and Leads Management

    private async Task FetchCampaignTemplates()
    {
        const string adminUuid = "c38e38e8-d7b6-4c00-836d-3437678ef4d9";

        try
        {
            _logger.LogInformation("Starting campaign templates fetch");

            // Get all campaigns to fetch templates for
            var campaigns = await _campaignRepository.GetByAdminUuidAsync(adminUuid);
            var campaignList = campaigns.ToList();

            _logger.LogInformation("Processing templates for {Count} campaigns", campaignList.Count);

            foreach (var campaign in campaigns)
            {
                try
                {
                    // Check if templates were recently updated (avoid unnecessary API calls)
                    var existingTemplateCount =
                        await _emailTemplateRepository.CountByCampaignIdAsync(campaign.CampaignId);
                    if (existingTemplateCount > 0 && campaign.LastUpdatedAt.HasValue &&
                        DateTime.UtcNow.Subtract(campaign.LastUpdatedAt.Value) <= TimeSpan.FromHours(24))
                    {
                        _logger.LogDebug("Skipping templates for campaign {CampaignId} - recently updated",
                            campaign.CampaignId);
                        continue;
                    }

                    // Fetch templates from Smartlead API
                    List<CampaignEmailTemplate>? templates =
                        _apiService.FetchCampaignTemplates(campaign.CampaignId.ToString());

                    if (templates?.Any() == true)
                    {
                        // UPSERT templates (idempotent - safe for abrupt shutdown)
                        foreach (var template in templates)
                        {
                            var templateModel = new EmailTemplateDbModel
                            {
                                AdminUuid = adminUuid,
                                CampaignId = campaign.CampaignId,
                                Subject = template.Subject ?? string.Empty,
                                Body = template.Body ?? string.Empty,
                                SequenceNumber = template.SequenceNumber
                            };

                            string templateId = await _emailTemplateRepository.UpsertAsync(templateModel);

                            // UPSERT variants if they exist
                            if (template.Variants != null && template.Variants.Any())
                            {
                                foreach (var variant in template.Variants)
                                {
                                    var variantModel = new EmailTemplateVariantDbModel
                                    {
                                        TemplateId = templateId,
                                        SmartleadVariantId = variant.Id,
                                        VariantLabel = variant.VariantLabel,
                                        Subject = variant.Subject ?? string.Empty,
                                        Body = variant.Body ?? string.Empty
                                    };

                                    await _emailTemplateRepository.UpsertVariantAsync(variantModel);
                                }

                                _logger.LogInformation("Cached {Count} variants for template {TemplateId}",
                                    template.Variants.Count, templateId);
                            }
                        }

                        _logger.LogInformation("Cached {Count} email templates for campaign {CampaignId}",
                            templates.Count, campaign.CampaignId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching templates for campaign {CampaignId}", campaign.CampaignId);
                }
            }

            _logger.LogInformation("Completed campaign templates fetch successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchCampaignTemplates");
        }
    }

    private async Task FetchCampaignLeads()
    {
        const string adminUuid = "c38e38e8-d7b6-4c00-836d-3437678ef4d9";

        try
        {
            _logger.LogInformation("Starting campaign leads fetch");

            // Get all campaigns to fetch leads for
            var campaigns = await _campaignRepository.GetByAdminUuidAsync(adminUuid);
            var campaignList = campaigns.ToList();

            _logger.LogInformation("Processing leads for {Count} campaigns", campaignList.Count);

            foreach (var campaign in campaignList)
            {
                try
                {
                    // Get or create sync progress tracking
                    var syncProgress = await GetOrCreateSyncProgress(campaign.CampaignId);

                    // Skip if recently completed successfully (within 6 hours)
                    if (syncProgress.SyncStatus == "completed" &&
                        syncProgress.SyncCompletedAt.HasValue &&
                        DateTime.UtcNow.Subtract(syncProgress.SyncCompletedAt.Value) <= TimeSpan.FromHours(6))
                    {
                        _logger.LogDebug("Skipping campaign {CampaignId} - recently completed (completed at {CompletedAt})",
                            campaign.CampaignId, syncProgress.SyncCompletedAt);
                        continue;
                    }

                    // Mark sync as in progress
                    await UpdateSyncProgress(campaign.CampaignId, "in_progress", null, null);

                    // Fetch leads from Smartlead API
                    LeadIdResponse[] leads = _apiService.FetchLeadIds(campaign.CampaignId);

                    if (leads.Length == 0)
                    {
                        _logger.LogInformation("No leads found for campaign {CampaignId}, marking as completed", campaign.CampaignId);
                        await UpdateSyncProgress(campaign.CampaignId, "completed", null, null);
                        continue;
                    }

                    _logger.LogInformation("Fetched {Count} leads from API for campaign {CampaignId}", leads.Length, campaign.CampaignId);

                    // Update total leads count
                    await UpdateSyncProgressTotalLeads(campaign.CampaignId, leads.Length);

                    // Resume from last processed lead if sync was interrupted
                    var leadsToProcess = leads.AsEnumerable();
                    if (syncProgress.SyncStatus == "partial" && !string.IsNullOrEmpty(syncProgress.LastProcessedLeadEmail))
                    {
                        var lastIndex = Array.FindIndex(leads, l => l.Lead?.Email == syncProgress.LastProcessedLeadEmail);
                        if (lastIndex >= 0)
                        {
                            leadsToProcess = leads.Skip(lastIndex + 1);
                            _logger.LogInformation("Resuming campaign {CampaignId} from lead index {Index} (last processed: {LastEmail})",
                                campaign.CampaignId, lastIndex + 1, syncProgress.LastProcessedLeadEmail);
                        }
                    }

                    int processedCount = syncProgress.LeadsProcessed;

                    // Process leads with transaction protection
                    foreach (var leadDatum in leadsToProcess)
                    {
                        using var connection = await _connectionService.GetConnectionAsync();
                        using var transaction = connection.BeginTransaction();

                        try
                        {
                            // Fetch history for this specific lead
                            LeadEmailHistory[] history =
                                _apiService.FetchHistory(campaign.CampaignId.ToString(), leadDatum.Id);

                            // UPSERT lead basic info with safe string lengths and sync tracking
                            const string leadUpsertSql = @"
                                INSERT INTO lead_conversations (
                                    id, admin_uuid, campaign_id, lead_email, lead_first_name,
                                    lead_last_name, status, conversation_data, created_at, updated_at,
                                    last_synced_at, sync_status
                                )
                                VALUES (
                                    @Id, @AdminUuid, @CampaignId, @LeadEmail, @LeadFirstName,
                                    @LeadLastName, @Status, @ConversationData, @CreatedAt, @UpdatedAt,
                                    @LastSyncedAt, @SyncStatus
                                )
                                ON CONFLICT (campaign_id, lead_email)
                                DO UPDATE SET
                                    lead_first_name = EXCLUDED.lead_first_name,
                                    lead_last_name = EXCLUDED.lead_last_name,
                                    status = EXCLUDED.status,
                                    conversation_data = EXCLUDED.conversation_data,
                                    updated_at = EXCLUDED.updated_at,
                                    last_synced_at = EXCLUDED.last_synced_at,
                                    sync_status = EXCLUDED.sync_status";

                            await connection.ExecuteAsync(leadUpsertSql, new
                            {
                                Id = Guid.NewGuid().ToString(),
                                AdminUuid = adminUuid,
                                CampaignId = campaign.CampaignId,
                                LeadEmail = TruncateString(leadDatum.Lead?.Email ?? string.Empty, 500),
                                LeadFirstName = TruncateString(leadDatum.Lead?.FirstName ?? string.Empty, 500),
                                LeadLastName = TruncateString(leadDatum.Lead?.LastName ?? string.Empty, 500),
                                Status = TruncateString(leadDatum.Status ?? string.Empty, 50),
                                ConversationData = JsonConvert.SerializeObject(leadDatum),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                LastSyncedAt = DateTime.UtcNow,
                                SyncStatus = "completed"
                            }, transaction);

                            // UPSERT email history within transaction
                        const string historyUpsertSql = @"
                            INSERT INTO lead_email_history (
                                id, admin_uuid, campaign_id, lead_id, lead_email, subject, body,
                                sequence_number, time, type, created_at, updated_at
                            )
                            VALUES (
                                @Id, @AdminUuid, @CampaignId, @LeadId, @LeadEmail, @Subject, @Body,
                                @SequenceNumber, @Time, @Type, @CreatedAt, @UpdatedAt
                            )
                            ON CONFLICT (campaign_id, lead_id, sequence_number, type)
                            DO UPDATE SET
                                subject = EXCLUDED.subject,
                                body = EXCLUDED.body,
                                time = EXCLUDED.time,
                                updated_at = EXCLUDED.updated_at";

                            foreach (var emailHistory in history)
                            {
                                await connection.ExecuteAsync(historyUpsertSql, new
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    AdminUuid = adminUuid,
                                    CampaignId = campaign.CampaignId,
                                    LeadId = TruncateString(leadDatum.Id, 50),
                                    LeadEmail = TruncateString(leadDatum.Lead?.Email ?? string.Empty, 500),
                                    Subject = emailHistory.Subject ?? string.Empty,
                                    Body = emailHistory.Body ?? string.Empty,
                                    SequenceNumber = emailHistory.SequenceNumber,
                                    Type = emailHistory.Type,
                                    Time = emailHistory.Time,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                }, transaction);
                            }

                            // Commit transaction - lead and all email history saved atomically
                            transaction.Commit();

                            // Update progress tracking
                            processedCount++;
                            await UpdateSyncProgress(campaign.CampaignId, "partial",
                                leadDatum.Id, leadDatum.Lead?.Email, processedCount);

                            _logger.LogDebug("Successfully processed lead {LeadEmail} for campaign {CampaignId} ({Processed}/{Total})",
                                leadDatum.Lead?.Email, campaign.CampaignId, processedCount, leads.Length);
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction on any failure
                            transaction.Rollback();
                            _logger.LogError(ex, "Error processing lead {LeadEmail} for campaign {CampaignId}, rolled back transaction",
                                leadDatum.Lead?.Email, campaign.CampaignId);

                            // Mark sync as failed and save error
                            await UpdateSyncProgress(campaign.CampaignId, "failed", null, null, processedCount, ex.Message);
                            throw; // Re-throw to be caught by campaign-level handler
                        }
                    }

                    // Mark campaign sync as completed
                    await UpdateSyncProgress(campaign.CampaignId, "completed", null, null, processedCount);
                    _logger.LogInformation("Successfully cached {Count} lead conversations for campaign {CampaignId}",
                        processedCount, campaign.CampaignId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching leads for campaign {CampaignId}", campaign.CampaignId);
                }
            }

            _logger.LogInformation("Completed campaign leads fetch successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchCampaignLeads");
        }
    }

    #endregion

    #region Lead Conversation Analysis

    private async Task AnalyzeLeadConversationsWithRevReply()
    {
        const string adminUuid = "c38e38e8-d7b6-4c00-836d-3437678ef4d9";

        try
        {
            _logger.LogInformation(
                "Starting lead conversation analysis with RevReply API using inbox-replies endpoint");

            // Get all campaigns to analyze emails for
            IEnumerable<CampaignDetailsDbModel> campaigns = await _campaignRepository.GetByAdminUuidAsync(adminUuid);
            var campaignList = campaigns.ToList();

            _logger.LogInformation("Analyzing email replies for {Count} campaigns", campaignList.Count);

            int totalProcessed = 0;
            int totalClassified = 0;
            int totalFailed = 0;

            foreach (CampaignDetailsDbModel campaign in campaignList)
            {
                try
                {
                    _logger.LogInformation("Analyzing emails for campaign {CampaignId} ({Name})", campaign.CampaignId,
                        campaign.Name);

                    // Fetch emails from Smartlead inbox-replies API for this campaign
                    var inboxRepliesResponse =
                        _apiService.FetchInboxReplies(offset: 0, limit: 20, campaignId: campaign.CampaignId);

                    if (inboxRepliesResponse?.Data == null || !inboxRepliesResponse.Data.Any())
                    {
                        _logger.LogInformation("No inbox replies found for campaign {CampaignId}", campaign.CampaignId);
                        continue;
                    }

                    _logger.LogInformation("Found {EmailCount} inbox replies for campaign {CampaignId}",
                        inboxRepliesResponse.Data.Count, campaign.CampaignId);

                    // Process each inbox reply and its email history
                    foreach (var inboxReply in inboxRepliesResponse.Data)
                    {
                        if (inboxReply.EmailHistory == null || !inboxReply.EmailHistory.Any())
                            continue;

                        // Process each email in the conversation history
                        foreach (var emailHistory in inboxReply.EmailHistory.Where(e =>
                                     e.Type == "REPLY" && !string.IsNullOrWhiteSpace(e.EmailBody)))
                        {
                            try
                            {
                                // Skip if this email has already been classified
                                if (!string.IsNullOrWhiteSpace(emailHistory.MessageId))
                                {
                                    bool alreadyClassified =
                                        await _classifiedEmailRepository.ExistsByMessageIdAsync(emailHistory.MessageId);
                                    if (alreadyClassified)
                                    {
                                        _logger.LogDebug(
                                            "Email {MessageId} from {LeadEmail} already classified, skipping",
                                            emailHistory.MessageId, inboxReply.LeadEmail);
                                        continue;
                                    }
                                }

                                // Convert HTML to plain text for better classification
                                string emailBodyText = ConvertHtmlToText(emailHistory.EmailBody ?? string.Empty);

                                if (string.IsNullOrWhiteSpace(emailBodyText))
                                    continue;

                                // Create email body hash for additional duplicate detection
                                string emailBodyHash = ComputeSha256Hash(emailBodyText);

                                // Check if we've already classified an email with this exact content
                                bool duplicateContent =
                                    await _classifiedEmailRepository.ExistsByEmailBodyHashAsync(emailBodyHash);
                                if (duplicateContent)
                                {
                                    _logger.LogDebug(
                                        "Email content from {LeadEmail} already classified (duplicate content), skipping",
                                        inboxReply.LeadEmail);
                                    continue;
                                }

                                _logger.LogInformation("Processing reply from {LeadEmail} in campaign {CampaignId}",
                                    inboxReply.LeadEmail, campaign.CampaignId);

                                // Call RevReply API to classify the message
                                var classificationResult =
                                    await _revReplyClassificationService.ClassifyEmailAsync(emailBodyText);

                                // Create classified email record
                                var classifiedEmail = new ClassifiedEmailDbModel
                                {
                                    AdminUuid = adminUuid,
                                    CampaignId = campaign.CampaignId,
                                    EmailAccountId =
                                        inboxReply
                                            .EmailAccountId, // Store email account ID for frontend linking/mapping
                                    MessageId = emailHistory.MessageId ?? Guid.NewGuid().ToString(),
                                    LeadEmail = inboxReply.LeadEmail ?? string.Empty,
                                    EmailType = emailHistory.Type ?? "REPLY",
                                    EmailTime = emailHistory.Time ?? DateTime.UtcNow,
                                    EmailBodyHash = emailBodyHash,
                                    ClassificationResult = classificationResult.Success
                                        ? classificationResult.ClassificationResult
                                        : "FAILED",
                                    ClassifiedAt = DateTime.UtcNow
                                };

                                // UPSERT the classification record (idempotent - safe for abrupt shutdown)
                                await _classifiedEmailRepository.UpsertAsync(classifiedEmail);

                                totalProcessed++;

                                if (classificationResult.Success)
                                {
                                    totalClassified++;
                                    _logger.LogInformation("Email from {LeadEmail} classified as: {Classification}",
                                        inboxReply.LeadEmail, classificationResult.ClassificationResult);
                                }
                                else
                                {
                                    totalFailed++;
                                    _logger.LogWarning("Failed to classify email from {LeadEmail}: {Error}",
                                        inboxReply.LeadEmail, classificationResult.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error analyzing email from {LeadEmail}", inboxReply.LeadEmail);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing campaign {CampaignId}", campaign.CampaignId);
                }
            }

            _logger.LogInformation(
                "Completed lead conversation analysis with RevReply API: {TotalProcessed} processed, {TotalClassified} classified successfully, {TotalFailed} failed",
                totalProcessed, totalClassified, totalFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeLeadConversationsWithRevReply");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Converts HTML content to plain text for better email classification
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <returns>Plain text content</returns>
    private static string ConvertHtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";

        // Remove HTML tags using regex for simple conversion
        string pattern = "<.*?>";
        string plainText = System.Text.RegularExpressions.Regex.Replace(html, pattern, "");

        // Decode HTML entities
        plainText = System.Net.WebUtility.HtmlDecode(plainText);

        // Clean up excessive whitespace
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");

        return plainText.Trim();
    }

    /// <summary>
    /// Computes SHA256 hash of email content for duplicate detection
    /// </summary>
    /// <param name="content">Email content</param>
    /// <returns>SHA256 hash string</returns>
    private static string ComputeSha256Hash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Safely truncates a string to the specified maximum length to prevent database field overflow
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>Truncated string</returns>
    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return input.Substring(0, maxLength);
    }

    private List<DateTime> GetDateRangeAsDateTime(DateTime startDate, DateTime endDate)
    {
        var dates = new List<DateTime>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
    }

    private async Task<EmailAccountStatsDate?> GetEmailAccountStatsDateAsync(string adminUuid, DateTime date)
    {
        return await _emailAccountStatsDateRepository.GetByDateAsync(adminUuid, date);
    }

    private async Task ProcessEmailStatsForDate(string adminUuid, DateTime date)
    {
        var dateString = date.ToString("yyyy-MM-dd");

        try
        {
            // Fetch stats from API for this specific date
            var stats = _apiService.FetchEmailStats(new List<DateTime> { date });

            if (stats != null && stats.Any())
            {
                // Get all email accounts for this admin
                var emailAccounts = await _emailAccountRepository.GetByAdminUuidAsync(adminUuid);
                // Handle duplicate email addresses by taking the first one and logging duplicates
                var emailAccountsDict = new Dictionary<string, EmailAccountDbModel>();
                foreach (var account in emailAccounts)
                {
                    if (!emailAccountsDict.ContainsKey(account.Email))
                    {
                        emailAccountsDict[account.Email] = account;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Duplicate email address found in ProcessEmailStatsForDate: {Email} (ID: {Id})",
                            account.Email, account.Id);
                    }
                }

                // Process each email health metric
                foreach (var emailHealthMetric in stats)
                {
                    if (emailAccountsDict.TryGetValue(emailHealthMetric.FromEmail, out var emailAccount))
                    {
                        // Only update daily stats using the service (removed accumulation on email account)
                        // Stats on email accounts are now calculated from daily stats table when needed
                        await _dailyStatsService.UpdateDailyStatsAsync(
                            emailAccount.Id,
                            adminUuid,
                            emailHealthMetric.DateTime.Date,
                            emailHealthMetric.Sent,
                            emailHealthMetric.Opened,
                            emailHealthMetric.Replied,
                            emailHealthMetric.Bounced
                        );
                    }
                }

                // Mark this date as processed
                await _emailAccountStatsDateRepository.CreateAsync(new EmailAccountStatsDate
                {
                    AdminUuid = adminUuid,
                    LatestDateTime = date
                });

                _logger.LogInformation("Processed email stats for date {Date}", dateString);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email stats for date {Date}", dateString);
        }
    }

    #endregion

    #region Campaign Count Update

    private async Task UpdateCampaignCounts()
    {
        try
        {
            _logger.LogInformation("Starting campaign count update for all email accounts");

            const string updateSql = @"
                UPDATE email_accounts 
                SET 
                    campaign_count = COALESCE((
                        SELECT COUNT(*)
                        FROM campaigns c
                        WHERE jsonb_array_length(c.email_ids) > 0 
                        AND c.email_ids @> CAST(CONCAT('[', email_accounts.id, ']') AS JSONB)
                    ), 0),
                    active_campaign_count = COALESCE((
                        SELECT COUNT(*)
                        FROM campaigns c
                        WHERE jsonb_array_length(c.email_ids) > 0 
                        AND c.email_ids ? email_accounts.id::text
                        AND c.status = 'ACTIVE'
                    ), 0),
                    is_sending_actual_emails = CASE 
                        WHEN sent > 0 THEN true
                        WHEN sent = 0 AND warmup_sent > 0 THEN false
                        ELSE NULL
                    END";

            using IDbConnection connection = await _connectionService.GetConnectionAsync();
            int rowsAffected = await connection.ExecuteAsync(updateSql);

            _logger.LogInformation("Updated campaign counts and email sending status for {RowsAffected} email accounts",
                rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign counts");
        }
    }

    /// <summary>
    /// Normalizes email account status values from the Smartlead API to ensure consistent sorting
    /// </summary>
    private static string NormalizeEmailAccountStatus(string apiStatus)
    {
        if (string.IsNullOrWhiteSpace(apiStatus))
            return "inactive";

        var normalized = apiStatus.ToLower().Trim();

        return normalized switch
        {
            "active" or "running" or "live" => "active",
            "warming" or "warmup" or "warming_up" or "warming up" => "warming",
            "warmed" or "warmed_up" or "warmed up" => "warmed",
            "inactive" or "paused" or "stopped" => "inactive",
            "error" or "issue" or "blocked" or "failed" => "error",
            _ => normalized.Length > 0 ? normalized : "inactive"
        };
    }

    #endregion

    #region Sync Progress Tracking Helpers

    private async Task<CampaignSyncProgressDbModel> GetOrCreateSyncProgress(int campaignId)
    {
        using var connection = await _connectionService.GetConnectionAsync();

        const string selectSql = "SELECT * FROM campaign_sync_progress WHERE campaign_id = @CampaignId";
        var progress = await connection.QueryFirstOrDefaultAsync<CampaignSyncProgressDbModel>(selectSql,
            new { CampaignId = campaignId });

        if (progress != null)
            return progress;

        // Create new progress record
        const string insertSql = @"
            INSERT INTO campaign_sync_progress (
                campaign_id, sync_status, created_at, updated_at
            )
            VALUES (@CampaignId, 'not_started', @Now, @Now)
            RETURNING *";

        var now = DateTime.UtcNow;
        progress = await connection.QuerySingleAsync<CampaignSyncProgressDbModel>(insertSql,
            new { CampaignId = campaignId, Now = now });

        return progress;
    }

    private async Task UpdateSyncProgressTotalLeads(int campaignId, int totalLeads)
    {
        using var connection = await _connectionService.GetConnectionAsync();

        const string sql = @"
            UPDATE campaign_sync_progress
            SET total_leads_in_campaign = @TotalLeads,
                sync_started_at = @Now,
                updated_at = @Now
            WHERE campaign_id = @CampaignId";

        await connection.ExecuteAsync(sql, new
        {
            CampaignId = campaignId,
            TotalLeads = totalLeads,
            Now = DateTime.UtcNow
        });
    }

    private async Task UpdateSyncProgress(
        int campaignId,
        string status,
        string? lastProcessedLeadId,
        string? lastProcessedLeadEmail,
        int? leadsProcessed = null,
        string? errorMessage = null)
    {
        using var connection = await _connectionService.GetConnectionAsync();

        var now = DateTime.UtcNow;
        DateTime? completedAt = status == "completed" ? now : null;

        const string sql = @"
            UPDATE campaign_sync_progress
            SET sync_status = @Status,
                last_processed_lead_id = COALESCE(@LastProcessedLeadId, last_processed_lead_id),
                last_processed_lead_email = COALESCE(@LastProcessedLeadEmail, last_processed_lead_email),
                leads_processed = COALESCE(@LeadsProcessed, leads_processed),
                sync_completed_at = @CompletedAt,
                error_message = @ErrorMessage,
                updated_at = @Now
            WHERE campaign_id = @CampaignId";

        await connection.ExecuteAsync(sql, new
        {
            CampaignId = campaignId,
            Status = status,
            LastProcessedLeadId = lastProcessedLeadId,
            LastProcessedLeadEmail = lastProcessedLeadEmail,
            LeadsProcessed = leadsProcessed,
            CompletedAt = completedAt,
            ErrorMessage = errorMessage,
            Now = now
        });
    }

    #endregion
}