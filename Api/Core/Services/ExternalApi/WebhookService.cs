using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LeadHype.Api.Services
{
    public interface IWebhookService
    {
        Task<string> CreateWebhookAsync(string userId, CreateWebhookRequest request);
        Task<Webhook?> GetWebhookAsync(string id);
        Task<IEnumerable<WebhookResponse>> GetUserWebhooksAsync(string userId);
        Task<bool> UpdateWebhookAsync(string id, UpdateWebhookRequest request);
        Task<bool> DeleteWebhookAsync(string id);
        Task<IEnumerable<WebhookDeliveryResponse>> GetWebhookDeliveriesAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null);
        Task<(IEnumerable<WebhookDeliveryResponse> deliveries, int totalCount)> GetWebhookDeliveriesWithCountAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null);
        
        // Webhook event triggering
        Task TriggerWebhookAsync(string eventType, object payload, string? userId = null);
        Task<bool> TriggerWebhookByIdAsync(string webhookId, string eventType, object payload);
        Task<bool> TestWebhookAsync(string webhookId);
        Task<bool> SendWebhookToUrlAsync(string url, object payload, int timeoutSeconds = 30);
        
    }

    public class WebhookService : IWebhookService
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(
            IWebhookRepository webhookRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<WebhookService> logger)
        {
            _webhookRepository = webhookRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> CreateWebhookAsync(string userId, CreateWebhookRequest request)
        {
            // Events validation removed - webhooks now work without predefined event types

            var webhook = new Webhook
            {
                UserId = userId,
                Name = request.Name,
                Url = request.Url,
                Headers = request.Headers ?? new Dictionary<string, string>(),
                RetryCount = request.RetryCount,
                TimeoutSeconds = request.TimeoutSeconds
            };

            var id = await _webhookRepository.CreateAsync(webhook);
            _logger.LogInformation($"Webhook created: {id} for user {userId}");

            return id;
        }

        public async Task<Webhook?> GetWebhookAsync(string id)
        {
            return await _webhookRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<WebhookResponse>> GetUserWebhooksAsync(string userId)
        {
            var webhooks = await _webhookRepository.GetByUserIdAsync(userId);
            
            return webhooks.Select(w => new WebhookResponse
            {
                Id = w.Id,
                Name = w.Name,
                Url = w.Url,
                Headers = w.Headers,
                IsActive = w.IsActive,
                RetryCount = w.RetryCount,
                TimeoutSeconds = w.TimeoutSeconds,
                LastTriggeredAt = w.LastTriggeredAt,
                FailureCount = w.FailureCount,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            });
        }

        public async Task<bool> UpdateWebhookAsync(string id, UpdateWebhookRequest request)
        {
            var webhook = await _webhookRepository.GetByIdAsync(id);
            if (webhook == null)
            {
                return false;
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Name))
                webhook.Name = request.Name;
            
            if (!string.IsNullOrEmpty(request.Url))
                webhook.Url = request.Url;
            
            
            if (request.Headers != null)
                webhook.Headers = request.Headers;
            
            if (request.RetryCount.HasValue)
                webhook.RetryCount = request.RetryCount.Value;
            
            if (request.TimeoutSeconds.HasValue)
                webhook.TimeoutSeconds = request.TimeoutSeconds.Value;
            
            if (request.IsActive.HasValue)
                webhook.IsActive = request.IsActive.Value;

            return await _webhookRepository.UpdateAsync(webhook);
        }

        public async Task<bool> DeleteWebhookAsync(string id)
        {
            var deleted = await _webhookRepository.DeleteAsync(id);
            if (deleted)
            {
                _logger.LogInformation($"Webhook deleted: {id}");
            }
            return deleted;
        }

        public async Task<IEnumerable<WebhookDeliveryResponse>> GetWebhookDeliveriesAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null)
        {
            var deliveries = await _webhookRepository.GetDeliveriesAsync(webhookId, limit, offset, failuresOnly);
            
            return deliveries.Select(d => new WebhookDeliveryResponse
            {
                Id = d.Id,
                EventType = d.EventType,
                StatusCode = d.StatusCode,
                ResponseBody = d.ResponseBody,
                ErrorMessage = d.ErrorMessage,
                AttemptCount = d.AttemptCount,
                DeliveredAt = d.DeliveredAt,
                CreatedAt = d.CreatedAt
            });
        }

        public async Task<(IEnumerable<WebhookDeliveryResponse> deliveries, int totalCount)> GetWebhookDeliveriesWithCountAsync(string webhookId, int limit = 100, int offset = 0, bool? failuresOnly = null)
        {
            var (deliveries, totalCount) = await _webhookRepository.GetDeliveriesWithCountAsync(webhookId, limit, offset, failuresOnly);
            
            var deliveryResponses = deliveries.Select(d => new WebhookDeliveryResponse
            {
                Id = d.Id,
                EventType = d.EventType,
                StatusCode = d.StatusCode,
                ResponseBody = d.ResponseBody,
                ErrorMessage = d.ErrorMessage,
                AttemptCount = d.AttemptCount,
                DeliveredAt = d.DeliveredAt,
                CreatedAt = d.CreatedAt
            });

            return (deliveryResponses, totalCount);
        }

        public async Task TriggerWebhookAsync(string eventType, object payload, string? userId = null)
        {
            try
            {
                // Get all active webhooks that listen for this event type
                var webhooks = await _webhookRepository.GetActiveWebhooksForEventAsync(eventType);
                
                // Filter by user if specified
                if (!string.IsNullOrEmpty(userId))
                {
                    webhooks = webhooks.Where(w => w.UserId == userId);
                }

                var tasks = webhooks.Select(webhook => DeliverWebhookAsync(webhook, eventType, payload));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error triggering webhooks for event {eventType}");
            }
        }

        public async Task<bool> TestWebhookAsync(string webhookId)
        {
            var webhook = await _webhookRepository.GetByIdAsync(webhookId);
            if (webhook == null)
            {
                return false;
            }

            var testPayload = new
            {
                test = true,
                message = "This is a test webhook delivery",
                timestamp = DateTime.UtcNow,
                webhook_id = webhookId
            };

            await DeliverWebhookAsync(webhook, "test.webhook", testPayload);
            return true;
        }

        public async Task<bool> TriggerWebhookByIdAsync(string webhookId, string eventType, object payload)
        {
            var webhook = await _webhookRepository.GetByIdAsync(webhookId);
            if (webhook == null || !webhook.IsActive)
            {
                return false;
            }

            await DeliverWebhookAsync(webhook, eventType, payload);
            return true;
        }

        private async Task DeliverWebhookAsync(Webhook webhook, string eventType, object payload)
        {
            var delivery = new WebhookDelivery
            {
                WebhookId = webhook.Id,
                EventType = eventType,
                Payload = ConvertToStringObjectDictionary(payload)
            };

            try
            {
                await _webhookRepository.LogDeliveryAsync(delivery);

                var success = await SendWebhookRequestAsync(webhook, delivery);

                if (success)
                {
                    // Don't reset failure count - keep it cumulative for historical tracking
                    await _webhookRepository.UpdateLastTriggeredAsync(webhook.Id, DateTime.UtcNow);
                }
                else
                {
                    await _webhookRepository.IncrementFailureCountAsync(webhook.Id);
                    
                    // Retry logic
                    if (delivery.AttemptCount < webhook.RetryCount)
                    {
                        // Schedule retry (in a real implementation, you'd use a background job queue)
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromMinutes(Math.Pow(2, delivery.AttemptCount))); // Exponential backoff
                            delivery.AttemptCount++;
                            await SendWebhookRequestAsync(webhook, delivery);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error delivering webhook {webhook.Id} for event {eventType}");
                await _webhookRepository.IncrementFailureCountAsync(webhook.Id);
            }
        }

        private async Task<bool> SendWebhookRequestAsync(Webhook webhook, WebhookDelivery delivery)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(webhook.TimeoutSeconds);

                var payloadJson = JsonConvert.SerializeObject(new
                {
                    @event = delivery.EventType,
                    data = delivery.Payload,
                    timestamp = delivery.CreatedAt,
                    delivery_id = delivery.Id,
                    webhook_id = webhook.Id
                });

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(webhook.Url),
                    Content = new StringContent(payloadJson, Encoding.UTF8)
                    {
                        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
                    }
                };

                // Add custom headers
                foreach (var header in webhook.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Add standard headers
                request.Headers.TryAddWithoutValidation("User-Agent", "Smartlead-Webhooks/1.0");
                request.Headers.TryAddWithoutValidation("X-Webhook-Event", delivery.EventType);
                request.Headers.TryAddWithoutValidation("X-Webhook-Delivery", delivery.Id);

                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                await _webhookRepository.UpdateDeliveryStatusAsync(
                    delivery.Id,
                    (int)response.StatusCode,
                    responseBody.Length > 1000 ? responseBody.Substring(0, 1000) + "..." : responseBody
                );

                var isSuccess = response.IsSuccessStatusCode;
                
                _logger.LogInformation(
                    $"Webhook {webhook.Id} delivered for event {delivery.EventType}. " +
                    $"Status: {(int)response.StatusCode}, Success: {isSuccess}");

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending webhook request to {webhook.Url}");

                await _webhookRepository.UpdateDeliveryStatusAsync(
                    delivery.Id,
                    0, // No status code
                    null,
                    ex.Message
                );

                return false;
            }
        }

        public async Task<bool> SendWebhookToUrlAsync(string url, object payload, int timeoutSeconds = 30)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                var payloadJson = JsonConvert.SerializeObject(new
                {
                    @event = "campaign.created",
                    data = payload,
                    timestamp = DateTime.UtcNow,
                    delivery_id = Guid.NewGuid().ToString(),
                    callback_url = url
                });

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                // Add standard headers
                request.Headers.TryAddWithoutValidation("User-Agent", "Smartlead-Webhooks/1.0");
                request.Headers.TryAddWithoutValidation("X-Webhook-Event", "campaign.created");
                request.Headers.TryAddWithoutValidation("X-Callback-Url", url);

                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                var isSuccess = response.IsSuccessStatusCode;
                
                _logger.LogInformation(
                    $"Callback sent to {url} for campaign creation. " +
                    $"Status: {(int)response.StatusCode}, Success: {isSuccess}");

                if (!isSuccess)
                {
                    _logger.LogWarning(
                        $"Callback failed. Status: {(int)response.StatusCode}, " +
                        $"Response: {(responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody)}");
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending callback to {url}");
                return false;
            }
        }

        private static Dictionary<string, object> ConvertToStringObjectDictionary(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
    }
}