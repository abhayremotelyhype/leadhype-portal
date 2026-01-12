using System.Globalization;
using System.Net;
using HtmlAgilityPack;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.API.Smartlead;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UtilityPack;
using static LeadHype.Api.DI;
using static UtilityPack.Tasks;

namespace LeadHype.Api;

public class SmartleadApiService
{
    public SmartleadApiService(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:139.0) Gecko/20100101 Firefox/139.0");

        ApiKey = apiKey;
    }

    private HttpClient _httpClient;

    #region Public Properties

    public string ApiKey { get; set; }

    #endregion

    public Action<DateTime> OnEmailStatsSuccessfullyFetched { get; set; }

    #region Public Methods

    public List<CampaignSummary> FetchCampaigns()
    {
        return Run(IFetchCampaigns,
            r => r is not null,
            OnError
        ) ?? [];
    }

    public List<CampaignStat> FetchCampaignStats(string id)
    {
        int offset = 0;
        int limit = 1000;
        List<CampaignStat> campaignStats = new();
        while (true)
        {
            var statsResponse = Run(() => IFetchCampaignStats(id, offset, limit),
                r => r is not null,
                OnError
            );

            if (statsResponse is null)
                break;

            if (!int.TryParse(statsResponse.TotalStats, out var total))
                break;

            if (total == 0)
                break;

            campaignStats.AddRange(statsResponse.Stats);

            if (campaignStats.Count >= total)
                break;

            offset += limit;
        }

        return campaignStats;
    }

    public List<LeadDatum> FetchLeads(string campaignId)
    {
        int offset = 0;
        int limit = 100;
        List<LeadDatum> leads = new();
        while (true)
        {
            var statsResponse = Run(() => IFetchLeads(campaignId, offset, limit),
                r => r is not null,
                OnError
            );

            if (statsResponse is null)
                break;

            if (!int.TryParse(statsResponse.TotalLeads, out var total))
                break;

            if (total == 0)
                break;

            leads.AddRange(statsResponse.Data);
            if (leads.Count >= total)
                break;

            offset += limit;
        }

        return leads;
    }

    public List<EmailHealthMetric> FetchEmailStats(List<DateTime> dateRange)
    {
        // DateTime endDate = DateTime.Now;

        List<EmailHealthMetric> emailHealthMetrics = [];
        foreach (DateTime dateRangeItem in dateRange)
        {
            List<EmailHealthMetric>? emailHealthMetricsResponse = Run(() =>
                    IFetchEmailStats(dateRangeItem),
                r => r is not null,
                OnError
            );

            if (emailHealthMetricsResponse is null)
                break;

            emailHealthMetricsResponse.ForEach(d => { d.DateTime = dateRangeItem; });

            emailHealthMetrics.AddRange(emailHealthMetricsResponse);
        }

        return emailHealthMetrics;
    }

    public List<EmailAccountDetails> FetchEmailAccountDetails()
    {
        int offset = 0;
        int limit = 100;
        List<EmailAccountDetails> emailAccountDetails = new();

        while (true)
        {
            List<EmailAccountDetails>? accountDetails = Run(() =>
                    IFetchEmailAccountDetails(offset, limit),
                r => r is not null,
                OnError
            );

            if (accountDetails is null ||
                accountDetails.Count == 0)
                break;

            emailAccountDetails.AddRange(accountDetails);

            if (accountDetails.Count < limit)
                break;

            offset += limit;
        }

        return emailAccountDetails;
    }

    public EmailWarmupStats? FetchEmailWarmupStats(string emailId)
    {
        return Run(() => IFetchEmailWarmupStats(emailId),
            r => r is not null,
            OnError
        );
    }

    public LeadIdResponse[] FetchLeadIds(int campaignId)
    {
        int limit = 100;
        int offset = 0;

        List<LeadIdResponse> leads = new();
        
        while (true)
        {
            LeadIdResponse[]? leadIds = Run(() =>
                    IFetchLeadIds(campaignId, limit, offset),
                r => r is not null,
                OnError
            );
            
            if (leadIds is null ||
                leadIds.Length == 0)
                break;
            
            leads.AddRange(leadIds);
            
            if (leadIds.Length < limit)
                break;
            
            offset += limit;
        }
       
        return leads.ToArray();
        // return Run(() =>
        //         IFetchLeadIds(campaignId),
        //     r => r is not null,
        //     OnError
        // ) ?? [];
    }

    public dynamic FetchMasterInboxReplies(int offset = 0, int limit = 20, dynamic filters = null)
    {
        return Run(() =>
                IFetchMasterInboxReplies(offset, limit, filters),
            r => r is not null,
            OnError
        ) ?? new { };
    }

    public SmartleadInboxRepliesResponse? FetchInboxReplies(int offset = 0, int limit = 20, int? campaignId = null)
    {
        return Run(() =>
                IFetchInboxReplies(offset, limit, campaignId),
            r => r is not null,
            OnError
        );
    }

    public SmartleadCreateCampaignResponse? CreateCampaign(string name, int? clientId = null)
    {
        return Run(() =>
                ICreateCampaign(name, clientId),
            r => r is not null,
            OnError
        );
    }

    public SmartleadUpdateCampaignSettingsResponse? UpdateCampaignSettings(int campaignId, SmartleadUpdateCampaignSettingsRequest request)
    {
        return Run(() =>
                IUpdateCampaignSettings(campaignId, request),
            r => r is not null,
            OnError
        );
    }

    public SmartleadScheduleCampaignResponse? ScheduleCampaign(int campaignId, SmartleadScheduleCampaignRequest request)
    {
        return Run(() =>
                IScheduleCampaign(campaignId, request),
            r => r is not null,
            OnError
        );
    }

    public SmartleadSequencesResponse? ConfigureCampaignSequences(int campaignId, SmartleadSequencesRequest request)
    {
        return Run(() =>
                IConfigureCampaignSequences(campaignId, request),
            r => r is not null,
            OnError
        );
    }

    public async Task<SmartleadUploadLeadsResponse?> UploadCampaignLeadsAsync(int campaignId, object request)
    {
        return await Task.Run(() => Run(() =>
                IUploadCampaignLeads(campaignId, request),
            r => r is not null,
            OnError
        ));
    }

    #endregion


    #region Private Methods

    #region LeadIds

    private LeadIdResponse[]? IFetchLeadIds(int campaignId, int limit, int offset)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "campaigns", campaignId.ToString(), "leads")
            .AddQuery("api_key", ApiKey)
            .AddQuery("offset", offset.ToString())
            .AddQuery("limit", limit.ToString())
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        string responseBody = responseMessage.Content.ReadAsString();

        if (!responseMessage.IsSuccessStatusCode ||
            string.IsNullOrEmpty(responseBody))
        {
            return null;
        }

        if (!responseBody.IsJson(out JObject? jObject) ||
            jObject is null)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return null;
        }

        List<string> leadIds = [];

        var jsonArray = jObject?["data"] as JArray;

        List<LeadIdResponse> list = [];

        foreach (JToken? json in jsonArray)
        {
            string? status = json?["status"]?.Value<string>();
            if (string.IsNullOrEmpty(status))
                continue;

            //Confirm if the status meets the criteria
            if (!status.Equals("COMPLETED") &&
                !status.Equals("INPROGRESS"))
                continue;

            var leadId = json?["lead"]?["id"]?.Value<string>();
            if (string.IsNullOrEmpty(leadId))
                continue;

            // Extract the full lead object
            var leadData = json?["lead"]?.ToObject<Lead>();
            
            var lead = new LeadIdResponse
            {
                Id = leadId,
                Status = status,
                Lead = leadData
            };

            JObject customFields = (JObject)json?["lead"]?["custom_fields"];
            if (customFields is not null)
            {
                foreach (JProperty property in customFields.Properties())
                {
                    string propertyName = property.Name;
                    string propertyValue = property.Value.ToString();
                    lead.CustomFields.Add(new KeyValuePair<string, string>(propertyName, propertyValue));
                }
            }

            list.Add(lead);
        }

        if (list.Any(lead => lead.Status.Equals("COMPLETED")))
            return list.Where(lead => lead.Status.Equals("COMPLETED"))
                .ToArray();

        return list
            .ToArray();
    }

    #endregion

    public PositiveReplyDaybyDayStats? FetchDaybyDayPositiveReplyStats(string campaignId, DateTime startDate,
        DateTime endDate)
    {
        return Run(() => IFetchDaybyDayPositiveReplyStats(campaignId, startDate, endDate),
            r => r is not null && r.Success.GetValueOrDefault(),
            OnError
        );
    }

    private bool HandleResponse(HttpResponseMessage responseMessage)
    {
        if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
        {
            Console.WriteLine($"[WARN] Smartlead API rate limit exceeded (429). Request: {responseMessage.RequestMessage?.Method} {responseMessage.RequestMessage?.RequestUri}");
            Thread.Sleep(TimeSpan.FromSeconds(15));
            return false;
        }

        if (!responseMessage.IsSuccessStatusCode)
        {
            string responseBody = responseMessage.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"[ERROR] Smartlead API error. Status: {(int)responseMessage.StatusCode} {responseMessage.StatusCode}. Request: {responseMessage.RequestMessage?.Method} {responseMessage.RequestMessage?.RequestUri}. Response: {responseBody}");
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            Thread.Sleep(TimeSpan.FromSeconds(5));
            return false;
        }

        return true;
    }

    private PositiveReplyDaybyDayStats? IFetchDaybyDayPositiveReplyStats(string campaignId, DateTime startDate,
        DateTime endDate)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "analytics", "day-wise-positive-reply-stats")
            .AddQuery("api_key", ApiKey)
            .AddQuery("start_date", $"{startDate:yyyy-MM-dd}")
            .AddQuery("end_date", $"{endDate:yyyy-MM-dd}")
            .AddQuery("campaign_ids", campaignId)
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();


        var positiveReplyDaybyDayStats = JsonConvert.DeserializeObject<PositiveReplyDaybyDayStats>(responseBody);
        if (positiveReplyDaybyDayStats == null)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return null;
        }

        return positiveReplyDaybyDayStats;
    }

    private List<CampaignSummary>? IFetchCampaigns()
    {
        HttpRequestMessage message = new()
        {
            RequestUri = new Uri($"https://server.smartlead.ai/api/v1/campaigns?api_key={ApiKey}"),
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();

        var campaignSummaries = JsonConvert.DeserializeObject<List<CampaignSummary>>(responseBody);
        if (campaignSummaries == null || campaignSummaries.Count == 0)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return [];
        }

        return campaignSummaries;
    }

    private CampaignStatsResponse? IFetchCampaignStats(string id, int offset, int limit = 1000)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "campaigns", id, "statistics")
            .AddQuery("api_key", ApiKey)
            .AddQuery("offset", offset.ToString())
            .AddQuery("limit", limit.ToString())
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);

        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();

        CampaignStatsResponse? campaignStats = JsonConvert.DeserializeObject<CampaignStatsResponse>(responseBody);

        if (campaignStats == null || campaignStats.Limit == 0)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
        }

        return campaignStats;
    }

    public List<long>? FetchEmailAccountsOfCampaign(string campaignId)
    {
        return Run(() =>
                IFetchEmailAccountsOfCampaign(campaignId),
            r => r is not null,
            OnError
        );
    }

    private List<long>? IFetchEmailAccountsOfCampaign(string campaignId)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "campaigns", campaignId, "email-accounts")
            .AddQuery("api_key", ApiKey)
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        string responseBody = responseMessage.Content.ReadAsString();

        JArray? jsonArray = null;
        try
        {
            jsonArray = JArray.Parse(responseBody);
        }
        catch
        {
            jsonArray = null;
        }

        if (jsonArray == null)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return [];
        }

        List<long> emailAccounts = [];

        foreach (var jsonObject in jsonArray)
        {
            long? id = jsonObject?["id"]?.Value<long>();
            if (!id.HasValue)
                continue;

            emailAccounts.Add(id.Value);
        }

        return emailAccounts;
    }

    private LeadsResponse? IFetchLeads(string campaignId, int offset, int limit = 100)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "campaigns", campaignId, "leads")
            .AddQuery("api_key", ApiKey)
            .AddQuery("offset", offset.ToString())
            .AddQuery("limit", limit.ToString())
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();

        LeadsResponse? leadsResponse = JsonConvert.DeserializeObject<LeadsResponse>(responseBody);

        if (leadsResponse == null || leadsResponse.Limit == 0)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
        }

        return leadsResponse;
    }


    private List<EmailHealthMetric>? IFetchEmailStats(DateTime dateTime)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "analytics", "mailbox", "name-wise-health-metrics")
            .AddQuery("api_key", ApiKey)
            .AddQuery("start_date", dateTime.ToString("yyyy-MM-dd"))
            .AddQuery("end_date", dateTime.ToString("yyyy-MM-dd"))
            .AddQuery("full_data", "true")
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);

        //Wait if rate limited
        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();

        var emailHealthMetricsResponse = JsonConvert.DeserializeObject<EmailHealthMetricsResponse>(responseBody);
        if (emailHealthMetricsResponse == null ||
            !emailHealthMetricsResponse.Success.GetValueOrDefault())
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return null;
        }

        var emailHealthMetricsData = emailHealthMetricsResponse.Data;
        var emailHealthMetrics = emailHealthMetricsData.EmailHealthMetrics;

        //Update the date time
        // emailHealthMetrics.ForEach(d => d.DateTime = DateTime.Now);
        OnEmailStatsSuccessfullyFetched?.Invoke(dateTime);
        return emailHealthMetrics;
    }

    private List<EmailAccountDetails>? IFetchEmailAccountDetails(int offset, int limit = 100)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "email-accounts")
            .AddQuery("api_key", ApiKey)
            .AddQuery("offset", offset.ToString())
            .AddQuery("limit", limit.ToString())
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        if (!HandleResponse(responseMessage))
            return null;


        string responseBody = responseMessage.Content.ReadAsString();

        List<EmailAccountDetails>? emailAccountDetails =
            JsonConvert.DeserializeObject<List<EmailAccountDetails>>(responseBody);

        if (emailAccountDetails == null)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return null;
        }

        return emailAccountDetails;
    }


    private EmailWarmupStats? IFetchEmailWarmupStats(string emailId)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "email-accounts", emailId, "warmup-stats")
            .AddQuery("api_key", ApiKey)
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();
        EmailWarmupStats? emailWarmupStats = JsonConvert.DeserializeObject<EmailWarmupStats>(responseBody);

        if (emailWarmupStats != null)
            return emailWarmupStats;

        Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
        return null;
    }

    #endregion

    #region Lead History and Templates Methods

    public LeadEmailHistory[] FetchHistory(string campaignId, string leadId)
    {
        return Run(() =>
                IFetchHistory(campaignId, leadId),
            r => r is not null,
            OnError
        ) ?? [];
    }

    private LeadEmailHistory[]? IFetchHistory(string campaignId, string leadId)
    {
        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "campaigns", campaignId, "leads", leadId, "message-history")
            .AddQuery("api_key", ApiKey)
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Get
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        string responseBody = responseMessage.Content.ReadAsString();

        if (!responseBody.IsJson(out JObject? jObject) ||
            jObject is null)
        {
            Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
            return null;
        }

        JArray? historyArray = jObject?["history"] as JArray;

        List<LeadEmailHistory> historyModels = [];
        foreach (JToken? history in historyArray)
        {
            string? emailBody = history?["email_body"]?.Value<string>();
            string? emailSubject = history?["subject"]?.Value<string>();
            int? emailSequenceNumber = history?["email_seq_number"]?.Value<int>();
            string? type = history?["type"]?.Value<string>();
            string? timeString = history?["time"]?.Value<string>();
            
            string format = "MM/dd/yyyy HH:mm:ss";
            DateTime? timeValue = null;
            if (!string.IsNullOrEmpty(timeString) && DateTime.TryParseExact(timeString, format, CultureInfo.InvariantCulture,DateTimeStyles.None, out DateTime parsedDate))
            {
                timeValue = parsedDate;
            }

            historyModels.Add(new()
            {
                Subject = emailSubject ?? "",
                Body = ConvertHtmlToText(emailBody ?? ""),
                SequenceNumber = emailSequenceNumber.GetValueOrDefault(),
                Type = type ?? "SENT",
                Time = timeValue
            });
        }

        return historyModels.ToArray();
    }

    // public List<CampaignLead>? FetchCampaignLeads(string campaignId)
    // {
    //     return Run(() => IFetchCampaignLeads(campaignId),
    //         r => r is not null,
    //         OnError
    //     );
    // }

    public List<LeadEmailHistory>? FetchLeadHistory(string campaignId, string leadId)
    {
        return Run(() => IFetchLeadHistory(campaignId, leadId),
            r => r is not null,
            OnError
        );
    }

    public List<CampaignEmailTemplate>? FetchCampaignTemplates(string campaignId)
    {
        return Run(() => IFetchCampaignTemplates(campaignId),
            r => r is not null,
            OnError
        );
    }

    // private List<CampaignLead>? IFetchCampaignLeads(string campaignId)
    // {
    //     try
    //     {
    //         Uri uri = Url.Create("https://server.smartlead.ai")
    //             .AddPaths("api", "v1", "campaigns", campaignId, "leads")
    //             .AddQuery("api_key", ApiKey)
    //             .AddQuery("offset", "0")
    //             .AddQuery("limit", "100")
    //             .ToUri();
    //
    //         HttpRequestMessage message = new()
    //         {
    //             RequestUri = uri,
    //             Method = HttpMethod.Get
    //         };
    //
    //         HttpResponseMessage responseMessage = _httpClient.Send(message);
    //         if (!HandleResponse(responseMessage))
    //             return null;
    //
    //         string responseBody = responseMessage.Content.ReadAsString();
    //         
    //         // Parse the response manually to match Smartlead's actual API format
    //         var jObject = JsonConvert.DeserializeObject<JObject>(responseBody);
    //         if (jObject == null) return null;
    //         
    //         var dataArray = jObject["data"] as JArray;
    //         if (dataArray == null) return new List<CampaignLead>();
    //         
    //         var leadsList = new List<CampaignLead>();
    //         foreach (var item in dataArray)
    //         {
    //             var status = item["status"]?.Value<string>() ?? "";
    //             
    //             // Only include leads that are COMPLETED or INPROGRESS
    //             if (!status.Equals("COMPLETED") && !status.Equals("INPROGRESS"))
    //                 continue;
    //                 
    //             var leadId = item["lead"]?["id"]?.Value<string>() ?? "";
    //             if (string.IsNullOrEmpty(leadId)) continue;
    //             
    //             var lead = new CampaignLead
    //             {
    //                 Id = leadId,
    //                 Status = status,
    //                 CustomFields = new List<KeyValuePair<string, string>>()
    //             };
    //             
    //             // Parse custom fields from the lead object
    //             var leadObject = item["lead"] as JObject;
    //             if (leadObject != null)
    //             {
    //                 foreach (var property in leadObject.Properties())
    //                 {
    //                     if (property.Name == "id") continue; // Skip the id field
    //                     var value = property.Value?.ToString() ?? "";
    //                     if (!string.IsNullOrEmpty(value))
    //                     {
    //                         lead.CustomFields.Add(new KeyValuePair<string, string>(property.Name, value));
    //                     }
    //                 }
    //             }
    //             
    //             leadsList.Add(lead);
    //         }
    //         
    //         return leadsList;
    //     }
    //     catch (Exception ex)
    //     {
    //         Logger.LogError(ex, "Error fetching campaign leads");
    //         return null;
    //     }
    // }

    private List<LeadEmailHistory>? IFetchLeadHistory(string campaignId, string leadId)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", campaignId, "leads", leadId, "message-history")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Get
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();

            // Parse the response manually to match Smartlead's actual API format
            var jObject = JsonConvert.DeserializeObject<JObject>(responseBody);
            if (jObject == null) return null;

            var historyArray = jObject["history"] as JArray;
            if (historyArray == null) return new List<LeadEmailHistory>();

            var historyList = new List<LeadEmailHistory>();
            foreach (var item in historyArray)
            {
                var emailBody = item["email_body"]?.Value<string>() ?? "";
                var subject = item["subject"]?.Value<string>() ?? "";
                var sequenceNumber = item["email_seq_number"]?.Value<int>() ?? 0;
                var type = item["type"]?.Value<string>() ?? "SENT";
                var timeString = item["time"]?.Value<string>();
                DateTime? timeValue = null;
                
                if (!string.IsNullOrEmpty(timeString) && DateTime.TryParse(timeString, out DateTime parsedTime))
                {
                    timeValue = parsedTime;
                }

                historyList.Add(new LeadEmailHistory
                {
                    Subject = subject,
                    Body = emailBody, // Keep HTML format for now
                    SequenceNumber = sequenceNumber,
                    Type = type,
                    Time = timeValue
                });
            }

            return historyList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching lead history");
            return null;
        }
    }

    private List<CampaignEmailTemplate>? IFetchCampaignTemplates(string campaignId)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", campaignId, "sequences")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Get
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();

            // Parse the response manually to match Smartlead's actual API format
            var jsonArray = JsonConvert.DeserializeObject<JArray>(responseBody);
            if (jsonArray == null) return new List<CampaignEmailTemplate>();

            var templatesList = new List<CampaignEmailTemplate>();
            foreach (var item in jsonArray)
            {
                var subject = item["subject"]?.Value<string>() ?? "";
                var body = item["email_body"]?.Value<string>() ?? "";
                var sequenceNumber = item["seq_number"]?.Value<int>() ?? 0;

                // Parse sequence_variants array if it exists
                List<CampaignEmailTemplateVariant>? variants = null;
                var variantsArray = item["sequence_variants"] as JArray;
                if (variantsArray != null && variantsArray.Count > 0)
                {
                    variants = new List<CampaignEmailTemplateVariant>();
                    foreach (var variant in variantsArray)
                    {
                        variants.Add(new CampaignEmailTemplateVariant
                        {
                            Id = variant["id"]?.Value<long>() ?? 0,
                            VariantLabel = variant["variant_label"]?.Value<string>() ?? "",
                            Subject = variant["subject"]?.Value<string>() ?? "",
                            Body = variant["email_body"]?.Value<string>() ?? ""
                        });
                    }
                }

                templatesList.Add(new CampaignEmailTemplate
                {
                    Subject = subject,
                    Body = body,
                    SequenceNumber = sequenceNumber,
                    Variants = variants
                });
            }

            return templatesList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching campaign templates");
            return null;
        }
    }

    #endregion

    #region Helper Methods

    static string ConvertHtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script and style elements
        doc.DocumentNode.Descendants()
            .Where(n => n.Name == "script" || n.Name == "style")
            .ToList()
            .ForEach(n => n.Remove());

        // Replace br tags with newlines
        doc.DocumentNode.Descendants()
            .Where(n => n.Name == "br")
            .ToList()
            .ForEach(n => n.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), n));

        // Replace div tags with newlines (commonly used for line breaks)
        doc.DocumentNode.Descendants()
            .Where(n => n.Name == "div")
            .ToList()
            .ForEach(n =>
            {
                if (n.PreviousSibling != null && !string.IsNullOrWhiteSpace(n.PreviousSibling.InnerText))
                {
                    n.ParentNode.InsertBefore(doc.CreateTextNode("\n"), n);
                }
            });

        // Replace p tags with double newlines
        doc.DocumentNode.Descendants()
            .Where(n => n.Name == "p")
            .ToList()
            .ForEach(n =>
            {
                if (n.NextSibling != null)
                {
                    n.ParentNode.InsertAfter(doc.CreateTextNode("\n\n"), n);
                }
            });

        // Add space after common inline elements to prevent word concatenation
        var inlineElements = new[] { "span", "strong", "b", "em", "i", "a", "code" };
        foreach (var tagName in inlineElements)
        {
            doc.DocumentNode.Descendants()
                .Where(n => n.Name == tagName)
                .ToList()
                .ForEach(n =>
                {
                    if (n.NextSibling != null &&
                        !string.IsNullOrWhiteSpace(n.NextSibling.InnerText) &&
                        !n.NextSibling.InnerText.StartsWith(" "))
                    {
                        n.ParentNode.InsertAfter(doc.CreateTextNode(" "), n);
                    }
                });
        }

        // Get the inner text and decode HTML entities
        string text = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.InnerText);

        // Clean up excessive whitespace while preserving intentional line breaks
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }

    private dynamic IFetchMasterInboxReplies(int offset, int limit, dynamic filters)
    {
        var requestBody = new
        {
            offset = offset,
            limit = limit,
            filters = filters ?? new { }
        };

        var jsonBody = JsonConvert.SerializeObject(requestBody);
        // Logger.LogInformation($"Sending request to Smartlead: {jsonBody}");

        Uri uri = Url.Create("https://server.smartlead.ai")
            .AddPaths("api", "v1", "master-inbox", "inbox-replies")
            .AddQuery("api_key", ApiKey)
            .AddQuery("fetch_message_history", "true")
            .ToUri();

        HttpRequestMessage message = new()
        {
            RequestUri = uri,
            Method = HttpMethod.Post,
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        };

        HttpResponseMessage responseMessage = _httpClient.Send(message);
        if (!HandleResponse(responseMessage))
            return null;

        string responseBody = responseMessage.Content.ReadAsString();
        // Logger.LogInformation($"Received response from Smartlead: {responseBody}");

        // Return the raw response string to avoid JSON parsing issues
        // The client will receive the proper JSON structure
        return responseBody;
    }

    private SmartleadCreateCampaignResponse? ICreateCampaign(string name, int? clientId)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", "create")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            var requestBody = new SmartleadCreateCampaignRequest
            {
                Name = name,
                ClientId = clientId
            };

            var jsonBody = JsonConvert.SerializeObject(requestBody);

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();
            
            var response = JsonConvert.DeserializeObject<SmartleadCreateCampaignResponse>(responseBody);
            if (response == null)
            {
                Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating campaign in Smartlead API");
            return null;
        }
    }

    private SmartleadUpdateCampaignSettingsResponse? IUpdateCampaignSettings(int campaignId, SmartleadUpdateCampaignSettingsRequest request)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", campaignId.ToString(), "settings")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            var jsonBody = JsonConvert.SerializeObject(request);

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();
            
            var response = JsonConvert.DeserializeObject<SmartleadUpdateCampaignSettingsResponse>(responseBody);
            if (response == null)
            {
                Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating campaign settings in Smartlead API");
            return null;
        }
    }

    private SmartleadScheduleCampaignResponse? IScheduleCampaign(int campaignId, SmartleadScheduleCampaignRequest request)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", campaignId.ToString(), "schedule")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            var jsonBody = JsonConvert.SerializeObject(request);

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();
            
            var response = JsonConvert.DeserializeObject<SmartleadScheduleCampaignResponse>(responseBody);
            if (response == null)
            {
                Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error scheduling campaign in Smartlead API");
            return null;
        }
    }

    private SmartleadSequencesResponse? IConfigureCampaignSequences(int campaignId, SmartleadSequencesRequest request)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", campaignId.ToString(), "sequences")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            var jsonBody = JsonConvert.SerializeObject(request);
            Console.WriteLine($"[DEBUG] Smartlead Sequences JSON: {jsonBody}");
            Console.WriteLine($"[DEBUG] Request.Sequences count: {request.Sequences?.Count ?? 0}");

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();
            
            var response = JsonConvert.DeserializeObject<SmartleadSequencesResponse>(responseBody);
            if (response == null)
            {
                Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error configuring campaign sequences in Smartlead API");
            return null;
        }
    }

    private SmartleadUploadLeadsResponse? IUploadCampaignLeads(int campaignId, object request)
    {
        try
        {
            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "campaigns", campaignId.ToString(), "leads")
                .AddQuery("api_key", ApiKey)
                .ToUri();

            var jsonBody = JsonConvert.SerializeObject(request);

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();
            
            var response = JsonConvert.DeserializeObject<SmartleadUploadLeadsResponse>(responseBody);
            if (response == null)
            {
                Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading leads to campaign in Smartlead API");
            return null;
        }
    }

    private SmartleadInboxRepliesResponse? IFetchInboxReplies(int offset, int limit, int? campaignId)
    {
        try
        {
            var requestBody = new
            {
                offset,
                limit,
                filters = campaignId.HasValue 
                    ? (object)new { campaignId = new[] { campaignId.Value } } 
                    : new { },
                sortBy = "REPLY_TIME_DESC"
            };

            var jsonBody = JsonConvert.SerializeObject(requestBody);

            Uri uri = Url.Create("https://server.smartlead.ai")
                .AddPaths("api", "v1", "master-inbox", "inbox-replies")
                .AddQuery("api_key", ApiKey)
                .AddQuery("fetch_message_history", "true")
                .ToUri();

            HttpRequestMessage message = new()
            {
                RequestUri = uri,
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage = _httpClient.Send(message);
            if (!HandleResponse(responseMessage))
                return null;

            string responseBody = responseMessage.Content.ReadAsString();
            
            var response = JsonConvert.DeserializeObject<SmartleadInboxRepliesResponse>(responseBody);
            if (response == null)
            {
                Logger.LogSourceCode((int)responseMessage.StatusCode, responseBody);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching inbox replies from Smartlead API");
            return null;
        }
    }

    private void OnError(Exception obj)
    {
    }

    #endregion
}