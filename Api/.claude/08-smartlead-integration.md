# Smartlead API Integration

**Service:** SmartleadApiService ([Core/Services/ExternalApi/SmartleadApiService.cs](../Core/Services/ExternalApi/SmartleadApiService.cs))
**Sync Service:** SmartleadSyncService ([Core/Services/ExternalApi/SmartleadSyncService.cs](../Core/Services/ExternalApi/SmartleadSyncService.cs))
**API Base:** Smartlead.ai REST API

## Hardcoded Client ID

**Important:** API calls use hardcoded client ID `3138`

## Sync Service

### Background Sync Loop
**Frequency:** Every 2 hours (infinite loop in Task.Run)
**Started:** [Program.cs:253-263](../Program.cs#L253-L263)

```csharp
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var syncService = scope.ServiceProvider.GetRequiredService<SmartleadSyncService>();
    await syncService.Fetch(); // Infinite loop with 2h delay
});
```

### Sync Operations (Sequential)

1. **FetchEmailAccounts()**
   - Syncs email account details
   - Warmup statistics (daily dictionaries)
   - Email health stats (daily normalized)
   - Stores in: `email_accounts`, `warmup_metrics`, `email_account_daily_stat_entries`

2. **FetchEmailAccountsFromCampaigns()**
   - Syncs all campaigns
   - Associates email accounts
   - Stores in: `campaigns` (with email_ids JSONB)

3. **UpdateCampaignCounts()**
   - Updates active/total campaign counts per email account

4. **FetchCampaignTemplates()**
   - Caches email sequences and A/B variants
   - Stores in: `email_templates`, `email_template_variants`

5. **FetchCampaignLeads()**
   - Fetches lead conversations
   - Resumable sync with progress tracking
   - Transaction-protected for data integrity
   - Stores in: `lead_conversations`, `lead_email_history`

6. **AnalyzeLeadConversationsWithRevReply()**
   - Classifies REPLY emails using RevReply AI
   - Deduplicates via `classified_emails` table
   - Updates `lead_email_history.classification_result`

## SmartleadApiService Methods

### Campaign Operations

**Fetch:**
- `FetchCampaigns()` - Get all campaigns
- `FetchCampaignStats(id)` - Detailed stats with pagination
- `FetchLeads(campaignId)` - All leads
- `FetchCampaignTemplates(campaignId)` - Email sequences
- `FetchEmailAccountsOfCampaign(campaignId)` - Assigned accounts
- `FetchDaybyDayPositiveReplyStats(campaignId, start, end)` - Daily stats

**Modify:**
- `CreateCampaign(name, clientId)` - Create new
- `UpdateCampaignSettings(campaignId, request)` - Settings
- `ScheduleCampaign(campaignId, request)` - Timing config
- `ConfigureCampaignSequences(campaignId, request)` - Email sequences
- `UploadCampaignLeadsAsync(campaignId, request)` - Upload leads

### Email Account Operations

- `FetchEmailAccountDetails()` - All accounts (paginated)
- `FetchEmailWarmupStats(emailId)` - Warmup metrics
- `FetchEmailStats(dateRange)` - Daily health stats

### Lead Operations

- `FetchLeadIds(campaignId)` - Lead IDs with status filter
- `FetchHistory(campaignId, leadId)` - Conversation history
- `FetchLeadHistory(campaignId, leadId)` - Email thread
- `FetchInboxReplies(offset, limit, campaignId)` - Inbox with filter
- `FetchMasterInboxReplies(offset, limit, filters)` - Advanced inbox

## API Response Patterns

### Pagination
Most endpoints use:
```json
{
  "limit": 100,
  "offset": 0,
  "total": 500,
  "data": [...]
}
```

### Rate Limiting
- **429 Status:** Automatic retry with delay
- **Retry Logic:** Built into SmartleadApiService

### Error Handling
- Logs errors but continues sync
- Partial failures don't stop entire sync

## Data Mapping

### Campaign ID Mapping
- **campaign_id** (INT) - Smartlead's campaign ID
- **id** (UUID) - Internal database ID

### Event Sourcing
Campaign statistics use `campaign_events` table:
```sql
SELECT add_campaign_event('campaign-uuid', 'sent', 50);
```

Event types: sent, opened, replied, positive, bounced, clicked

### Normalization
Email account stats stored two ways:
- **Legacy:** JSONB dictionaries (warmup_*_dictionary)
- **New:** Normalized daily entries (email_account_daily_stat_entries)

## Resumable Sync

**campaign_sync_progress table:**
```sql
CREATE TABLE campaign_sync_progress (
    campaign_id INT PRIMARY KEY,
    last_processed_lead_id VARCHAR(50),
    leads_processed INT,
    total_leads_in_campaign INT,
    sync_status VARCHAR(20), -- not_started, in_progress, completed, failed
    sync_started_at TIMESTAMP,
    sync_completed_at TIMESTAMP
);
```

**Process:**
1. Check progress table
2. Resume from last_processed_lead_id if in_progress
3. Update progress after each batch
4. Mark completed when done

## RevReply Classification

**Service:** RevReplyClassificationService
**API:** RevReply AI classification API
**Config:** [appsettings.json](../appsettings.json) - RevReplyApi:ApiKey

**Classifications:**
- POSITIVE_REPLY
- NEGATIVE_REPLY
- NEUTRAL_REPLY
- OUT_OF_OFFICE
- UNSUBSCRIBE

**Deduplication:**
Uses `classified_emails` table with:
- `message_id` (unique from Smartlead)
- `email_body_hash` (SHA256 of content)

## Performance Optimizations

1. **Batch Fetching** - Pagination for large datasets
2. **Progress Tracking** - Resumable syncs
3. **Event Sourcing** - Immutable campaign events
4. **Deduplication** - Hash-based duplicate detection
5. **Transaction Protection** - Data integrity for lead syncs

## Configuration

**appsettings.json:**
```json
{
  "MultiloginToken": "...",  // Browser automation
  "RevReplyApi": {
    "ApiKey": "..."
  }
}
```

**Hardcoded:**
- Smartlead API key (in SmartleadApiService)
- Client ID: 3138

## Monitoring

**Logs:**
- Sync start/complete timestamps
- Lead processing progress
- API errors and retries
- Classification results

## Related Services

- **[07-services.md](07-services.md)** - SmartleadSyncService details
- **[04-database-schema.md](04-database-schema.md)** - Tables updated by sync
- **[01-architecture.md](01-architecture.md)** - Background task startup
