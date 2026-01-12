# Database Schema Summary

**Database:** PostgreSQL
**Migration Files:** [Core/Database/Migrations/](../Core/Database/Migrations/)
**Latest:** 003_EmailTemplateVariants.sql

## Core Table Groups

### Users & Auth
- **users** - User accounts (id, email, username, password_hash, role, assigned_client_ids JSONB)
- **user_sessions** - Active sessions (refresh tokens, device tracking)
- **api_keys** - API key management (key_hash, permissions JSONB, rate_limit, ip_whitelist JSONB)
- **api_key_usage** - Usage logs

### Clients
- **clients** - Client organizations (id, name, email, company, status, color, notes)
- **settings** - Key-value store (key, value)

### Email Accounts
- **email_accounts** - Email account config (id BIGINT, email, client_id, sent/opened/replied/bounced totals, tags JSONB)
- **warmup_metrics** - Warmup stats with daily dictionaries (warmup_sent_dictionary JSONB)
- **email_account_daily_stat_entries** - Normalized daily stats (email_account_id, stat_date, sent, opened, replied, bounced)
- **email_account_stats_dates** - Date tracking

### Campaigns
- **campaigns** - Campaign details (id UUID, campaign_id INT, client_id, total_sent/opened/replied/bounced, email_ids JSONB, tags JSONB)
- **campaign_events** - Event sourcing (campaign_id, event_type, event_date, event_count) - UPSERT pattern
- **campaign_daily_stat_entries** - Daily stats (campaign_id, stat_date, sent, opened, clicked, replied, positive_replies, bounced)
- **campaign_sync_progress** - Resumable sync tracking
- **campaign_analytics** - Flexible JSONB storage

### Campaign Content
- **email_templates** - Email sequences (campaign_id, sequence_number, subject, body)
- **email_template_variants** - A/B testing (template_id, variant_label, smartlead_variant_id)
- **lead_conversations** - Lead tracking (campaign_id + lead_email UNIQUE, conversation_data TEXT, sync_status)
- **lead_email_history** - Email thread tracking (lead_id, sequence_number, type SENT|REPLY, classification_result)
- **classified_emails** - Deduplication tracking (message_id UNIQUE, email_body_hash SHA256)

### Webhooks
- **webhooks** - Webhook configs (user_id, url, events JSONB, headers JSONB, secret, retry_count)
- **webhook_deliveries** - Delivery logs
- **webhook_event_configs** - Event monitoring (event_type, config_parameters JSONB, target_scope JSONB)
- **webhook_event_triggers** - Trigger history

### Tracking
- **last_fetched_dates** - API fetch timestamps per admin
- **rate_limits** - Rate limiting tracking

## Key Relationships

```
clients (1:N)
  ├─> campaigns (client_id)
  └─> email_accounts (client_id)

campaigns (1:N)
  ├─> campaign_events
  ├─> campaign_daily_stat_entries
  ├─> email_templates (1:N) ─> email_template_variants
  ├─> lead_conversations
  └─> lead_email_history

email_accounts (1:1)
  ├─> warmup_metrics
  └─> email_account_daily_stat_entries (1:N)

users (1:N)
  ├─> user_sessions
  ├─> api_keys ─> api_key_usage
  └─> webhooks ─> webhook_deliveries
```

## Important Patterns

### Multi-tenancy
All tables have `admin_uuid` for tenant isolation - **ALWAYS filter by admin_uuid**

### Event Sourcing (Campaigns)
- Use `campaign_events` table for immutable event log
- Helper function: `add_campaign_event(campaign_id, event_type, count)` - UPSERT pattern
- Aggregate via `SUM()` queries for time-range filtering

### Daily Stats
- Two approaches: dictionaries (JSONB) vs normalized tables
- **Normalized (preferred)**: `*_daily_stat_entries` tables with stat_date
- **Dictionary (legacy)**: `warmup_*_dictionary` JSONB fields

### Deduplication
- `classified_emails.message_id` - Prevent duplicate API calls
- `classified_emails.email_body_hash` - SHA256 content hash
- `lead_conversations` - UNIQUE (campaign_id, lead_email)
- `campaign_daily_stat_entries` - UNIQUE (campaign_id, stat_date)

### Auto-update Triggers
Most tables have `update_*_updated_at` trigger for automatic `updated_at` timestamp

## Common Indexes

### GIN (JSONB)
- `email_accounts.tags`
- `campaigns.email_ids`
- `users.assigned_client_ids`

### Composite (Performance)
- `(campaign_id, stat_date)` - Time-series queries
- `(admin_uuid, is_active)` - Filtered listings
- `(email_account_id, campaign_id)` - Joins

### Unique Constraints
- Users: email, username
- API Keys: key_hash
- Campaign stats: (campaign_id, stat_date)
- Lead conversations: (campaign_id, lead_email)
- Classified emails: message_id

## Connection Info

**From [appsettings.json](../appsettings.json):**
- Host: localhost:5432
- Database: leadhype_db
- Pooling: Min 5, Max 20 connections
- Idle lifetime: 300s

## Migration Files

1. **001_InitialSchema.sql** - Consolidated schema (merged 001-021)
2. **002_AddLeadSyncTracking.sql** - Added sync_status, last_synced_at to lead_conversations
3. **003_EmailTemplateVariants.sql** - A/B testing support

**See:** [Core/Database/Migrations/](../Core/Database/Migrations/)

## Quick Reference Queries

### Get Campaign with Stats
```sql
SELECT c.*, cl.name AS client_name
FROM campaigns c
LEFT JOIN clients cl ON c.client_id = cl.id
WHERE c.id = $1;
```

### Time-range Campaign Stats (Event Sourced)
```sql
SELECT event_type, SUM(event_count) AS total
FROM campaign_events
WHERE campaign_id = $1
  AND event_date BETWEEN $2 AND $3
GROUP BY event_type;
```

### Daily Stats for Date Range
```sql
SELECT * FROM campaign_daily_stat_entries
WHERE campaign_id = $1
  AND stat_date BETWEEN $2 AND $3
ORDER BY stat_date;
```

### Email Account Warmup Progress
```sql
SELECT ea.email, wm.total_sent, wm.total_replied,
       wm.warmup_sent_dictionary
FROM email_accounts ea
JOIN warmup_metrics wm ON ea.id = wm.id
WHERE ea.admin_uuid = $1;
```

### Unclassified Replies
```sql
SELECT * FROM lead_email_history
WHERE type = 'REPLY'
  AND is_classified = FALSE
  AND admin_uuid = $1
LIMIT 100;
```

## Schema Files

**Models:** [Core/Models/Database/](../Core/Models/Database/)
- Campaign/
- EmailAccount/
- General/ (Client, Setting)
- User/
- WebhookEvent/

**Repositories:** [Core/Database/Repositories/](../Core/Database/Repositories/)
32 repositories for data access via Dapper

**See also:** [05-repositories.md](05-repositories.md) for repository patterns
