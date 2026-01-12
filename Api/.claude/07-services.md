# Services Overview

**Total Services:** 20+
**Registration:** [Program.cs:82-115](../Program.cs#L82-L115)

## Service Categories

### 🎯 Core Business Services

#### CampaignService
**File:** [Core/Services/Campaign/CampaignService.cs](../Core/Services/Campaign/CampaignService.cs)
**Lifecycle:** Scoped
- Creates campaigns via Smartlead API
- Calculates time-range filtered statistics
- Configures email sequences and A/B variants
- Uploads leads with custom fields

#### DashboardService
**File:** [Core/Services/Analytics/DashboardService.cs](../Core/Services/Analytics/DashboardService.cs)
**Lifecycle:** Scoped
- Dashboard overview with role-based filtering
- Performance trends (time-series data)
- Top campaigns/clients rankings
- Recent activities feed

#### CampaignMetricsMonitoringService
**File:** [Core/Services/Campaign/CampaignMetricsMonitoringService.cs](../Core/Services/Campaign/CampaignMetricsMonitoringService.cs)
**Lifecycle:** Scoped
- Monitors campaign thresholds (replies, positive replies, sent)
- Triggers webhooks when thresholds exceeded
- Used by WebhookEventMonitoringService background job

### 🔐 Authentication Services

#### AuthService
**File:** [Core/Services/Authentication/AuthService.cs](../Core/Services/Authentication/AuthService.cs)
**Lifecycle:** Scoped
- JWT token generation (access + refresh)
- Password verification (BCrypt)
- Session management
- User CRUD operations

#### ApiKeyService
**File:** [Core/Services/Authentication/ApiKeyService.cs](../Core/Services/Authentication/ApiKeyService.cs)
**Lifecycle:** Extension methods
- API key generation (SHA256 hashing)
- Validation and permission checking
- Usage logging
- Rate limit enforcement

#### RateLimitService
**File:** [Core/Services/Authentication/RateLimitService.cs](../Core/Services/Authentication/RateLimitService.cs)
**Lifecycle:** Singleton
- In-memory sliding window rate limiting
- Used for login attempts (5/15min)
- API key rate limits (configurable)

#### DatabaseInitializationService
**File:** [Core/Services/Infrastructure/DatabaseInitializationService.cs](../Core/Services/Infrastructure/DatabaseInitializationService.cs)
**Lifecycle:** Scoped
- Creates default admin user on first run
- Username: `admin`, Password: `Admin123!`

### 📊 Analytics Services

#### ClientStatsService
**File:** [Core/Services/Analytics/ClientStatsService.cs](../Core/Services/Analytics/ClientStatsService.cs)
**Lifecycle:** Scoped
- Client-level statistics aggregation
- Reply timing metrics (emails per reply)
- Campaign/email account counts
- Pagination and filtering support

#### UserStatsService
**File:** [Core/Services/Analytics/UserStatsService.cs](../Core/Services/Analytics/UserStatsService.cs)
**Lifecycle:** Scoped
- User activity statistics
- Assigned client/campaign filtering
- Role-based access control
- API key usage tracking

#### EmailAccountDailyStatEntryService
**File:** [Core/Services/Campaign/EmailAccountDailyStatEntryService.cs](../Core/Services/Campaign/EmailAccountDailyStatEntryService.cs)
**Lifecycle:** Scoped
- Daily stat entry CRUD (UPSERT pattern)
- Batch operations for multiple accounts
- Date range aggregation
- Client-wide rollups

### 🌐 External API Services

#### SmartleadApiService
**File:** [Core/Services/ExternalApi/SmartleadApiService.cs](../Core/Services/ExternalApi/SmartleadApiService.cs)
**Lifecycle:** Not registered (instantiated manually)

**Key Methods:**
- **Campaigns:** Fetch, create, update settings, schedule, configure sequences
- **Leads:** Fetch IDs, history, inbox replies, upload leads
- **Email Accounts:** Fetch details, warmup stats, daily health metrics
- **Statistics:** Day-by-day positive reply stats

**Features:**
- Automatic retry on 429 (rate limit)
- Pagination handling
- HTML to text conversion

#### SmartleadSyncService
**File:** [Core/Services/ExternalApi/SmartleadSyncService.cs](../Core/Services/ExternalApi/SmartleadSyncService.cs)
**Lifecycle:** Scoped
**Frequency:** Every 2 hours

**Sync Operations (in order):**
1. Email accounts + warmup + daily stats
2. Campaigns + email account associations
3. Campaign counts update
4. Email templates + variants
5. Lead conversations (resumable sync)
6. AI classification of replies

**Features:**
- Transaction-protected lead sync
- Progress tracking (resumable)
- Event-sourced statistics
- Duplicate detection (SHA256)

#### RevReplyClassificationService
**File:** [Core/Services/ExternalApi/RevReplyClassificationService.cs](../Core/Services/ExternalApi/RevReplyClassificationService.cs)
**Lifecycle:** Scoped
- Classifies email replies via AI
- Categories: POSITIVE_REPLY, NEGATIVE_REPLY, NEUTRAL_REPLY, OUT_OF_OFFICE, UNSUBSCRIBE
- Prevents duplicate API calls via classified_emails table

#### WebhookService
**File:** [Core/Services/ExternalApi/WebhookService.cs](../Core/Services/ExternalApi/WebhookService.cs)
**Lifecycle:** Scoped
- Webhook CRUD operations
- HTTP callback delivery with retry
- HMAC-SHA256 signature generation
- Delivery tracking

### ⏰ Background Services

#### SessionCleanupService
**File:** [Core/Services/Authentication/SessionCleanupService.cs](../Core/Services/Authentication/SessionCleanupService.cs)
**Type:** IHostedService
**Frequency:** Every 1 hour
- Deletes expired sessions (>7 days)
- Keeps database clean

#### WebhookEventMonitoringService
**File:** [Core/Services/BackgroundServices/WebhookEventMonitoringService.cs](../Core/Services/BackgroundServices/WebhookEventMonitoringService.cs)
**Type:** IHostedService
**Frequency:** Every 5 minutes
- Monitors campaign metrics
- Triggers configured webhooks
- Uses CampaignMetricsMonitoringService

## Service Interconnections

```
┌─────────────────────────┐
│ SmartleadSyncService    │ (Every 2h)
│ (Background Task.Run)   │
└────────┬────────────────┘
         │
         ├─> SmartleadApiService (External)
         ├─> EmailAccountDailyStatEntryService
         ├─> CampaignEventRepository (Event sourcing)
         └─> RevReplyClassificationService

┌─────────────────────────┐
│ Controller              │
└────────┬────────────────┘
         │
         ├─> DashboardService
         ├─> CampaignService
         │   └─> SmartleadApiService
         │
         ├─> AuthService
         │   ├─> RateLimitService
         │   └─> UserRepository
         │
         └─> WebhookService

┌──────────────────────────┐
│ Background Services      │
└────────┬─────────────────┘
         │
         ├─> SessionCleanupService (1h)
         │
         └─> WebhookEventMonitoringService (5min)
             └─> CampaignMetricsMonitoringService
                 └─> WebhookService
```

## DI Registration Summary

**From [Program.cs](../Program.cs):**

```csharp
// Singleton (app lifetime)
builder.Services.AddSingleton<IDbConnectionService, PostgreSqlConnectionService>();
builder.Services.AddSingleton<JwtSettings>();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddSingleton<MultiLogin>();
builder.Services.AddSingleton<TaskManager>();

// Scoped (per request)
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IEmailAccountDailyStatEntryService, EmailAccountDailyStatEntryService>();
builder.Services.AddScoped<ICampaignMetricsMonitoringService, CampaignMetricsMonitoringService>();
builder.Services.AddScoped<IRevReplyClassificationService, RevReplyClassificationService>();
builder.Services.AddScoped<SmartleadSyncService>();
// ... all repositories are scoped

// Background Services (Hosted)
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<WebhookEventMonitoringService>();
```

## Key Service Patterns

### 1. Repository Pattern
All services depend on `I*Repository` interfaces for data access
```csharp
public CampaignService(ICampaignRepository repo, ...) { }
```

### 2. Scoped Lifetime
Most services are scoped to prevent state leaks between requests

### 3. Background Initialization
SmartleadSyncService runs via `Task.Run()` on startup (non-blocking)

### 4. Event Sourcing
CampaignEventRepository uses immutable event log for statistics

### 5. Batch Operations
Services provide batch methods for performance:
- `GetBatchTotalStatsAsync()` - Multiple accounts at once
- `GetAggregatedTotalsForCampaignsAsync()` - Multiple campaigns

## Performance Optimizations

1. **Connection Pooling** - IDbConnectionService singleton with 5-20 connections
2. **Event Sourcing** - Immutable campaign_events table for fast aggregation
3. **Batch Queries** - Fetch multiple entities in single query
4. **Progress Tracking** - Resumable syncs (campaign_sync_progress table)
5. **Deduplication** - SHA256 hashes prevent duplicate processing

## Configuration

**Background Service Timing:**
- SmartleadSyncService: 2 hours (Task.Run loop)
- SessionCleanupService: 1 hour
- WebhookEventMonitoringService: 5 minutes

**Rate Limiting:**
- Login: 5 attempts / 15 minutes per IP+email
- API Keys: Configurable per key (default 10,000/hour)

## Common Service Workflows

### Dashboard Load
```
User → DashboardController
  → DashboardService.GetDashboardOverviewAsync()
    → CampaignRepository.GetAllAsync() (filtered by user)
    → EmailAccountRepository.GetAllAsync()
    → CampaignEventRepository.GetAggregatedTotals()
  → Return stats
```

### Campaign Creation
```
User → CampaignController.CreateCampaignAsync()
  → CampaignService.CreateCampaignAsync()
    → SmartleadApiService.CreateCampaign() (external API)
    → SmartleadApiService.UpdateCampaignSettings()
    → SmartleadApiService.ScheduleCampaign()
    → CampaignRepository.CreateAsync() (local DB)
  → Return campaign
```

### Data Sync (Background)
```
Startup → Task.Run()
  → SmartleadSyncService.Fetch() (loop every 2h)
    → SmartleadApiService.FetchEmailAccounts()
    → EmailAccountRepository.UpsertAsync()
    → SmartleadApiService.FetchCampaigns()
    → CampaignRepository.UpsertAsync()
    → SmartleadApiService.FetchLeads()
    → LeadConversationRepository.UpsertAsync()
    → RevReplyClassificationService.ClassifyEmailAsync()
```

## Testing Notes

All services use dependency injection - easy to mock:
- Mock `I*Repository` for database
- Mock `HttpClient` for external APIs
- Mock `ILogger<T>` for logging verification

## Related Documentation

- [01-architecture.md](01-architecture.md) - DI setup and lifecycle
- [04-database-schema.md](04-database-schema.md) - Tables accessed by services
- [05-repositories.md](05-repositories.md) - Data access layer
- [08-smartlead-integration.md](08-smartlead-integration.md) - External API details
