using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Services.BackgroundServices;

/// <summary>
/// Background service that periodically monitors webhook events and triggers webhooks when conditions are met
/// </summary>
public class WebhookEventMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookEventMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public WebhookEventMonitoringService(
        IServiceProvider serviceProvider, 
        ILogger<WebhookEventMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Event Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing webhook event monitoring");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Webhook Event Monitoring Service stopped");
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var campaignMetricsMonitoringService = scope.ServiceProvider.GetRequiredService<ICampaignMetricsMonitoringService>();

        _logger.LogDebug("Starting webhook event monitoring cycle");

        try
        {
            await campaignMetricsMonitoringService.CheckMetricsThresholdsAsync();
            _logger.LogDebug("Webhook event monitoring cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during webhook event monitoring cycle");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Event Monitoring Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}