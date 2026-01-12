# Database Migrations

**Location:** [Core/Database/Migrations/](../Core/Database/Migrations/)
**Service:** DatabaseMigrationService ([Core/Database/DatabaseMigrationService.cs](../Core/Database/DatabaseMigrationService.cs))
**Tracking Table:** `__migrations`

## Migration Files

### 001_InitialSchema.sql
**Date:** 2025-09-30
**Description:** Consolidated schema combining migrations 001-021

**Creates:**
- All core tables (users, clients, campaigns, email_accounts, etc.)
- Indexes for performance
- Auto-update triggers for `updated_at` columns
- Helper functions (update_updated_at_column)

**Archived:** Old migrations 001-021 moved to `archive_old_migrations/`

### 002_AddLeadSyncTracking.sql
**Description:** Lead sync progress tracking

**Adds:**
- `campaign_sync_progress` table
- `lead_conversations.last_synced_at` column
- `lead_conversations.sync_status` column (pending, in_progress, completed, failed)
- Indexes for sync queries

**Purpose:** Enable resumable lead syncing in SmartleadSyncService

### 003_EmailTemplateVariants.sql
**Description:** A/B testing support for email templates

**Adds:**
- `email_template_variants` table
- Columns: template_id, smartlead_variant_id, variant_label (A, B, C...), subject, body
- Foreign key to email_templates
- Unique constraint on (template_id, variant_label)

**Purpose:** Support multiple template variants per sequence

### AddWebhookEventTables.cs (C# Migration)
**Number:** 015
**Description:** Webhook event monitoring

**Adds:**
- `webhook_event_configs` table
- `webhook_event_triggers` table
- Indexes for queries

**Purpose:** Enable threshold-based webhook triggering

## Migration Process

### Automatic on Startup
**[Program.cs:186-191](../Program.cs#L186-L191)**

```csharp
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
    await migrationService.MigrateAsync(); // BLOCKS until complete
}
```

### Migration Tracking

**__migrations table:**
```sql
CREATE TABLE __migrations (
    id SERIAL PRIMARY KEY,
    migration_name TEXT NOT NULL UNIQUE,
    applied_at TIMESTAMP DEFAULT NOW()
);
```

### How It Works

1. Check `__migrations` table for applied migrations
2. Load `.sql` files from Migrations folder
3. Execute unapplied migrations in order
4. Record completion in `__migrations`
5. Execute C# code migrations (AddWebhookEventTables.cs)

**Code Pattern:**
```csharp
public async Task MigrateAsync()
{
    var applied = await GetAppliedMigrationsAsync();
    var pending = GetPendingMigrations(applied);

    foreach (var migration in pending)
    {
        await ExecuteMigrationAsync(migration);
        await RecordMigrationAsync(migration.Name);
    }
}
```

## Auto-Update Triggers

Most tables have automatic `updated_at` trigger:

```sql
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE 'plpgsql';

CREATE TRIGGER update_campaigns_updated_at
    BEFORE UPDATE ON campaigns
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
```

## Creating New Migrations

1. Create numbered SQL file: `004_YourMigrationName.sql`
2. Place in `Core/Database/Migrations/`
3. Migration auto-runs on next startup
4. For C# migrations, implement in DatabaseMigrationService

**SQL Migration Template:**
```sql
-- 004_AddYourFeature.sql

-- Add table
CREATE TABLE your_table (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Add indexes
CREATE INDEX idx_your_table_name ON your_table(name);

-- Add trigger
CREATE TRIGGER update_your_table_updated_at
    BEFORE UPDATE ON your_table
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
```

## Migration Best Practices

1. **Idempotent** - Use IF NOT EXISTS where possible
2. **Transactional** - Each migration runs in transaction
3. **Numbered** - Sequential numbering (001, 002, 003...)
4. **Tested** - Test on dev database first
5. **Documented** - Add comment at top explaining purpose

## Rollback Strategy

**No automatic rollback** - migrations are one-way

For rollback:
1. Create new migration to undo changes
2. Or manually execute SQL to revert
3. Remove entry from `__migrations` table if needed

## Related Files

- **[DatabaseMigrationService.cs](../Core/Database/DatabaseMigrationService.cs)** - Migration engine
- **[IDbConnectionService](../Core/Database/PostgreSqlConnectionService.cs)** - Connection management
- **[Program.cs](../Program.cs)** - Startup migration execution

## Migration History

| # | File | Date | Description |
|---|------|------|-------------|
| 001 | InitialSchema.sql | 2025-09-30 | Consolidated schema (001-021) |
| 002 | AddLeadSyncTracking.sql | - | Lead sync progress |
| 003 | EmailTemplateVariants.sql | - | A/B testing support |
| 015 | AddWebhookEventTables.cs | - | Webhook monitoring |
