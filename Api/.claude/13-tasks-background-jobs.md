# Background Tasks & Jobs

## Task Manager

**File:** [Core/Managers/TaskManager.cs](../Core/Managers/TaskManager.cs)
**Lifecycle:** Singleton
**Purpose:** Manages Google OAuth automation tasks

### Functionality
- Creates async tasks for OAuth flows
- Tracks task state (ID, IsCompleted, IsSuccess, Message)
- Coordinates with MultiLogin for browser automation
- Provides task status polling
- Callbacks to external URLs on completion

### Usage
```csharp
public class TaskManager
{
    public int Create(OAuthRequest request, int? id, int? userId)
    {
        TaskModel task = CreateTaskId();

        Task.Run(() =>
        {
            // 1. Start Multilogin profile
            // 2. Connect Selenium
            // 3. Login to workspace account
            // 4. Perform OAuth
            // 5. Callback with result
        });

        return task.Id;
    }

    public TaskModel? GetTaskById(int id) { ... }
}
```

### Task Model
```csharp
public class TaskModel
{
    public int Id { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}
```

## MultiLogin Manager

**File:** [ServiceApis/MultiLogin.cs](../Core/ServiceApis/MultiLogin.cs)
**Lifecycle:** Singleton
**Purpose:** Browser automation via Multilogin API

### Functionality
- Starts quick browser profiles with proxies
- Provides remote debugging ports for Selenium
- Manages profile lifecycle (start/stop)
- Checks if Multilogin service is running

### QuickProfileResponse
```csharp
public class QuickProfileResponse
{
    public bool IsSuccess { get; set; }
    public string Port { get; set; }
    public string Id { get; set; }
}
```

## Background Services (IHostedService)

### 1. SessionCleanupService
**File:** [Core/Services/Authentication/SessionCleanupService.cs](../Core/Services/Authentication/SessionCleanupService.cs)
**Registration:** [Program.cs:114](../Program.cs#L114)
**Interval:** Every 1 hour

```csharp
public class SessionCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionRepo = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

                // Delete sessions older than 30 days or expired refresh tokens
                await sessionRepo.DeleteExpiredSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up sessions");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

**Cleanup Logic:**
- Deletes sessions where `refresh_token_expiry_time < NOW()`
- Deletes sessions where `last_accessed_at < NOW() - INTERVAL '30 days'`

### 2. WebhookEventMonitoringService
**File:** [Core/Services/BackgroundServices/WebhookEventMonitoringService.cs](../Core/Services/BackgroundServices/WebhookEventMonitoringService.cs)
**Registration:** [Program.cs:115](../Program.cs#L115)
**Interval:** Every 5 minutes

```csharp
public class WebhookEventMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for app to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var monitoringService = scope.ServiceProvider
                    .GetRequiredService<ICampaignMetricsMonitoringService>();

                await monitoringService.CheckCampaignMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in webhook event monitoring");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

**Monitored Events:**
- Reply rate drops below threshold
- Bounce rate exceeds threshold
- No positive replies for X days
- No replies for X days
- Emails sent reaches threshold

## Startup Tasks (Task.Run)

### SmartleadSyncService
**File:** [Core/Services/ExternalApi/SmartleadSyncService.cs](../Core/Services/ExternalApi/SmartleadSyncService.cs)
**Started:** [Program.cs:253-263](../Program.cs#L253-L263)
**Type:** Infinite loop with 2-hour delay
**Non-blocking:** Uses `Task.Run()` - doesn't block startup

```csharp
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();

    // Initialize default admin
    var initService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    await initService.InitializeAsync();

    // Start sync loop
    var syncService = scope.ServiceProvider.GetRequiredService<SmartleadSyncService>();
    await syncService.Fetch(); // Infinite loop
});
```

**Sync Operations:**
1. Email accounts + warmup + daily stats
2. Campaigns + email account associations
3. Campaign counts
4. Email templates
5. Lead conversations (resumable)
6. AI classification

**Loop Structure:**
```csharp
public async Task Fetch()
{
    while (true)
    {
        try
        {
            await FetchEmailAccounts();
            await FetchEmailAccountsFromCampaigns();
            await UpdateCampaignCounts();
            await FetchCampaignTemplates();
            await FetchCampaignLeads();
            await AnalyzeLeadConversationsWithRevReply();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync error");
        }

        await Task.Delay(TimeSpan.FromHours(2));
    }
}
```

## Service Registration

**[Program.cs](../Program.cs):**
```csharp
// Singletons (app lifetime)
builder.Services.AddSingleton<MultiLogin>();
builder.Services.AddSingleton<TaskManager>();

// Scoped (per request)
builder.Services.AddScoped<SmartleadSyncService>();
builder.Services.AddScoped<ICampaignMetricsMonitoringService, CampaignMetricsMonitoringService>();

// Background Services (hosted)
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<WebhookEventMonitoringService>();
```

## Scoping Pattern

Background services must create their own scopes:

```csharp
using var scope = _serviceProvider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IMyService>();
await service.DoWorkAsync();
// Scope disposed here, releasing scoped resources
```

**Why?** Background services are singletons, can't directly inject scoped services

## Task Coordination

```
Startup
  ├─> Task.Run (non-blocking)
  │   ├─> DatabaseInitializationService
  │   └─> SmartleadSyncService (infinite loop)
  │
  ├─> IHostedService (managed by host)
  │   ├─> SessionCleanupService (1h interval)
  │   └─> WebhookEventMonitoringService (5min interval)
  │
  └─> app.Run() (blocks until shutdown)
```

## Graceful Shutdown

**CancellationToken** propagates shutdown signal:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await DoWork();
        await Task.Delay(interval, stoppingToken);
    }
}
```

**Shutdown sequence:**
1. Host signals cancellation
2. Background services stop their loops
3. In-progress work completes (or times out)
4. Application exits

## Logging

All background tasks use ILogger:
```csharp
_logger.LogInformation("Starting background service");
_logger.LogError(ex, "Error in background service");
```

## Best Practices

1. **Always use CancellationToken** for cooperative cancellation
2. **Create scopes** for scoped services in background work
3. **Catch exceptions** - don't crash background services
4. **Log extensively** - background failures are silent
5. **Delay between iterations** - prevent tight loops
6. **Use Task.Delay with token** - allows graceful shutdown

## Common Pitfalls

❌ **Don't inject scoped services directly:**
```csharp
// WRONG
public MyBackgroundService(IUserRepository repo) { ... }
```

✅ **Do create scopes:**
```csharp
// CORRECT
using var scope = _serviceProvider.CreateScope();
var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
```

❌ **Don't block startup:**
```csharp
// WRONG - blocks app.Run()
await SmartleadSyncService.Fetch();
```

✅ **Do use Task.Run:**
```csharp
// CORRECT - non-blocking
_ = Task.Run(async () => await SmartleadSyncService.Fetch());
```

## Monitoring

**Check logs for:**
- Background service startup messages
- Iteration completion logs
- Error logs (critical for debugging)
- Performance metrics (sync duration, record counts)

## Related Docs

- [07-services.md](07-services.md) - SmartleadSyncService details
- [08-smartlead-integration.md](08-smartlead-integration.md) - Sync operations
- [01-architecture.md](01-architecture.md) - DI and startup sequence
