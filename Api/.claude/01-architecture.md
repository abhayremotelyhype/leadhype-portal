# Architecture Overview

## Layered Architecture

```
┌─────────────────────────────────────────────────┐
│  HTTP Request (Client/Frontend)                 │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Middleware Pipeline                            │
│  - CORS                                         │
│  - Security Headers                             │
│  - Request Logging (dev only)                   │
│  - Authentication (JWT/ApiKey)                  │
│  - API Key Middleware                           │
│  - Session Validation (disabled currently)      │
│  - Authorization                                │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Controllers (18 total)                         │
│  - Validate input                               │
│  - Call services                                │
│  - Return responses                             │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Services (20 total)                            │
│  - Business logic                               │
│  - Orchestration                                │
│  - External API calls                           │
│  - Call repositories                            │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Repositories (32 total)                        │
│  - Data access via Dapper                       │
│  - SQL queries                                  │
│  - CRUD operations                              │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  PostgreSQL Database                            │
│  - Tables: Users, Clients, Campaigns, etc.      │
│  - Connection pooling (5-20 connections)        │
└─────────────────────────────────────────────────┘
```

## Dependency Injection

**DI Container Setup**: [Program.cs:37-157](../Program.cs#L37-L157)

### Lifecycle Patterns

#### Singleton (Application lifetime)
- `IDbConnectionService` - Connection pooling
- `JwtSettings` - JWT configuration
- `IRateLimitService` - Rate limiting state
- `MultiLogin` - Browser automation manager
- `TaskManager` - OAuth task tracking
- `ILogFactory` (DI.cs) - File logging

#### Scoped (Per request)
- All Repositories (I*Repository)
- Most Services (IAuthService, ICampaignService, etc.)
- `IDatabaseMigrationService`

#### Transient
- None explicitly registered (use scoped instead)

### Registration Example
```csharp
// Singleton for connection pooling
builder.Services.AddSingleton<IDbConnectionService, PostgreSqlConnectionService>();

// Scoped for per-request lifecycle
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
```

## Startup Sequence

**[Program.cs](../Program.cs)**

1. **Line 23-26**: Initialize custom DI (Logger)
2. **Line 28-45**: Configure WebApplicationBuilder (Kestrel, JSON, OpenAPI)
3. **Line 50-115**: Register all DI services
4. **Line 117-182**: Configure JWT + API Key authentication + CORS
5. **Line 184**: Build app
6. **Line 186-191**: Run database migrations (BLOCKING on startup)
7. **Line 194-250**: Configure middleware pipeline
8. **Line 253-263**: Background initialization (admin user, Smartlead sync)
9. **Line 265**: Run application

### Critical Startup Operations

```csharp
// Database migrations (blocking)
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
    await migrationService.MigrateAsync(); // BLOCKS until complete
}

// Background init (non-blocking)
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();

    // Create default admin if not exists
    var initService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    await initService.InitializeAsync();

    // Sync Smartlead data
    var mapperService = scope.ServiceProvider.GetRequiredService<SmartleadSyncService>();
    await mapperService.Fetch();
});
```

## Middleware Pipeline Order

**Order matters!** [Program.cs:194-250](../Program.cs#L194-L250)

1. **Static Files** - Serve wwwroot (CSS/JS with caching)
2. **Request Logging** - Dev only ([RequestLoggingMiddleware.cs](../Core/Middleware/RequestLoggingMiddleware.cs))
3. **Security Headers** - X-Content-Type-Options, X-Frame-Options, CSP, etc.
4. **CORS** - SmartleadPolicy (localhost + production origins)
5. **Authentication** - JWT Bearer validation
6. **API Key Auth** - Custom middleware ([ApiKeyAuthenticationMiddleware.cs](../Core/Middleware/ApiKeyAuthenticationMiddleware.cs))
7. **Session Validation** - DISABLED (line 248 commented out)
8. **Authorization** - Role/claims checking
9. **Controllers** - Route to endpoints

## Custom DI System

**[Core/DependencyInjection/DI.cs](../Core/DependencyInjection/DI.cs)**

Parallel DI system for legacy logging:

```csharp
public class DI
{
    public static IServiceProvider ServiceProvider { get; set; }
    public static ILogFactory Logger { get; set; } // File logger

    public static void Build(string basePath)
    {
        Logger = new BaseLogFactory([
            new FileLogger(Path.Combine(basePath, "Data", "Logs"))
        ]);
    }
}
```

Used via: `DI.Logger.Log("message")`

## Background Services

**IHostedService** - Run on application lifetime

1. **SessionCleanupService** ([Services/SessionCleanupService.cs](../Core/Services/SessionCleanupService.cs))
   - Runs every 1 hour
   - Removes expired UserSession records

2. **WebhookEventMonitoringService** ([Services/BackgroundServices/WebhookEventMonitoringService.cs](../Core/Services/BackgroundServices/WebhookEventMonitoringService.cs))
   - Monitors campaign metrics (opens, clicks, replies)
   - Triggers webhooks when thresholds met
   - Configured interval

Both registered: [Program.cs:114-115](../Program.cs#L114-L115)

## Manager Services

### TaskManager
**[Core/Managers/TaskManager.cs](../Core/Managers/TaskManager.cs)**

Manages Google OAuth flows via browser automation:
- Creates async tasks for OAuth operations
- Tracks task state (IsCompleted, IsSuccess, Message)
- Coordinates with MultiLogin for browser profiles
- Callbacks to external URLs on completion
- Thread-safe with SemaphoreSlim

### MultiLogin
**[ServiceApis/MultiLogin.cs](../Core/ServiceApis/MultiLogin.cs)** (assumed location)

Browser automation service:
- Starts quick browser profiles with proxies
- Provides remote debugging ports for Selenium
- Manages profile lifecycle

## Connection Pooling

**PostgreSQL Configuration** ([appsettings.json](../appsettings.json))

```
MinPoolSize=5
MaxPoolSize=20
ConnectionIdleLifetime=300 (5 minutes)
ConnectionPruningInterval=10 (seconds)
CommandTimeout=30 (seconds)
```

**Singleton IDbConnectionService** ensures connection reuse across requests.

## Kestrel Configuration

[Program.cs:31-35](../Program.cs#L31-L35) + [appsettings.json:17-29](../appsettings.json#L17-L29)

```csharp
options.Limits.MaxRequestHeaderCount = 100;
options.Limits.MaxRequestHeadersTotalSize = 32KB;
options.Limits.MaxConcurrentConnections = 100;
options.Limits.MaxRequestBodySize = 10MB;
options.Limits.KeepAliveTimeout = 2 minutes;
options.Limits.RequestHeadersTimeout = 30 seconds;
```

Binds to: `http://0.0.0.0:5010`

## Error Handling Pattern

Controllers generally return:
- **200 OK**: Success with data
- **400 Bad Request**: Validation errors
- **401 Unauthorized**: Auth failures
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Unhandled exceptions

Most controllers use try-catch with error responses.

## Async Patterns

- **All I/O operations** use async/await
- **Repository methods** return `Task<T>` or `Task`
- **Controllers** use `async Task<IActionResult>`
- **No synchronous blocking** in hot paths

## Key Design Decisions

1. **Dapper over EF Core**: Performance, control over SQL
2. **Repository Pattern**: Testability, separation of concerns
3. **Scoped Services**: Request isolation, no state leaks
4. **Connection Pooling**: Prevent connection exhaustion
5. **Manual Migrations**: SQL files, explicit control
6. **JWT + API Keys**: Dual auth for users and services
7. **Background Init**: Non-blocking startup for Smartlead sync
8. **Custom Logging**: Parallel to ASP.NET Core ILogger

## Cross-Cutting Concerns

- **Authentication**: [03-authentication.md](03-authentication.md)
- **Middleware Details**: [10-middleware-security.md](10-middleware-security.md)
- **Database Layer**: [04-database-schema.md](04-database-schema.md)
- **Services**: [07-services.md](07-services.md)
