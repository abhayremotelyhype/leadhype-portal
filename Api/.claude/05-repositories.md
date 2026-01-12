# Repositories - Data Access Layer

**Pattern:** Repository pattern with Dapper ORM
**Location:** [Core/Database/Repositories/](../Core/Database/Repositories/)
**Connection:** IDbConnectionService (singleton, pooled)

## All Repositories (32 total)

### Campaign
- **ICampaignRepository** / CampaignRepository
- **ICampaignEventRepository** / CampaignEventRepository
- **ICampaignDailyStatEntryRepository** / CampaignDailyStatEntryRepository
- **IEmailTemplateRepository** / EmailTemplateRepository
- **ILeadConversationRepository** / LeadConversationRepository
- **ILeadEmailHistoryRepository** / LeadEmailHistoryRepository
- **IClassifiedEmailRepository** / ClassifiedEmailRepository

### Email Accounts
- **IEmailAccountRepository** / EmailAccountRepository
- **IEmailAccountDailyStatEntryRepository** / EmailAccountDailyStatEntryRepository
- **IEmailAccountStatsDateRepository** / EmailAccountStatsDateRepository

### Core
- **IClientRepository** / ClientRepository
- **IUserRepository** / UserRepository
- **IUserSessionRepository** / UserSessionRepository
- **ISettingsRepository** / SettingsRepository

### API & Webhooks
- **IApiKeyRepository** / ApiKeyRepository
- **IWebhookRepository** / WebhookRepository
- **IWebhookEventConfigRepository** / WebhookEventConfigRepository
- **IWebhookEventTriggerRepository** / WebhookEventTriggerRepository

## Common Patterns

### Standard CRUD
```csharp
Task<T?> GetByIdAsync(string id);
Task<IEnumerable<T>> GetAllAsync();
Task<string> CreateAsync(T entity);
Task<bool> UpdateAsync(T entity);
Task<bool> DeleteAsync(string id);
```

### Dapper Usage
```csharp
public async Task<Campaign?> GetByIdAsync(string id)
{
    const string sql = @"
        SELECT id, name, client_id, status, ...
        FROM campaigns
        WHERE id = @Id";

    using var connection = await _connectionService.GetConnectionAsync();
    return await connection.QuerySingleOrDefaultAsync<Campaign>(sql, new { Id = id });
}
```

### Connection Pattern
```csharp
using var connection = await _connectionService.GetConnectionAsync();
// Connection automatically disposed, returned to pool
```

## Key Repository Features

### CampaignRepository
- `GetByClientIdAsync(clientId)` - Filter by client
- `GetByAdminUuidAsync(adminUuid)` - Multi-tenant filtering
- `SearchAsync(query, clientIds)` - Full-text search
- Pagination support

### CampaignEventRepository
- `AddEventAsync(campaignId, eventType, count)` - Event sourcing
- `GetAggregatedTotalsForCampaignsAsync(campaignIds, startDate, endDate)` - Batch aggregation
- `GetStatsForCampaignsAsync(campaignIds, startDate, endDate)` - Daily breakdown
- Uses UPSERT pattern for events

### EmailAccountRepository
- `GetByEmailAsync(email)` - Lookup by email
- `GetByClientIdAsync(clientId)` - Client filtering
- `UpdateWarmupStatsAsync()` - Warmup metrics update
- `GetAllWithStatsAsync()` - Joined with stats

### UserRepository
- `GetByEmailAsync(email)` - Login lookup
- `GetByUsernameAsync(username)` - Alternative lookup
- Passwords stored as BCrypt hash

### UserSessionRepository
- `GetByRefreshTokenAsync(token)` - Token refresh flow
- `DeleteExpiredSessionsAsync()` - Cleanup (>30 days)
- `GetByUserIdAsync(userId)` - All user sessions

### ApiKeyRepository
- `GetByKeyHashAsync(hash)` - API key validation
- `GetByUserIdAsync(userId)` - User's keys
- `LogUsageAsync(usage)` - Track API calls
- Keys stored as SHA256 hash

## Transaction Support

Use IDbConnectionService for manual transactions:
```csharp
using var connection = await _connectionService.GetConnectionAsync();
using var transaction = connection.BeginTransaction();

try
{
    await _repo1.CreateAsync(entity1, transaction);
    await _repo2.CreateAsync(entity2, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## DI Registration

All repositories registered as **Scoped** in [Program.cs:56-73](../Program.cs#L56-L73):
```csharp
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();
// ... etc
```

## Performance Tips

1. **Use batch methods** when fetching multiple entities
2. **Filter by admin_uuid** for multi-tenancy
3. **Use pagination** for large result sets
4. **Leverage indexes** - see [04-database-schema.md](04-database-schema.md)
5. **Connection pooling** handles concurrent requests

## Related Docs

- [04-database-schema.md](04-database-schema.md) - Table structures
- [07-services.md](07-services.md) - Services using repositories
- [01-architecture.md](01-architecture.md) - DI lifecycle
