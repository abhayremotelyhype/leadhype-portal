using Dapper;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace LeadHype.Api.Core.Database;

public interface IDatabaseMigrationService
{
    Task MigrateAsync();
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbConnectionService _connectionService;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseMigrationService(IDbConnectionService connectionService, ILogger<DatabaseMigrationService> logger, IConfiguration configuration)
    {
        _connectionService = connectionService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task MigrateAsync()
    {
        try
        {
            _logger.LogInformation("Starting database migration...");

            // Consolidated migration - all previous migrations (001-021) merged into single file
            var migrations = new[]
            {
                new {
                    Script = "001_InitialSchema.sql",
                    RequiredTables = new[] {
                        "users", "clients", "email_accounts", "campaigns", "campaign_events", "settings", "user_sessions",
                        "warmup_metrics", "campaign_daily_stat_entries", "email_account_daily_stat_entries",
                        "campaign_analytics", "api_keys", "api_key_usage", "webhooks", "webhook_deliveries",
                        "rate_limits", "email_templates", "lead_conversations", "lead_email_history",
                        "classified_emails"
                    }
                },
                new {
                    Script = "002_AddLeadSyncTracking.sql",
                    RequiredTables = new[] {
                        "campaign_sync_progress"
                    }
                },
                new {
                    Script = "003_EmailTemplateVariants.sql",
                    RequiredTables = new[] {
                        "email_template_variants"
                    }
                },
                new {
                    Script = "004_AddCampaignIdUniqueConstraint.sql",
                    RequiredTables = new string[] { } // No new tables, just adds constraint
                }
            };

            using var connection = await _connectionService.GetConnectionAsync();

            // Set search path to public schema for PostgreSQL 17 compatibility
            await connection.ExecuteAsync("SET search_path TO public");

            // Check if migrations tracking table exists
            var migrationsTableExists = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT 1 FROM information_schema.tables WHERE table_name = '__migrations' AND table_schema = 'public'");

            // Create migrations tracking table if it doesn't exist
            if (!migrationsTableExists.HasValue)
            {
                const string createMigrationsTableSql = @"
                    CREATE TABLE public.__migrations (
                        id SERIAL PRIMARY KEY,
                        migration_name VARCHAR(255) NOT NULL UNIQUE,
                        applied_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    )";

                await connection.ExecuteAsync(createMigrationsTableSql);
            }

            // Apply each migration script
            foreach (var migration in migrations)
            {
                var migrationExists = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT 1 FROM __migrations WHERE migration_name = @MigrationName",
                    new { MigrationName = migration.Script });

                // Check if required tables exist even if migration was marked as applied
                var needsToRun = !migrationExists.HasValue;
                
                if (migrationExists.HasValue)
                {
                    // Migration was applied before, but check if tables still exist
                    var missingTables = new List<string>();
                    foreach (var tableName in migration.RequiredTables)
                    {
                        var tableExists = await connection.QueryFirstOrDefaultAsync<int?>(
                            "SELECT 1 FROM information_schema.tables WHERE table_name = @TableName AND table_schema = 'public'",
                            new { TableName = tableName });
                            
                        if (!tableExists.HasValue)
                        {
                            missingTables.Add(tableName);
                        }
                    }
                    
                    if (missingTables.Any())
                    {
                        _logger.LogWarning("Migration {ScriptName} was applied but tables are missing: {MissingTables}. Re-running migration...", 
                            migration.Script, string.Join(", ", missingTables));
                        needsToRun = true;
                        
                        // Remove the old migration record so we can re-apply
                        await connection.ExecuteAsync(
                            "DELETE FROM __migrations WHERE migration_name = @MigrationName",
                            new { MigrationName = migration.Script });
                    }
                    else
                    {
                        _logger.LogInformation("Migration {ScriptName} already applied and all tables exist, skipping...", migration.Script);
                    }
                }

                if (!needsToRun) continue;

                _logger.LogInformation("Applying migration {ScriptName}...", migration.Script);

                // Read the migration script
                var scriptPath = Path.Combine(AppContext.BaseDirectory, "Core", "Database", "Migrations", migration.Script);
                
                if (!File.Exists(scriptPath))
                {
                    _logger.LogError("Migration script not found: {ScriptPath}", scriptPath);
                    throw new FileNotFoundException($"Migration script not found: {scriptPath}");
                }

                var migrationSql = await File.ReadAllTextAsync(scriptPath);

                // Execute the migration script
                await connection.ExecuteAsync(migrationSql);

                // Record the migration as applied
                await connection.ExecuteAsync(
                    "INSERT INTO __migrations (migration_name) VALUES (@MigrationName)",
                    new { MigrationName = migration.Script });

                _logger.LogInformation("Migration {ScriptName} applied successfully", migration.Script);
            }

            // Also apply performance indexes if needed
            await ApplyPerformanceIndexesIfNeeded(connection);
            
            // Apply webhook event tables migration
            await ApplyWebhookEventTablesMigrationIfNeeded(connection);

            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database migration");
            throw;
        }
    }

    private async Task ApplyPerformanceIndexesIfNeeded(IDbConnection connection)
    {
        const string indexMigrationName = "004_PerformanceIndexes.sql";
        
        var migrationExists = await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT 1 FROM __migrations WHERE migration_name = @MigrationName",
            new { MigrationName = indexMigrationName });

        if (migrationExists.HasValue)
        {
            _logger.LogDebug("Performance indexes migration already applied, skipping...");
            return;
        }

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Core", "Database", "Migrations", indexMigrationName);
        
        if (!File.Exists(scriptPath))
        {
            _logger.LogDebug("Performance indexes script not found: {ScriptPath}, skipping...", scriptPath);
            return;
        }

        _logger.LogInformation("Applying performance indexes...");

        var migrationSql = await File.ReadAllTextAsync(scriptPath);
        await connection.ExecuteAsync(migrationSql);

        await connection.ExecuteAsync(
            "INSERT INTO __migrations (migration_name) VALUES (@MigrationName)",
            new { MigrationName = indexMigrationName });

        _logger.LogInformation("Performance indexes applied successfully");
    }

    private async Task ApplyWebhookEventTablesMigrationIfNeeded(IDbConnection connection)
    {
        const string migrationName = "015_WebhookEventTables";
        
        var migrationExists = await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT 1 FROM __migrations WHERE migration_name = @MigrationName",
            new { MigrationName = migrationName });

        // Check if required tables exist
        var requiredTables = new[] { "webhook_event_configs", "webhook_event_triggers" };
        var missingTables = new List<string>();
        
        foreach (var tableName in requiredTables)
        {
            var tableExists = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT 1 FROM information_schema.tables WHERE table_name = @TableName AND table_schema = 'public'",
                new { TableName = tableName });
                
            if (!tableExists.HasValue)
            {
                missingTables.Add(tableName);
            }
        }

        if (migrationExists.HasValue && !missingTables.Any())
        {
            _logger.LogDebug("Webhook event tables migration already applied and all tables exist, skipping...");
            return;
        }

        if (migrationExists.HasValue && missingTables.Any())
        {
            _logger.LogWarning("Webhook event tables migration was applied but tables are missing: {MissingTables}. Re-running migration...", 
                string.Join(", ", missingTables));
            
            // Remove the old migration record so we can re-apply
            await connection.ExecuteAsync(
                "DELETE FROM __migrations WHERE migration_name = @MigrationName",
                new { MigrationName = migrationName });
        }

        _logger.LogInformation("Applying webhook event tables migration...");

        try
        {
            // Apply the webhook event tables migration
            var connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var webhookEventMigration = new Migrations.AddWebhookEventTables(connectionString);
            await webhookEventMigration.ApplyMigrationAsync();

            // Record the migration as applied
            await connection.ExecuteAsync(
                "INSERT INTO __migrations (migration_name) VALUES (@MigrationName)",
                new { MigrationName = migrationName });

            _logger.LogInformation("Webhook event tables migration applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying webhook event tables migration");
            throw;
        }
    }
}