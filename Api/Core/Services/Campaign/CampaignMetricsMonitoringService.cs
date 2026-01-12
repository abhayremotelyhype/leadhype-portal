using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.Database.WebhookEvent;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Models;
using LeadHype.Api.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Dapper;
using Npgsql;

namespace LeadHype.Api.Services;

public interface ICampaignMetricsMonitoringService
{
    /// <summary>
    /// Check all active monitoring configs and trigger webhooks if thresholds are exceeded
    /// </summary>
    Task CheckMetricsThresholdsAsync();
    
    /// <summary>
    /// Check a specific webhook event config
    /// </summary>
    Task CheckEventConfigAsync(WebhookEventConfig config);
}

public class CampaignMetricsMonitoringService : ICampaignMetricsMonitoringService
{
    private readonly IWebhookEventConfigRepository _eventConfigRepository;
    private readonly IWebhookEventTriggerRepository _eventTriggerRepository;
    private readonly ICampaignDailyStatEntryRepository _campaignStatsRepository;
    private readonly IEmailAccountDailyStatEntryRepository _emailAccountStatsRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IWebhookRepository _webhookRepository;
    private readonly IWebhookService _webhookService;
    private readonly IUserRepository _userRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CampaignMetricsMonitoringService> _logger;
    private readonly string _connectionString;

    public CampaignMetricsMonitoringService(
        IWebhookEventConfigRepository eventConfigRepository,
        IWebhookEventTriggerRepository eventTriggerRepository,
        ICampaignDailyStatEntryRepository campaignStatsRepository,
        IEmailAccountDailyStatEntryRepository emailAccountStatsRepository,
        ICampaignRepository campaignRepository,
        IEmailAccountRepository emailAccountRepository,
        IClientRepository clientRepository,
        IWebhookRepository webhookRepository,
        IWebhookService webhookService,
        IUserRepository userRepository,
        HttpClient httpClient,
        ILogger<CampaignMetricsMonitoringService> logger,
        IConfiguration configuration)
    {
        _eventConfigRepository = eventConfigRepository;
        _eventTriggerRepository = eventTriggerRepository;
        _campaignStatsRepository = campaignStatsRepository;
        _emailAccountStatsRepository = emailAccountStatsRepository;
        _campaignRepository = campaignRepository;
        _emailAccountRepository = emailAccountRepository;
        _clientRepository = clientRepository;
        _webhookRepository = webhookRepository;
        _webhookService = webhookService;
        _userRepository = userRepository;
        _httpClient = httpClient;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                           throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task CheckMetricsThresholdsAsync()
    {
        try
        {
            var activeConfigs = await _eventConfigRepository.GetActiveConfigsAsync();
            var replyRateConfigs = activeConfigs.Where(c => c.EventType == "reply_rate_drop").ToList();
            var bounceRateConfigs = activeConfigs.Where(c => c.EventType == "bounce_rate_high").ToList();
            var noPositiveReplyConfigs = activeConfigs.Where(c => c.EventType == "no_positive_reply_for_x_days").ToList();

            _logger.LogInformation("Checking {ReplyRateCount} reply rate, {BounceRateCount} bounce rate, and {NoPositiveReplyCount} no positive reply monitoring configs", 
                replyRateConfigs.Count, bounceRateConfigs.Count, noPositiveReplyConfigs.Count);

            // Check reply rate configs
            foreach (var config in replyRateConfigs)
            {
                try
                {
                    await CheckEventConfigAsync(config);
                    await _eventConfigRepository.UpdateLastCheckedAsync(config.Id, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking reply rate config {ConfigId}", config.Id);
                }
            }

            // Check bounce rate configs
            foreach (var config in bounceRateConfigs)
            {
                try
                {
                    await CheckEventConfigAsync(config);
                    await _eventConfigRepository.UpdateLastCheckedAsync(config.Id, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking bounce rate config {ConfigId}", config.Id);
                }
            }

            // Check no positive reply configs
            foreach (var config in noPositiveReplyConfigs)
            {
                try
                {
                    await CheckEventConfigAsync(config);
                    await _eventConfigRepository.UpdateLastCheckedAsync(config.Id, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking no positive reply config {ConfigId}", config.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in campaign metrics monitoring");
        }
    }

    public async Task CheckEventConfigAsync(WebhookEventConfig config)
    {
        // Handle different parameter validation based on event type
        if (config.EventType == "no_positive_reply_for_x_days")
        {
            await CheckNoPositiveReplyEventAsync(config);
            return;
        }

        if (config.EventType == "no_reply_for_x_days")
        {
            await CheckNoReplyEventAsync(config);
            return;
        }

        // Parse configuration parameters for reply_rate_drop and bounce_rate_high
        var configParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(config.ConfigParameters);
        if (!configParams.TryGetValue("thresholdPercent", out var thresholdObj) ||
            !configParams.TryGetValue("monitoringPeriodDays", out var periodObj))
        {
            _logger.LogWarning("Invalid config parameters for event config {ConfigId}", config.Id);
            return;
        }

        double thresholdPercent;
        int monitoringPeriodDays;
        int minimumEmailsSent = 100; // Default value

        // Extract minimumEmailsSent if available
        if (configParams.TryGetValue("minimumEmailsSent", out var minimumEmailsObj))
        {
            _logger.LogDebug("Found minimumEmailsSent in config {ConfigId}: {Value}", config.Id, minimumEmailsObj);
            if (minimumEmailsObj is Newtonsoft.Json.Linq.JValue minimumEmailsValue && minimumEmailsValue.Value != null)
            {
                minimumEmailsSent = Convert.ToInt32(minimumEmailsValue.Value);
            }
            else if (minimumEmailsObj is Newtonsoft.Json.Linq.JToken minimumEmailsToken)
            {
                if (minimumEmailsToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    minimumEmailsSent = (int)minimumEmailsToken;
                }
                else if (minimumEmailsToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    minimumEmailsSent = Convert.ToInt32((string)minimumEmailsToken);
                }
                else if (minimumEmailsToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    // Handle JsonElement object structure - use default
                    var valueKindToken = minimumEmailsToken["ValueKind"];
                    if (valueKindToken != null)
                    {
                        minimumEmailsSent = 100; // Default minimum emails
                    }
                    else
                    {
                        var valueToken = minimumEmailsToken["Value"];
                        if (valueToken != null)
                        {
                            minimumEmailsSent = (int)valueToken;
                        }
                    }
                }
            }
            else
            {
                minimumEmailsSent = Convert.ToInt32(minimumEmailsObj);
            }
        }
        else
        {
            _logger.LogInformation("Config {ConfigId} doesn't have minimumEmailsSent parameter, using default value: {Default}", config.Id, minimumEmailsSent);
        }
        
        _logger.LogDebug("Config {ConfigId} - Final minimumEmailsSent value: {MinimumEmails}", config.Id, minimumEmailsSent);
        
        // Handle various JSON deserialization formats
        if (thresholdObj is Newtonsoft.Json.Linq.JValue thresholdValue && thresholdValue.Value != null)
        {
            thresholdPercent = Convert.ToDouble(thresholdValue.Value);
        }
        else if (thresholdObj is Newtonsoft.Json.Linq.JToken thresholdToken)
        {
            // Handle different JToken types
            if (thresholdToken.Type == Newtonsoft.Json.Linq.JTokenType.Float || 
                thresholdToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                thresholdPercent = (double)thresholdToken;
            }
            else if (thresholdToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                thresholdPercent = Convert.ToDouble((string)thresholdToken);
            }
            else if (thresholdToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
            {
                // Handle JsonElement object structure serialized by Newtonsoft.Json
                var valueKindToken = thresholdToken["ValueKind"];
                if (valueKindToken != null)
                {
                    // This is a serialized JsonElement - use default values and fix the config
                    _logger.LogWarning("Detected corrupted JsonElement for thresholdPercent in config {ConfigId}. Using default value.", config.Id);
                    thresholdPercent = config.EventType == "reply_rate_drop" ? 10.0 : 20.0;
                    
                    // Schedule async fix for this configuration
                    _ = Task.Run(async () => await FixCorruptedConfigurationAsync(config.Id, config.EventType));
                }
                else
                {
                    // Look for other possible properties
                    var valueToken = thresholdToken["Value"];
                    if (valueToken != null)
                    {
                        thresholdPercent = (double)valueToken;
                    }
                    else
                    {
                        _logger.LogError("JObject thresholdPercent has unknown structure: {Token}", thresholdToken);
                        return;
                    }
                }
            }
            else
            {
                _logger.LogError("Unsupported JToken type for thresholdPercent: {Type}, Value: {Token}", thresholdToken.Type, thresholdToken);
                return;
            }
        }
        else
        {
            thresholdPercent = Convert.ToDouble(thresholdObj);
        }
        
        if (periodObj is Newtonsoft.Json.Linq.JValue periodValue && periodValue.Value != null)
        {
            monitoringPeriodDays = Convert.ToInt32(periodValue.Value);
        }
        else if (periodObj is Newtonsoft.Json.Linq.JToken periodToken)
        {
            // Handle different JToken types
            if (periodToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                monitoringPeriodDays = (int)periodToken;
            }
            else if (periodToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                monitoringPeriodDays = Convert.ToInt32((string)periodToken);
            }
            else if (periodToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
            {
                // Handle JsonElement object structure serialized by Newtonsoft.Json
                var valueKindToken = periodToken["ValueKind"];
                if (valueKindToken != null)
                {
                    // This is a serialized JsonElement - use default values
                    _logger.LogWarning("Detected corrupted JsonElement for monitoringPeriodDays in config {ConfigId}. Using default value.", config.Id);
                    monitoringPeriodDays = 7; // Default monitoring period
                }
                else
                {
                    // Look for other possible properties
                    var valueToken = periodToken["Value"];
                    if (valueToken != null)
                    {
                        monitoringPeriodDays = (int)valueToken;
                    }
                    else
                    {
                        _logger.LogError("JObject monitoringPeriodDays has unknown structure: {Token}", periodToken);
                        return;
                    }
                }
            }
            else
            {
                _logger.LogError("Unsupported JToken type for monitoringPeriodDays: {Type}, Value: {Token}", periodToken.Type, periodToken);
                return;
            }
        }
        else
        {
            monitoringPeriodDays = Convert.ToInt32(periodObj);
        }

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-monitoringPeriodDays);
        var previousEndDate = startDate.AddDays(-1);
        var previousStartDate = previousEndDate.AddDays(-monitoringPeriodDays);

        // Get target campaigns based on scope configuration
        var targetScope = JsonConvert.DeserializeObject<TargetScopeConfig>(config.TargetScope);
        var campaigns = await GetTargetCampaignsAsync(targetScope);

        foreach (var campaign in campaigns)
        {
            try
            {
                // Handle different event types
                switch (config.EventType?.ToLower())
                {
                    case "reply_rate_drop":
                        await CheckCampaignReplyRateAsync(config, campaign, thresholdPercent, monitoringPeriodDays, 
                            minimumEmailsSent, startDate, endDate, previousStartDate, previousEndDate);
                        break;
                    case "bounce_rate_high":
                        await CheckCampaignBounceRateAsync(config, campaign, thresholdPercent, monitoringPeriodDays, 
                            minimumEmailsSent, startDate, endDate, previousStartDate, previousEndDate);
                        break;
                    default:
                        _logger.LogWarning("Unknown event type {EventType} for config {ConfigId}", 
                            config.EventType, config.Id);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking campaign {CampaignId} for config {ConfigId}", 
                    campaign.Id, config.Id);
            }
        }
    }

    private async Task<List<CampaignDetailsDbModel>> GetTargetCampaignsAsync(TargetScopeConfig? targetScope)
    {
        if (targetScope == null || targetScope.Type == null)
        {
            return new List<CampaignDetailsDbModel>();
        }

        try
        {
            if (targetScope.Type.ToLower() == "campaigns")
            {
                var campaigns = new List<CampaignDetailsDbModel>();
                foreach (var campaignId in targetScope.Ids)
                {
                    var campaign = await _campaignRepository.GetByIdAsync(campaignId);
                    if (campaign != null)
                    {
                        campaigns.Add(campaign);
                    }
                }
                return campaigns;
            }
            else if (targetScope.Type.ToLower() == "clients")
            {
                var allCampaigns = new List<CampaignDetailsDbModel>();
                foreach (var clientId in targetScope.Ids)
                {
                    var clientCampaigns = await _campaignRepository.GetByClientIdAsync(clientId);
                    allCampaigns.AddRange(clientCampaigns);
                }
                return allCampaigns;
            }
            else if (targetScope.Type.ToLower() == "users")
            {
                var allCampaigns = new List<CampaignDetailsDbModel>();
                foreach (var userId in targetScope.Ids)
                {
                    try
                    {
                        // Get the user to access their assigned client IDs
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user?.AssignedClientIds != null)
                        {
                            // Get campaigns for all clients assigned to this user
                            foreach (var clientId in user.AssignedClientIds)
                            {
                                var clientCampaigns = await _campaignRepository.GetByClientIdAsync(clientId);
                                allCampaigns.AddRange(clientCampaigns);
                            }
                            
                            _logger.LogDebug("User {UserId} has {ClientCount} assigned clients with {CampaignCount} campaigns total", 
                                userId, user.AssignedClientIds.Count, allCampaigns.Count);
                        }
                        else
                        {
                            _logger.LogWarning("User {UserId} not found or has no assigned clients", userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing user {UserId} for webhook target scope", userId);
                    }
                }
                
                // Remove duplicates (in case multiple users have overlapping client assignments)
                var uniqueCampaigns = allCampaigns
                    .GroupBy(c => c.Id)
                    .Select(g => g.First())
                    .ToList();
                    
                _logger.LogInformation("Resolved {UserCount} users to {CampaignCount} unique campaigns for webhook monitoring", 
                    targetScope.Ids.Count, uniqueCampaigns.Count);
                    
                return uniqueCampaigns;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting target campaigns for scope {Scope}", 
                JsonConvert.SerializeObject(targetScope));
        }

        return new List<CampaignDetailsDbModel>();
    }

    private async Task CheckCampaignReplyRateAsync(WebhookEventConfig config, CampaignDetailsDbModel campaign,
        double thresholdPercent, int monitoringPeriodDays, int minimumEmailsSent, DateTime startDate, DateTime endDate,
        DateTime previousStartDate, DateTime previousEndDate)
    {
        // Get current period stats
        var currentStats = await _campaignStatsRepository.GetAggregatedStatsByCampaignAsync(
            campaign.Id, startDate, endDate);

        // Get previous period stats for comparison
        var previousStats = await _campaignStatsRepository.GetAggregatedStatsByCampaignAsync(
            campaign.Id, previousStartDate, previousEndDate);

        if (currentStats == null || currentStats.Sent == 0)
        {
            return; // No data to analyze
        }

        // Check minimum emails sent threshold
        _logger.LogInformation("REPLY RATE THRESHOLD CHECK: Campaign {CampaignId} - Sent: {Sent}, MinimumRequired: {Minimum}, Check: {Sent} < {Minimum} = {Result}", 
            campaign.Id, currentStats.Sent, minimumEmailsSent, currentStats.Sent, minimumEmailsSent, currentStats.Sent < minimumEmailsSent);
        
        if (currentStats.Sent < minimumEmailsSent)
        {
            _logger.LogInformation("Campaign {CampaignId} (Reply Rate Check) has only sent {Sent} emails, below minimum threshold of {Minimum}. Skipping webhook trigger.", 
                campaign.Id, currentStats.Sent, minimumEmailsSent);
            return;
        }
        
        _logger.LogInformation("Campaign {CampaignId} (Reply Rate Check) meets minimum emails threshold: {Sent} >= {Minimum}. Proceeding with checks.", 
            campaign.Id, currentStats.Sent, minimumEmailsSent);

        var currentReplyRate = (double)currentStats.Replied / currentStats.Sent * 100;
        var previousReplyRate = previousStats != null && previousStats.Sent > 0 
            ? (double)previousStats.Replied / previousStats.Sent * 100 
            : 0;

        // Check if reply rate dropped below threshold
        if (previousReplyRate > 0 && currentReplyRate < previousReplyRate)
        {
            var replyRateDrop = previousReplyRate - currentReplyRate;
            
            if (replyRateDrop >= thresholdPercent)
            {
                // Double-check minimum emails threshold before triggering webhook
                if (currentStats.Sent < minimumEmailsSent)
                {
                    _logger.LogWarning("Campaign {CampaignId} triggered reply rate alert but doesn't meet minimum emails threshold ({Sent} < {Minimum}). This should not happen.", 
                        campaign.Id, currentStats.Sent, minimumEmailsSent);
                    return;
                }
                
                _logger.LogInformation("Reply rate drop detected for campaign {CampaignId}: {Drop}% drop (Sent: {Sent}, Threshold: {Minimum})", 
                    campaign.Id, replyRateDrop, currentStats.Sent, minimumEmailsSent);
                
                _logger.LogInformation("CALLING TriggerWebhookAsync for campaign {CampaignId} with Sent: {Sent}, MinimumRequired: {Minimum}", 
                    campaign.Id, currentStats.Sent, minimumEmailsSent);

                await TriggerWebhookAsync(config, campaign, currentStats, previousStats, 
                    thresholdPercent, monitoringPeriodDays, minimumEmailsSent, startDate, endDate);
            }
        }
    }

    private async Task CheckCampaignBounceRateAsync(WebhookEventConfig config, CampaignDetailsDbModel campaign,
        double thresholdPercent, int monitoringPeriodDays, int minimumEmailsSent, DateTime startDate, DateTime endDate,
        DateTime previousStartDate, DateTime previousEndDate)
    {
        // Get current period stats
        var currentStats = await _campaignStatsRepository.GetAggregatedStatsByCampaignAsync(
            campaign.Id, startDate, endDate);

        // Get previous period stats for comparison (for context, not threshold checking)
        var previousStats = await _campaignStatsRepository.GetAggregatedStatsByCampaignAsync(
            campaign.Id, previousStartDate, previousEndDate);

        if (currentStats == null || currentStats.Sent == 0)
        {
            return; // No data to analyze
        }

        // Check minimum emails sent threshold
        _logger.LogInformation("BOUNCE RATE THRESHOLD CHECK: Campaign {CampaignId} - Sent: {Sent}, MinimumRequired: {Minimum}, Check: {Sent} < {Minimum} = {Result}", 
            campaign.Id, currentStats.Sent, minimumEmailsSent, currentStats.Sent, minimumEmailsSent, currentStats.Sent < minimumEmailsSent);
        
        if (currentStats.Sent < minimumEmailsSent)
        {
            _logger.LogInformation("Campaign {CampaignId} (Bounce Rate Check) has only sent {Sent} emails, below minimum threshold of {Minimum}. Skipping webhook trigger.", 
                campaign.Id, currentStats.Sent, minimumEmailsSent);
            return;
        }
        
        _logger.LogInformation("Campaign {CampaignId} (Bounce Rate Check) meets minimum emails threshold: {Sent} >= {Minimum}. Proceeding with checks.", 
            campaign.Id, currentStats.Sent, minimumEmailsSent);

        var currentBounceRate = (double)currentStats.Bounced / currentStats.Sent * 100;
        var previousBounceRate = previousStats != null && previousStats.Sent > 0 
            ? (double)previousStats.Bounced / previousStats.Sent * 100 
            : 0;

        // Check if bounce rate exceeds threshold
        if (currentBounceRate >= thresholdPercent)
        {
            // Double-check minimum emails threshold before triggering webhook
            if (currentStats.Sent < minimumEmailsSent)
            {
                _logger.LogWarning("Campaign {CampaignId} triggered bounce rate alert but doesn't meet minimum emails threshold ({Sent} < {Minimum}). This should not happen.", 
                    campaign.Id, currentStats.Sent, minimumEmailsSent);
                return;
            }
            
            _logger.LogInformation("High bounce rate detected for campaign {CampaignId}: {BounceRate}% (threshold: {Threshold}%, Sent: {Sent}, Minimum: {Minimum})", 
                campaign.Id, currentBounceRate, thresholdPercent, currentStats.Sent, minimumEmailsSent);
            
            _logger.LogInformation("CALLING TriggerBounceRateWebhookAsync for campaign {CampaignId} with Sent: {Sent}, MinimumRequired: {Minimum}", 
                campaign.Id, currentStats.Sent, minimumEmailsSent);

            await TriggerBounceRateWebhookAsync(config, campaign, currentStats, previousStats, 
                thresholdPercent, monitoringPeriodDays, minimumEmailsSent, startDate, endDate);
        }
    }

    private async Task TriggerWebhookAsync(WebhookEventConfig config, CampaignDetailsDbModel campaign,
        CampaignDailyStatEntry currentStats, CampaignDailyStatEntry? previousStats,
        double thresholdPercent, int monitoringPeriodDays, int minimumEmailsSent, DateTime startDate, DateTime endDate)
    {
        try
        {
            // CRITICAL SAFETY CHECK: Absolutely prevent webhook if minimum emails threshold not met
            if (currentStats.Sent < minimumEmailsSent)
            {
                _logger.LogError("CRITICAL: TriggerWebhookAsync called with insufficient emails! Campaign {CampaignId} sent {Sent} < minimum {Minimum}. This is a bug - webhook should not be triggered.", 
                    campaign.Id, currentStats.Sent, minimumEmailsSent);
                return;
            }
            
            // Get webhook details
            var webhook = await _webhookRepository.GetByIdAsync(config.WebhookId);
            if (webhook == null || !webhook.IsActive)
            {
                _logger.LogWarning("Webhook {WebhookId} not found or inactive", config.WebhookId);
                return;
            }

            // Analyze email account impact
            var emailAccountImpacts = await AnalyzeEmailAccountImpactAsync(campaign, startDate, endDate, minimumEmailsSent);

            // Create webhook payload
            var currentReplyRate = (double)currentStats.Replied / currentStats.Sent * 100;
            var previousReplyRate = previousStats != null && previousStats.Sent > 0 
                ? (double)previousStats.Replied / previousStats.Sent * 100 
                : 0;

            var payload = new ReplyRateDropWebhookPayload
            {
                EventConfigId = config.Id,
                EventConfigName = config.Name,
                Campaign = new ReplyRateDropWebhookPayload.CampaignMetrics
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    ClientId = campaign.ClientId ?? "",
                    ClientName = campaign.ClientName ?? "",
                    CurrentReplyRate = Math.Round(currentReplyRate, 2),
                    PreviousReplyRate = Math.Round(previousReplyRate, 2),
                    ReplyRateDrop = Math.Round(previousReplyRate - currentReplyRate, 2),
                    TotalSent7Days = currentStats.Sent,
                    TotalReplied7Days = currentStats.Replied
                },
                AffectedEmailAccounts = emailAccountImpacts,
                Threshold = new ReplyRateDropWebhookPayload.ThresholdDetails
                {
                    ThresholdPercent = thresholdPercent,
                    MonitoringPeriodDays = monitoringPeriodDays,
                    MinimumEmailsSent = minimumEmailsSent,
                    PeriodStart = startDate,
                    PeriodEnd = endDate
                }
            };

            // Create trigger log entry
            var trigger = new WebhookEventTrigger
            {
                EventConfigId = config.Id,
                WebhookId = config.WebhookId,
                CampaignId = campaign.Id,
                CampaignName = campaign.Name,
                TriggerData = JsonConvert.SerializeObject(payload)
            };

            // Use WebhookService to send webhook and log delivery
            await _webhookService.TriggerWebhookByIdAsync(config.WebhookId, "campaign.reply_rate_drop", payload);

            // Always mark as triggered and save trigger log since WebhookService handles delivery
            await _eventConfigRepository.UpdateLastTriggeredAsync(config.Id, DateTime.UtcNow);
            await _eventTriggerRepository.CreateAsync(trigger);

            _logger.LogInformation("Webhook triggered for campaign {CampaignId}, config {ConfigId}", 
                campaign.Id, config.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering webhook for campaign {CampaignId}", campaign.Id);
        }
    }

    private async Task<List<ReplyRateDropWebhookPayload.EmailAccountImpact>> AnalyzeEmailAccountImpactAsync(
        CampaignDetailsDbModel campaign, DateTime startDate, DateTime endDate, int minimumEmailsSent)
    {
        var impacts = new List<ReplyRateDropWebhookPayload.EmailAccountImpact>();

        try
        {
            // Get email accounts for this campaign
            var emailAccountIds = campaign.EmailIds ?? new List<long>();
            
            if (!emailAccountIds.Any())
                return impacts;

            // Get email account stats for the period
            var emailAccountStats = await _emailAccountStatsRepository.GetByEmailAccountIdsAndDateRangeAsync(
                emailAccountIds.ToList(), startDate, endDate);

            var statsGrouped = emailAccountStats
                .GroupBy(s => s.EmailAccountId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var emailAccountId in emailAccountIds)
            {
                if (!statsGrouped.TryGetValue(emailAccountId, out var stats))
                    continue;

                var totalSent = stats.Sum(s => s.Sent);
                var totalReplied = stats.Sum(s => s.Replied);

                if (totalSent == 0) continue;

                // Check minimum emails sent threshold for individual email accounts
                if (totalSent < minimumEmailsSent)
                {
                    _logger.LogDebug("Email account {EmailAccountId} has only sent {Sent} emails, below minimum threshold of {Minimum}. Excluding from impact analysis.", 
                        emailAccountId, totalSent, minimumEmailsSent);
                    continue;
                }

                var replyRate = (double)totalReplied / totalSent * 100;

                // Get email account details
                var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountId);

                // Determine impact level based on volume and reply rate
                var impactLevel = DetermineImpactLevel(totalSent, replyRate, totalSent);

                impacts.Add(new ReplyRateDropWebhookPayload.EmailAccountImpact
                {
                    EmailAccountId = emailAccountId,
                    EmailAddress = emailAccount?.Email ?? $"ID:{emailAccountId}",
                    ReplyRate7Days = Math.Round(replyRate, 2),
                    Sent7Days = totalSent,
                    Replied7Days = totalReplied,
                    ImpactLevel = impactLevel
                });
            }

            // Sort by impact level and then by volume
            impacts = impacts
                .OrderBy(i => i.ImpactLevel == "High" ? 0 : i.ImpactLevel == "Medium" ? 1 : 2)
                .ThenByDescending(i => i.Sent7Days)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing email account impact for campaign {CampaignId}", campaign.Id);
        }

        return impacts;
    }

    private string DetermineImpactLevel(int sent, double replyRate, int totalCampaignSent)
    {
        var volumePercent = totalCampaignSent > 0 ? (double)sent / totalCampaignSent * 100 : 0;

        // High impact: Low reply rate + high volume
        if (replyRate < 2.0 && volumePercent > 20)
            return "High";

        // High impact: Very low reply rate regardless of volume
        if (replyRate < 1.0)
            return "High";

        // Medium impact: Low reply rate or medium volume with poor performance
        if (replyRate < 3.0 || (volumePercent > 10 && replyRate < 5.0))
            return "Medium";

        return "Low";
    }


    private async Task TriggerBounceRateWebhookAsync(WebhookEventConfig config, CampaignDetailsDbModel campaign,
        CampaignDailyStatEntry currentStats, CampaignDailyStatEntry? previousStats,
        double thresholdPercent, int monitoringPeriodDays, int minimumEmailsSent, DateTime startDate, DateTime endDate)
    {
        try
        {
            // CRITICAL SAFETY CHECK: Absolutely prevent webhook if minimum emails threshold not met
            if (currentStats.Sent < minimumEmailsSent)
            {
                _logger.LogError("CRITICAL: TriggerBounceRateWebhookAsync called with insufficient emails! Campaign {CampaignId} sent {Sent} < minimum {Minimum}. This is a bug - webhook should not be triggered.", 
                    campaign.Id, currentStats.Sent, minimumEmailsSent);
                return;
            }
            
            // Get webhook details
            var webhook = await _webhookRepository.GetByIdAsync(config.WebhookId);
            if (webhook == null || !webhook.IsActive)
            {
                _logger.LogWarning("Webhook {WebhookId} not found or inactive", config.WebhookId);
                return;
            }

            // Analyze email account impact for bounce rates
            var emailAccountImpacts = await AnalyzeBounceRateEmailAccountImpactAsync(campaign, startDate, endDate, minimumEmailsSent);

            // Create webhook payload
            var currentBounceRate = (double)currentStats.Bounced / currentStats.Sent * 100;
            var previousBounceRate = previousStats != null && previousStats.Sent > 0 
                ? (double)previousStats.Bounced / previousStats.Sent * 100 
                : 0;

            var payload = new BounceRateHighWebhookPayload
            {
                EventConfigId = config.Id,
                EventConfigName = config.Name,
                Campaign = new BounceRateHighWebhookPayload.CampaignMetrics
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    ClientId = campaign.ClientId ?? "",
                    ClientName = campaign.ClientName ?? "",
                    CurrentBounceRate = Math.Round(currentBounceRate, 2),
                    PreviousBounceRate = Math.Round(previousBounceRate, 2),
                    BounceRateIncrease = Math.Round(currentBounceRate - previousBounceRate, 2),
                    TotalSent7Days = currentStats.Sent,
                    TotalBounced7Days = currentStats.Bounced
                },
                AffectedEmailAccounts = emailAccountImpacts,
                Threshold = new BounceRateHighWebhookPayload.ThresholdDetails
                {
                    ThresholdPercent = thresholdPercent,
                    MonitoringPeriodDays = monitoringPeriodDays,
                    MinimumEmailsSent = minimumEmailsSent,
                    PeriodStart = startDate,
                    PeriodEnd = endDate
                }
            };

            // Create trigger log entry
            var trigger = new WebhookEventTrigger
            {
                EventConfigId = config.Id,
                WebhookId = config.WebhookId,
                CampaignId = campaign.Id,
                CampaignName = campaign.Name,
                TriggerData = JsonConvert.SerializeObject(payload)
            };

            // Use WebhookService to send webhook and log delivery
            await _webhookService.TriggerWebhookByIdAsync(config.WebhookId, "campaign.bounce_rate_high", payload);

            // Always mark as triggered and save trigger log since WebhookService handles delivery
            await _eventConfigRepository.UpdateLastTriggeredAsync(config.Id, DateTime.UtcNow);
            await _eventTriggerRepository.CreateAsync(trigger);

            _logger.LogInformation("Bounce rate webhook triggered for campaign {CampaignId}, config {ConfigId}", 
                campaign.Id, config.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering bounce rate webhook for campaign {CampaignId}", campaign.Id);
        }
    }

    private async Task<List<BounceRateHighWebhookPayload.EmailAccountImpact>> AnalyzeBounceRateEmailAccountImpactAsync(
        CampaignDetailsDbModel campaign, DateTime startDate, DateTime endDate, int minimumEmailsSent)
    {
        var impacts = new List<BounceRateHighWebhookPayload.EmailAccountImpact>();

        if (campaign.EmailIds == null || !campaign.EmailIds.Any())
        {
            return impacts;
        }

        try
        {
            foreach (var emailId in campaign.EmailIds)
            {
                var emailAccount = await _emailAccountRepository.GetByIdAsync(emailId);
                if (emailAccount == null) continue;

                var stats = await _emailAccountStatsRepository.GetAggregatedStatsByEmailAccountAsync(
                    emailId, startDate, endDate);

                if (stats == null || stats.Sent == 0) continue;

                // Check minimum emails sent threshold for individual email accounts
                if (stats.Sent < minimumEmailsSent)
                {
                    _logger.LogDebug("Email account {EmailAccountId} has only sent {Sent} emails, below minimum threshold of {Minimum}. Excluding from bounce rate impact analysis.", 
                        emailId, stats.Sent, minimumEmailsSent);
                    continue;
                }

                var bounceRate = (double)stats.Bounced / stats.Sent * 100;
                
                var impact = new BounceRateHighWebhookPayload.EmailAccountImpact
                {
                    EmailAccountId = emailId,
                    EmailAddress = emailAccount.Email,
                    BounceRate7Days = Math.Round(bounceRate, 2),
                    Sent7Days = stats.Sent,
                    Bounced7Days = stats.Bounced,
                    ImpactLevel = bounceRate >= 10 ? "High" : bounceRate >= 5 ? "Medium" : "Low"
                };

                impacts.Add(impact);
            }

            return impacts.OrderByDescending(x => x.BounceRate7Days).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing bounce rate email account impact for campaign {CampaignId}", campaign.Id);
            return impacts;
        }
    }



    private async Task CheckNoPositiveReplyEventAsync(WebhookEventConfig config)
    {
        try
        {
            // Parse configuration parameters for no positive reply event
            var configParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(config.ConfigParameters);
            if (!configParams.TryGetValue("daysSinceLastReply", out var daysObj))
            {
                _logger.LogWarning("Invalid config parameters for no positive reply event config {ConfigId}: missing daysSinceLastReply", config.Id);
                return;
            }

            int daysSinceLastReply;
            
            // Handle various JSON deserialization formats
            if (daysObj is Newtonsoft.Json.Linq.JValue daysValue && daysValue.Value != null)
            {
                daysSinceLastReply = Convert.ToInt32(daysValue.Value);
            }
            else if (daysObj is Newtonsoft.Json.Linq.JToken daysToken)
            {
                if (daysToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    daysSinceLastReply = (int)daysToken;
                }
                else if (daysToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    daysSinceLastReply = Convert.ToInt32((string)daysToken);
                }
                else
                {
                    _logger.LogError("Unsupported JToken type for daysSinceLastReply: {Type}, Value: {Token}", daysToken.Type, daysToken);
                    return;
                }
            }
            else
            {
                daysSinceLastReply = Convert.ToInt32(daysObj);
            }

            var checkDate = DateTime.UtcNow.Date;
            var thresholdDate = checkDate.AddDays(-daysSinceLastReply);

            // Get target campaigns based on scope configuration
            var targetScope = JsonConvert.DeserializeObject<TargetScopeConfig>(config.TargetScope);
            var campaigns = await GetTargetCampaignsAsync(targetScope);

            var affectedCampaigns = new List<NoPositiveReplyForXDaysWebhookPayload.CampaignNoReplyInfo>();

            foreach (var campaign in campaigns)
            {
                try
                {
                    var campaignInfo = await CheckCampaignForNoPositiveReplyAsync(campaign, thresholdDate);
                    if (campaignInfo != null)
                    {
                        affectedCampaigns.Add(campaignInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking campaign {CampaignId} for no positive reply", campaign.Id);
                }
            }

            // Trigger webhook if any campaigns are affected
            if (affectedCampaigns.Any())
            {
                await TriggerNoPositiveReplyWebhookAsync(config, affectedCampaigns, daysSinceLastReply, checkDate, thresholdDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckNoPositiveReplyEventAsync for config {ConfigId}", config.Id);
        }
    }

    private async Task<NoPositiveReplyForXDaysWebhookPayload.CampaignNoReplyInfo?> CheckCampaignForNoPositiveReplyAsync(
        CampaignDetailsDbModel campaign, DateTime thresholdDate)
    {
        try
        {
            // Query to find last positive reply date for this campaign
            using var connection = new NpgsqlConnection(_connectionString);
            
            const string sql = @"
                SELECT MAX(leh.sent_at) as last_positive_reply_date,
                       COUNT(leh.id) as total_replies_in_period,
                       SUM(CASE WHEN leh.is_positive_reply = true THEN 1 ELSE 0 END) as positive_replies_in_period
                FROM lead_email_history leh
                WHERE leh.campaign_id = @CampaignId 
                  AND leh.is_reply = true 
                  AND leh.is_positive_reply = true
                  AND leh.sent_at >= @ThresholdDate";

            var result = await connection.QuerySingleOrDefaultAsync(sql, new 
            { 
                CampaignId = campaign.Id, 
                ThresholdDate = thresholdDate 
            });

            var lastPositiveReplyDate = result?.last_positive_reply_date as DateTime?;
            var totalRepliesInPeriod = result?.total_replies_in_period ?? 0;
            var positiveRepliesInPeriod = result?.positive_replies_in_period ?? 0;

            // If there have been positive replies within the threshold period, this campaign is fine
            if (lastPositiveReplyDate.HasValue && lastPositiveReplyDate >= thresholdDate)
            {
                return null;
            }

            // Calculate business days since last positive reply (excluding weekends)
            int daysSinceLastPositiveReply = lastPositiveReplyDate.HasValue 
                ? CalculateBusinessDaysSince(lastPositiveReplyDate.Value)
                : int.MaxValue; // Never had a positive reply

            // Get total sent count for context
            const string sentCountSql = @"
                SELECT COUNT(*) as total_sent
                FROM lead_email_history leh
                WHERE leh.campaign_id = @CampaignId 
                  AND leh.is_reply = false 
                  AND leh.sent_at >= @ThresholdDate";

            var sentResult = await connection.QuerySingleOrDefaultAsync<int>(sentCountSql, new 
            { 
                CampaignId = campaign.Id, 
                ThresholdDate = thresholdDate 
            });

            // Get email account information for this campaign
            var emailAccountInfo = await GetEmailAccountNoReplyInfoAsync(campaign, thresholdDate);

            return new NoPositiveReplyForXDaysWebhookPayload.CampaignNoReplyInfo
            {
                Id = campaign.Id,
                Name = campaign.Name,
                ClientId = campaign.ClientId ?? "",
                ClientName = campaign.ClientName ?? "",
                LastPositiveReplyDate = lastPositiveReplyDate,
                DaysSinceLastPositiveReply = daysSinceLastPositiveReply,
                TotalSentInPeriod = sentResult,
                TotalRepliesInPeriod = totalRepliesInPeriod,
                PositiveRepliesInPeriod = positiveRepliesInPeriod,
                EmailAccounts = emailAccountInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking campaign {CampaignId} for no positive replies", campaign.Id);
            return null;
        }
    }

    private async Task<List<NoPositiveReplyForXDaysWebhookPayload.EmailAccountNoReplyInfo>> GetEmailAccountNoReplyInfoAsync(
        CampaignDetailsDbModel campaign, DateTime thresholdDate)
    {
        var emailAccountInfo = new List<NoPositiveReplyForXDaysWebhookPayload.EmailAccountNoReplyInfo>();

        if (campaign.EmailIds == null || !campaign.EmailIds.Any())
        {
            return emailAccountInfo;
        }

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);

            foreach (var emailAccountId in campaign.EmailIds)
            {
                const string sql = @"
                    SELECT ea.email,
                           MAX(CASE WHEN leh.is_positive_reply = true THEN leh.sent_at END) as last_positive_reply_date,
                           COUNT(CASE WHEN leh.is_reply = false THEN leh.id END) as sent_in_period,
                           COUNT(CASE WHEN leh.is_reply = true THEN leh.id END) as replies_in_period,
                           SUM(CASE WHEN leh.is_positive_reply = true THEN 1 ELSE 0 END) as positive_replies_in_period
                    FROM email_accounts ea
                    LEFT JOIN lead_email_history leh ON leh.email_account_id = ea.id 
                                                      AND leh.campaign_id = @CampaignId 
                                                      AND leh.sent_at >= @ThresholdDate
                    WHERE ea.id = @EmailAccountId
                    GROUP BY ea.email";

                var result = await connection.QuerySingleOrDefaultAsync(sql, new 
                { 
                    CampaignId = campaign.Id, 
                    EmailAccountId = emailAccountId,
                    ThresholdDate = thresholdDate 
                });

                if (result != null)
                {
                    var lastPositiveReplyDate = result.last_positive_reply_date as DateTime?;
                    int daysSinceLastPositiveReply = lastPositiveReplyDate.HasValue 
                        ? CalculateBusinessDaysSince(lastPositiveReplyDate.Value)
                        : int.MaxValue;

                    emailAccountInfo.Add(new NoPositiveReplyForXDaysWebhookPayload.EmailAccountNoReplyInfo
                    {
                        EmailAccountId = emailAccountId,
                        EmailAddress = result.email ?? $"ID:{emailAccountId}",
                        LastPositiveReplyDate = lastPositiveReplyDate,
                        DaysSinceLastPositiveReply = daysSinceLastPositiveReply,
                        SentInPeriod = result.sent_in_period ?? 0,
                        RepliesInPeriod = result.replies_in_period ?? 0,
                        PositiveRepliesInPeriod = result.positive_replies_in_period ?? 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account info for campaign {CampaignId}", campaign.Id);
        }

        return emailAccountInfo;
    }

    private async Task TriggerNoPositiveReplyWebhookAsync(WebhookEventConfig config, 
        List<NoPositiveReplyForXDaysWebhookPayload.CampaignNoReplyInfo> affectedCampaigns,
        int daysSinceLastReply, DateTime checkDate, DateTime thresholdDate)
    {
        try
        {
            // Get webhook details
            var webhook = await _webhookRepository.GetByIdAsync(config.WebhookId);
            if (webhook == null || !webhook.IsActive)
            {
                _logger.LogWarning("Webhook {WebhookId} not found or inactive", config.WebhookId);
                return;
            }

            // Create webhook payload
            var payload = new NoPositiveReplyForXDaysWebhookPayload
            {
                EventConfigId = config.Id,
                EventConfigName = config.Name,
                AffectedCampaigns = affectedCampaigns,
                Threshold = new NoPositiveReplyForXDaysWebhookPayload.NoReplyThresholdDetails
                {
                    DaysSinceLastReply = daysSinceLastReply,
                    CheckDate = checkDate,
                    ThresholdDate = thresholdDate
                }
            };

            // Create trigger log entry for the first affected campaign (or create one for each?)
            var primaryCampaign = affectedCampaigns.First();
            var trigger = new WebhookEventTrigger
            {
                EventConfigId = config.Id,
                WebhookId = config.WebhookId,
                CampaignId = primaryCampaign.Id,
                CampaignName = primaryCampaign.Name,
                TriggerData = JsonConvert.SerializeObject(payload)
            };

            // Use WebhookService to send webhook and log delivery
            await _webhookService.TriggerWebhookByIdAsync(config.WebhookId, "campaign.no_positive_reply", payload);

            // Mark as triggered and save trigger log
            await _eventConfigRepository.UpdateLastTriggeredAsync(config.Id, DateTime.UtcNow);
            await _eventTriggerRepository.CreateAsync(trigger);

            _logger.LogInformation("No positive reply webhook triggered for {CampaignCount} campaigns, config {ConfigId}", 
                affectedCampaigns.Count, config.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering no positive reply webhook for config {ConfigId}", config.Id);
        }
    }

    private async Task CheckNoReplyEventAsync(WebhookEventConfig config)
    {
        try
        {
            // Parse configuration parameters for no reply event
            var configParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(config.ConfigParameters);
            if (!configParams.TryGetValue("daysSinceLastReply", out var daysObj))
            {
                _logger.LogWarning("Invalid config parameters for no reply event config {ConfigId}: missing daysSinceLastReply", config.Id);
                return;
            }

            int daysSinceLastReply;
            
            // Handle various JSON deserialization formats
            if (daysObj is Newtonsoft.Json.Linq.JValue daysValue && daysValue.Value != null)
            {
                daysSinceLastReply = Convert.ToInt32(daysValue.Value);
            }
            else if (daysObj is Newtonsoft.Json.Linq.JToken daysToken)
            {
                if (daysToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    daysSinceLastReply = (int)daysToken;
                }
                else if (daysToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    daysSinceLastReply = Convert.ToInt32((string)daysToken);
                }
                else
                {
                    _logger.LogError("Unsupported JToken type for daysSinceLastReply: {Type}, Value: {Token}", daysToken.Type, daysToken);
                    return;
                }
            }
            else
            {
                daysSinceLastReply = Convert.ToInt32(daysObj);
            }

            var checkDate = DateTime.UtcNow.Date;
            var thresholdDate = checkDate.AddDays(-daysSinceLastReply);

            // Get target campaigns based on scope configuration
            var targetScope = JsonConvert.DeserializeObject<TargetScopeConfig>(config.TargetScope);
            var campaigns = await GetTargetCampaignsAsync(targetScope);

            var affectedCampaigns = new List<NoReplyForXDaysWebhookPayload.CampaignNoReplyInfo>();

            foreach (var campaign in campaigns)
            {
                try
                {
                    var campaignInfo = await CheckCampaignForNoReplyAsync(campaign, thresholdDate);
                    if (campaignInfo != null)
                    {
                        affectedCampaigns.Add(campaignInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking campaign {CampaignId} for no reply", campaign.Id);
                }
            }

            // Trigger webhook if any campaigns are affected
            if (affectedCampaigns.Any())
            {
                await TriggerNoReplyWebhookAsync(config, affectedCampaigns, daysSinceLastReply, checkDate, thresholdDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckNoReplyEventAsync for config {ConfigId}", config.Id);
        }
    }

    private async Task<NoReplyForXDaysWebhookPayload.CampaignNoReplyInfo?> CheckCampaignForNoReplyAsync(
        CampaignDetailsDbModel campaign, DateTime thresholdDate)
    {
        try
        {
            // Query to find last reply date (any reply, not just positive) for this campaign
            using var connection = new NpgsqlConnection(_connectionString);
            
            const string sql = @"
                SELECT MAX(leh.sent_at) as last_reply_date,
                       COUNT(leh.id) as total_replies_in_period
                FROM lead_email_history leh
                WHERE leh.campaign_id = @CampaignId 
                  AND leh.is_reply = true 
                  AND leh.sent_at >= @ThresholdDate";

            var result = await connection.QuerySingleOrDefaultAsync(sql, new 
            { 
                CampaignId = campaign.Id, 
                ThresholdDate = thresholdDate 
            });

            var lastReplyDate = result?.last_reply_date as DateTime?;
            var totalRepliesInPeriod = result?.total_replies_in_period ?? 0;

            // If there have been replies within the threshold period, this campaign is fine
            if (lastReplyDate.HasValue && lastReplyDate >= thresholdDate)
            {
                return null;
            }

            // Calculate business days since last reply (excluding weekends)
            int daysSinceLastReply = lastReplyDate.HasValue 
                ? CalculateBusinessDaysSince(lastReplyDate.Value)
                : int.MaxValue; // Never had a reply

            // Get total sent count for context
            const string sentCountSql = @"
                SELECT COUNT(*) as total_sent
                FROM lead_email_history leh
                WHERE leh.campaign_id = @CampaignId 
                  AND leh.is_reply = false 
                  AND leh.sent_at >= @ThresholdDate";

            var sentResult = await connection.QuerySingleOrDefaultAsync<int>(sentCountSql, new 
            { 
                CampaignId = campaign.Id, 
                ThresholdDate = thresholdDate 
            });

            // Get email account information for this campaign
            var emailAccountInfo = await GetEmailAccountGeneralNoReplyInfoAsync(campaign, thresholdDate);

            return new NoReplyForXDaysWebhookPayload.CampaignNoReplyInfo
            {
                Id = campaign.Id,
                Name = campaign.Name,
                ClientId = campaign.ClientId ?? "",
                ClientName = campaign.ClientName ?? "",
                LastReplyDate = lastReplyDate,
                DaysSinceLastReply = daysSinceLastReply,
                TotalSentInPeriod = sentResult,
                TotalRepliesInPeriod = totalRepliesInPeriod,
                EmailAccounts = emailAccountInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking campaign {CampaignId} for no replies", campaign.Id);
            return null;
        }
    }

    private async Task<List<NoReplyForXDaysWebhookPayload.EmailAccountNoReplyInfo>> GetEmailAccountGeneralNoReplyInfoAsync(
        CampaignDetailsDbModel campaign, DateTime thresholdDate)
    {
        var emailAccountInfo = new List<NoReplyForXDaysWebhookPayload.EmailAccountNoReplyInfo>();

        if (campaign.EmailIds == null || !campaign.EmailIds.Any())
        {
            return emailAccountInfo;
        }

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);

            foreach (var emailAccountId in campaign.EmailIds)
            {
                const string sql = @"
                    SELECT ea.email,
                           MAX(CASE WHEN leh.is_reply = true THEN leh.sent_at END) as last_reply_date,
                           COUNT(CASE WHEN leh.is_reply = false THEN leh.id END) as sent_in_period,
                           COUNT(CASE WHEN leh.is_reply = true THEN leh.id END) as replies_in_period
                    FROM email_accounts ea
                    LEFT JOIN lead_email_history leh ON leh.email_account_id = ea.id 
                                                      AND leh.campaign_id = @CampaignId 
                                                      AND leh.sent_at >= @ThresholdDate
                    WHERE ea.id = @EmailAccountId
                    GROUP BY ea.email";

                var result = await connection.QuerySingleOrDefaultAsync(sql, new 
                { 
                    CampaignId = campaign.Id, 
                    EmailAccountId = emailAccountId,
                    ThresholdDate = thresholdDate 
                });

                if (result != null)
                {
                    var lastReplyDate = result.last_reply_date as DateTime?;
                    int daysSinceLastReply = lastReplyDate.HasValue 
                        ? CalculateBusinessDaysSince(lastReplyDate.Value)
                        : int.MaxValue;

                    emailAccountInfo.Add(new NoReplyForXDaysWebhookPayload.EmailAccountNoReplyInfo
                    {
                        EmailAccountId = emailAccountId,
                        EmailAddress = result.email ?? $"ID:{emailAccountId}",
                        LastReplyDate = lastReplyDate,
                        DaysSinceLastReply = daysSinceLastReply,
                        SentInPeriod = result.sent_in_period ?? 0,
                        RepliesInPeriod = result.replies_in_period ?? 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account info for campaign {CampaignId}", campaign.Id);
        }

        return emailAccountInfo;
    }

    private async Task TriggerNoReplyWebhookAsync(WebhookEventConfig config, 
        List<NoReplyForXDaysWebhookPayload.CampaignNoReplyInfo> affectedCampaigns,
        int daysSinceLastReply, DateTime checkDate, DateTime thresholdDate)
    {
        try
        {
            // Get webhook details
            var webhook = await _webhookRepository.GetByIdAsync(config.WebhookId);
            if (webhook == null || !webhook.IsActive)
            {
                _logger.LogWarning("Webhook {WebhookId} not found or inactive", config.WebhookId);
                return;
            }

            // Create webhook payload
            var payload = new NoReplyForXDaysWebhookPayload
            {
                EventConfigId = config.Id,
                EventConfigName = config.Name,
                AffectedCampaigns = affectedCampaigns,
                Threshold = new NoReplyForXDaysWebhookPayload.NoReplyThresholdDetails
                {
                    DaysSinceLastReply = daysSinceLastReply,
                    CheckDate = checkDate,
                    ThresholdDate = thresholdDate
                }
            };

            // Create trigger log entry for the first affected campaign
            var primaryCampaign = affectedCampaigns.First();
            var trigger = new WebhookEventTrigger
            {
                EventConfigId = config.Id,
                WebhookId = config.WebhookId,
                CampaignId = primaryCampaign.Id,
                CampaignName = primaryCampaign.Name,
                TriggerData = JsonConvert.SerializeObject(payload)
            };

            // Use WebhookService to send webhook and log delivery
            await _webhookService.TriggerWebhookByIdAsync(config.WebhookId, "campaign.no_reply", payload);

            // Mark as triggered and save trigger log
            await _eventConfigRepository.UpdateLastTriggeredAsync(config.Id, DateTime.UtcNow);
            await _eventTriggerRepository.CreateAsync(trigger);

            _logger.LogInformation("No reply webhook triggered for {CampaignCount} campaigns, config {ConfigId}", 
                affectedCampaigns.Count, config.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering no reply webhook for config {ConfigId}", config.Id);
        }
    }

    private async Task FixCorruptedConfigurationAsync(string configId, string eventType)
    {
        try
        {
            var defaultParams = eventType == "reply_rate_drop" 
                ? new { thresholdPercent = 10.0, monitoringPeriodDays = 7, minimumEmailsSent = 100 }
                : new { thresholdPercent = 20.0, monitoringPeriodDays = 14, minimumEmailsSent = 100 };

            var json = JsonConvert.SerializeObject(defaultParams);
            
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "UPDATE webhook_event_configs SET config_parameters = @ConfigParameters::jsonb, updated_at = NOW() WHERE id = @Id";
            
            await connection.ExecuteAsync(sql, new { ConfigParameters = json, Id = configId });
            _logger.LogInformation("Fixed corrupted configuration for webhook event config {ConfigId}", configId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fix corrupted configuration for webhook event config {ConfigId}", configId);
        }
    }

    /// <summary>
    /// Calculate business days between two dates, excluding weekends (Saturday & Sunday)
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (exclusive)</param>
    /// <returns>Number of business days</returns>
    private static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            return 0;

        int businessDays = 0;
        var currentDate = startDate.Date;

        while (currentDate < endDate.Date)
        {
            // Skip weekends (Saturday = 6, Sunday = 0)
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return businessDays;
    }

    /// <summary>
    /// Calculate business days since a given date, excluding weekends
    /// </summary>
    /// <param name="fromDate">The date to calculate from</param>
    /// <returns>Number of business days since the date</returns>
    private static int CalculateBusinessDaysSince(DateTime fromDate)
    {
        return CalculateBusinessDays(fromDate.Date, DateTime.UtcNow.Date);
    }
}