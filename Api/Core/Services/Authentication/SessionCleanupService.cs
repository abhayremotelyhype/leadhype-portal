using LeadHype.Api.Core.Database.Repositories;

namespace LeadHype.Api.Services;

/// <summary>
/// Background service that periodically cleans up expired user sessions
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run cleanup every hour

    public SessionCleanupService(
        ILogger<SessionCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired sessions.");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Session Cleanup Service is stopping.");
    }

    private async Task CleanupExpiredSessions()
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

        try
        {
            var expiredSessions = await sessionRepository.GetExpiredSessionsAsync();
            
            if (expiredSessions.Any())
            {
                _logger.LogInformation("Found {Count} expired sessions to clean up", expiredSessions.Count());
                
                foreach (var session in expiredSessions)
                {
                    await sessionRepository.DeleteAsync(session.Id);
                }
                
                _logger.LogInformation("Successfully cleaned up {Count} expired sessions", expiredSessions.Count());
            }
            else
            {
                _logger.LogDebug("No expired sessions found during cleanup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sessions");
            throw;
        }
    }
}