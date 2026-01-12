using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Models;
using LeadHype.Api.Core.Models.API.Requests;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Core.Models.API.Smartlead;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Services
{
    public interface ICampaignService
    {
        Task CalculateTimeRangeStats(List<CampaignDetailsDbModel> campaigns, int timeRangeDays);
        Task<CampaignDetailsDbModel?> GetCampaignWithStatsAsync(string campaignId, int? timeRangeDays = null);
        Task<Dictionary<string, object>> GetCampaignAnalyticsAsync(string campaignId);
        Task<bool> SyncCampaignFromSmartleadAsync(string campaignId);
        Task<int?> CreateCampaignAsync(CreateCampaignV1Request request, int clientId);
        Task<bool> ConfigureCampaignSequenceAsync(int campaignId, ConfigureSequenceV1Request request);
        Task<LeadUploadResult> UploadLeadsAsync(int campaignId, List<LeadInput> leads);
    }

    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ICampaignDailyStatEntryRepository _dailyStatsRepository;
        private readonly ILogger<CampaignService> _logger;
        private readonly SmartleadApiService _smartleadApiService;

        public CampaignService(
            ICampaignRepository campaignRepository,
            ICampaignDailyStatEntryRepository dailyStatsRepository,
            ILogger<CampaignService> logger)
        {
            _campaignRepository = campaignRepository;
            _dailyStatsRepository = dailyStatsRepository;
            _logger = logger;
            // Initialize SmartleadApiService with API key - this should ideally come from configuration
            _smartleadApiService = new SmartleadApiService("c38e38e8-d7b6-4c00-836d-3437678ef4d9_dtw3zft");
        }

        public async Task CalculateTimeRangeStats(List<CampaignDetailsDbModel> campaigns, int timeRangeDays)
        {
            if (!campaigns.Any())
            {
                return;
            }

            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-timeRangeDays);
            
            // Get campaign IDs for batch processing
            var campaignIds = campaigns.Select(c => c.Id).ToList();
            
            // Fetch all daily stats for the campaigns within the date range
            var dailyStats = await _dailyStatsRepository.GetByCampaignIdsAndDateRangeAsync(
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

            _logger.LogDebug($"Calculated time-range stats for {campaigns.Count} campaigns (range: {timeRangeDays} days)");
        }

        public async Task<CampaignDetailsDbModel?> GetCampaignWithStatsAsync(string campaignId, int? timeRangeDays = null)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            
            if (campaign != null && timeRangeDays.HasValue && timeRangeDays.Value < 9999)
            {
                var campaigns = new List<CampaignDetailsDbModel> { campaign };
                await CalculateTimeRangeStats(campaigns, timeRangeDays.Value);
            }

            return campaign;
        }

        public async Task<Dictionary<string, object>> GetCampaignAnalyticsAsync(string campaignId)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                return new Dictionary<string, object>();
            }

            var analytics = new Dictionary<string, object>
            {
                ["campaignId"] = campaign.Id,
                ["name"] = campaign.Name ?? "",
                ["status"] = campaign.Status ?? "",
                ["totalSent"] = campaign.TotalSent ?? 0,
                ["totalOpened"] = campaign.TotalOpened ?? 0,
                ["totalClicked"] = campaign.TotalClicked ?? 0,
                ["totalReplied"] = campaign.TotalReplied ?? 0,
                ["totalPositiveReplies"] = campaign.TotalPositiveReplies ?? 0,
                ["totalBounced"] = campaign.TotalBounced ?? 0,
                ["openRate"] = campaign.TotalSent > 0 ? Math.Round((decimal)((double)(campaign.TotalOpened ?? 0) / (campaign.TotalSent ?? 1) * 100), 2) : 0,
                ["clickRate"] = campaign.TotalSent > 0 ? Math.Round((decimal)((double)(campaign.TotalClicked ?? 0) / (campaign.TotalSent ?? 1) * 100), 2) : 0,
                ["replyRate"] = campaign.TotalSent > 0 ? Math.Round((decimal)((double)(campaign.TotalReplied ?? 0) / (campaign.TotalSent ?? 1) * 100), 2) : 0,
                ["bounceRate"] = campaign.TotalSent > 0 ? Math.Round((decimal)((double)(campaign.TotalBounced ?? 0) / (campaign.TotalSent ?? 1) * 100), 2) : 0,
                ["positiveReplyRate"] = campaign.TotalReplied > 0 ? Math.Round((decimal)((double)(campaign.TotalPositiveReplies ?? 0) / (campaign.TotalReplied ?? 1) * 100), 2) : 0
            };

            // Get daily stats for trend analysis
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-30);
            var dailyStats = await _dailyStatsRepository.GetByCampaignIdAndDateRangeAsync(campaign.Id, startDate, endDate);

            var dailyTrend = dailyStats.Select(s => new
            {
                date = s.StatDate.ToString("yyyy-MM-dd"),
                sent = s.Sent,
                opened = s.Opened,
                replied = s.Replied,
                bounced = s.Bounced
            }).ToList();

            analytics["dailyTrend"] = dailyTrend;
            analytics["peakDay"] = dailyStats.OrderByDescending(s => s.Sent).FirstOrDefault()?.StatDate.ToString("yyyy-MM-dd") ?? "";
            analytics["averageDailySent"] = dailyStats.Any() ? Math.Round(dailyStats.Average(s => s.Sent), 1) : 0;

            return analytics;
        }

        public async Task<bool> SyncCampaignFromSmartleadAsync(string campaignId)
        {
            try
            {
                // TODO: Implement Smartlead API sync
                // This would call the Smartlead API to fetch latest campaign data
                // and update the local database
                
                _logger.LogInformation($"Syncing campaign {campaignId} from Smartlead API");
                
                // Placeholder for now
                await Task.Delay(100);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing campaign {campaignId} from Smartlead");
                return false;
            }
        }

        public async Task<int?> CreateCampaignAsync(CreateCampaignV1Request request, int clientId)
        {
            try
            {
                _logger.LogInformation($"Creating campaign via Smartlead API: {request.Title} for client {clientId}");

                // Step 1: Create campaign in Smartlead
                var createResponse = _smartleadApiService.CreateCampaign(request.Title, clientId);
                if (createResponse == null)
                {
                    throw new InvalidOperationException("Failed to create campaign in Smartlead API");
                }

                _logger.LogInformation($"Campaign created in Smartlead with ID: {createResponse.Id}");

                // Store campaign ID to ensure we always return it even if subsequent steps fail
                int campaignId = createResponse.Id;

                // Step 1.5: Save campaign to local database immediately (makes sequence endpoint work right away)
                try
                {
                    const string adminUuid = "c38e38e8-d7b6-4c00-836d-3437678ef4d9"; // TODO: Get from user context

                    var newCampaign = new CampaignDetailsDbModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        AdminUuid = adminUuid,
                        CampaignId = campaignId,
                        Name = request.Title,
                        ClientId = null, // Will be filled by sync service later
                        Status = "DRAFT", // Initial status
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastUpdatedAt = DateTime.UtcNow // Prevents sync service from touching for 24 hours
                    };

                    await _campaignRepository.CreateAsync(newCampaign);
                    _logger.LogInformation($"Saved campaign {campaignId} to local database");
                }
                catch (Exception ex)
                {
                    // Don't fail campaign creation if database save fails
                    _logger.LogError(ex, $"Failed to save campaign {campaignId} to local database. Sync service will pick it up later.");
                }

                // Step 2: Update campaign settings in Smartlead (wrapped in try-catch to prevent failure)
                try
                {
                    var settingsRequest = MapToSmartleadSettings(request, clientId);

                    var settingsResponse = _smartleadApiService.UpdateCampaignSettings(campaignId, settingsRequest);
                    if (settingsResponse == null || !settingsResponse.Success)
                    {
                        _logger.LogWarning($"Failed to update campaign settings for campaign {campaignId}. Settings response: {settingsResponse?.Message}");
                    }
                    else
                    {
                        _logger.LogInformation($"Campaign settings updated successfully for campaign {campaignId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception while updating campaign settings for campaign {campaignId}. Campaign created successfully, but settings update failed.");
                }

                _logger.LogInformation($"Campaign successfully created: {campaignId}");

                // Step 3: Configure timing if provided (wrapped in try-catch to prevent failure)
                // Timezone is required by Smartlead API, so skip if not provided
                if (request.TimingSettings != null && !string.IsNullOrWhiteSpace(request.TimingSettings.Region))
                {
                    try
                    {
                        var smartleadTimingRequest = new SmartleadScheduleCampaignRequest
                        {
                            Timezone = request.TimingSettings.Region,
                            DaysOfTheWeek = request.TimingSettings.ActiveDays,
                            StartHour = request.TimingSettings.StartTime,
                            EndHour = request.TimingSettings.EndTime,
                            MinTimeBtwEmails = request.TimingSettings.IntervalMinutes,
                            MaxNewLeadsPerDay = request.TimingSettings.DailyLimit,
                            ScheduleStartTime = request.TimingSettings.ActivationTime
                        };

                        var timingResponse = _smartleadApiService.ScheduleCampaign(campaignId, smartleadTimingRequest);
                        if (timingResponse == null || !timingResponse.Success)
                        {
                            _logger.LogWarning($"Campaign created successfully but timing configuration failed for campaign {campaignId}. Response: {timingResponse?.Message}");
                        }
                        else
                        {
                            _logger.LogInformation($"Campaign timing configured successfully for campaign {campaignId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Exception while configuring timing for campaign {campaignId}. Campaign created successfully, but timing configuration failed.");
                    }
                }
                else if (request.TimingSettings != null)
                {
                    _logger.LogInformation($"Skipping timing configuration for campaign {campaignId} - timezone/region not provided");
                }

                return campaignId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign via Smartlead API");
                return null;
            }
        }

        private SmartleadUpdateCampaignSettingsRequest MapToSmartleadSettings(CreateCampaignV1Request request, int clientId)
        {
            // Map tracking settings - your format to Smartlead format
            var trackSettings = new List<string>();
            if (request.Tracking.Contains("NO_EMAIL_OPEN_TRACK"))
                trackSettings.Add("DONT_TRACK_EMAIL_OPEN");
            if (request.Tracking.Contains("NO_LINK_CLICK_TRACK"))
                trackSettings.Add("DONT_TRACK_LINK_CLICK");

            // Map lead stop condition
            var stopLeadSettings = request.LeadStopCondition switch
            {
                "ON_EMAIL_REPLY" => "REPLY_TO_AN_EMAIL",
                _ => "REPLY_TO_AN_EMAIL" // default
            };

            // Map out of office settings
            var oooSettings = new SmartleadOutOfOfficeSettings
            {
                IgnoreOOOasReply = !request.OutOfOfficeRules.CountOOOasReply,
                AutoReactivateOOO = request.OutOfOfficeRules.AutoResumeOOO,
                ReactivateOOOwithDelay = request.OutOfOfficeRules.ResumeOOOafterDelay,
                AutoCategorizeOOO = request.OutOfOfficeRules.TagOOOautomatically
            };

            return new SmartleadUpdateCampaignSettingsRequest
            {
                Name = request.Title,
                TrackSettings = trackSettings,
                StopLeadSettings = stopLeadSettings,
                UnsubscribeText = request.UnsubscribeMessage,
                SendAsPlainText = request.PlainTextOnly,
                ForcePlainText = request.ForcePlainFormat,
                EnableAiEspMatching = request.SmartProviderMatching,
                FollowUpPercentage = request.FollowupRatio,
                ClientId = clientId,
                AddUnsubscribeTag = request.IncludeUnsubscribeMarker,
                AutoPauseDomainLeadsOnReply = request.PauseDomainOnReply,
                IgnoreSsMailboxSendingLimit = request.BypassSharedLimit,
                // Only set BounceAutopauseThreshold if bounceLimit is provided (not empty)
                BounceAutopauseThreshold = string.IsNullOrWhiteSpace(request.BounceLimit) ? "5" : request.BounceLimit,
                OutOfOfficeDetectionSettings = oooSettings,
                AiCategorisationOptions = request.AiSortingOptions
            };
        }


        public async Task<bool> ConfigureCampaignSequenceAsync(int campaignId, ConfigureSequenceV1Request request)
        {
            try
            {
                _logger.LogInformation($"Configuring campaign sequence for campaign {campaignId}");
                _logger.LogInformation($"Request has {request.Steps?.Count ?? 0} steps");

                // Map the obfuscated sequence to Smartlead format
                var smartleadRequest = new SmartleadSequencesRequest
                {
                    Sequences = request.Steps.Select(step => MapSequenceStepToSmartleadSequence(step)).ToList()
                };

                // Call Smartlead API
                var response = _smartleadApiService.ConfigureCampaignSequences(campaignId, smartleadRequest);
                
                if (response == null || !response.Success)
                {
                    _logger.LogError($"Failed to configure campaign sequence for campaign {campaignId}. Response: {response?.Message}");
                    return false;
                }

                _logger.LogInformation($"Campaign sequence configured successfully for campaign {campaignId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error configuring campaign sequence for campaign {campaignId}");
                return false;
            }
        }

        private SmartleadSequence MapSequenceStepToSmartleadSequence(SequenceStep step)
        {
            var sequence = new SmartleadSequence
            {
                Id = step.Id,
                SeqNumber = step.StepNumber,
                SeqType = step.StepType,
                SeqDelayDetails = new SmartleadSeqDelayDetails
                {
                    DelayInDays = step.DelaySettings.DelayDays
                }
            };

            // If step has variants (A/B testing)
            if (step.MessageVariants != null && step.MessageVariants.Any())
            {
                sequence.VariantDistributionType = step.TestingMode;
                sequence.LeadDistributionPercentage = step.SampleSize;
                sequence.WinningMetricProperty = step.WinningCriteria;
                sequence.SeqVariants = step.MessageVariants.Select(variant => new SmartleadSeqVariant
                {
                    Id = variant.Id,
                    Subject = variant.Subject,
                    EmailBody = variant.Content,
                    VariantLabel = variant.Label,
                    VariantDistributionPercentage = variant.DistributionPercentage
                }).ToList();
            }
            else
            {
                // Simple follow-up without variants
                sequence.Subject = step.Subject;
                sequence.EmailBody = step.Content;
            }

            return sequence;
        }

        public async Task<LeadUploadResult> UploadLeadsAsync(int campaignId, List<LeadInput> leads)
        {
            try
            {
                _logger.LogInformation($"Uploading {leads.Count} leads to campaign {campaignId}");

                // Create the payload for Smartlead API with hardcoded settings
                var smartleadRequest = new
                {
                    lead_list = leads.Select(lead => new
                    {
                        first_name = lead.FirstName,
                        last_name = lead.LastName,
                        email = lead.Email,
                        phone_number = lead.PhoneNumber,
                        company_name = lead.CompanyName,
                        website = lead.Website,
                        location = lead.Location,
                        custom_fields = lead.CustomFields,
                        linkedin_profile = lead.LinkedinProfile,
                        company_url = lead.CompanyUrl
                    }).ToList(),
                    settings = new
                    {
                        ignore_global_block_list = true,
                        ignore_unsubscribe_list = true,
                        ignore_community_bounce_list = true,
                        ignore_duplicate_leads_in_other_campaign = true,
                        return_lead_ids = true
                    }
                };

                // Call Smartlead API to upload leads
                var response = await _smartleadApiService.UploadCampaignLeadsAsync(campaignId, smartleadRequest);
                
                if (response == null || !response.Success)
                {
                    _logger.LogError($"Failed to upload leads to campaign {campaignId}. Response: {response?.Message}");
                    return new LeadUploadResult
                    {
                        Success = false,
                        ErrorMessage = response?.Message ?? "Failed to upload leads to Smartlead",
                        UploadedCount = 0
                    };
                }

                _logger.LogInformation($"Successfully uploaded {leads.Count} leads to campaign {campaignId}");
                
                return new LeadUploadResult
                {
                    Success = true,
                    UploadedCount = leads.Count,
                    LeadIds = response.LeadIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading leads to campaign {campaignId}");
                return new LeadUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    UploadedCount = 0
                };
            }
        }
    }
}