using System.Text.Json;
using LeadHype.Api.Core.Models.ExternalApi;

namespace LeadHype.Api.Core.Services.ExternalApi;

public interface IRevReplyClassificationService
{
    Task<ClassificationResponse> ClassifyEmailAsync(string emailBody);
}

public class RevReplyClassificationService : IRevReplyClassificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RevReplyClassificationService> _logger;

    public RevReplyClassificationService(
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration,
        ILogger<RevReplyClassificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ClassificationResponse> ClassifyEmailAsync(string emailBody)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(emailBody), "email_body");
            
            var apiKey = _configuration["RevReplyApi:ApiKey"] ?? throw new InvalidOperationException("RevReply API key not configured");
            const string endpointUrl = "https://api.revreply.com/v1/response/classify";
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            HttpResponseMessage response = await httpClient.PostAsync(endpointUrl, formData);
            string result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Classification successful for email body. Raw response: {Response}", result);
                
                try
                {
                    // Parse the JSON response to extract the classification
                    using var doc = JsonDocument.Parse(result);
                    var root = doc.RootElement;
                    
                    // Handle different response formats
                    if (root.TryGetProperty("data", out var dataElement) && 
                        dataElement.TryGetProperty("classification", out var classificationElement))
                    {
                        var classification = classificationElement.GetString();
                        _logger.LogInformation("Extracted classification: {Classification}", classification);
                        return new ClassificationResponse(true, classification);
                    }
                    else if (root.TryGetProperty("classification", out var directClassificationElement))
                    {
                        var classification = directClassificationElement.GetString();
                        _logger.LogInformation("Extracted classification: {Classification}", classification);
                        return new ClassificationResponse(true, classification);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find classification in response structure: {Response}", result);
                        return new ClassificationResponse(false, Error: "Classification not found in API response");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse classification JSON response: {Response}", result);
                    // Fallback: return raw result if JSON parsing fails
                    return new ClassificationResponse(true, result);
                }
            }
            else
            {
                string error = $"Classification API failed with status: {response.StatusCode}, Response: {result}";
                _logger.LogError(error);
                return new ClassificationResponse(false, Error: error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email classification");
            return new ClassificationResponse(false, Error: ex.Message);
        }
    }
}