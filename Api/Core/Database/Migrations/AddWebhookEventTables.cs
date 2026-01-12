using Dapper;
using Npgsql;

namespace LeadHype.Api.Core.Database.Migrations;

public class AddWebhookEventTables
{
    private readonly string _connectionString;
    
    public AddWebhookEventTables(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task ApplyMigrationAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // Create webhook_event_configs table
        var createWebhookEventConfigsTable = @"
            CREATE TABLE IF NOT EXISTS webhook_event_configs (
                id VARCHAR(36) PRIMARY KEY,
                admin_uuid VARCHAR(36) NOT NULL,
                webhook_id VARCHAR(36) NOT NULL,
                event_type VARCHAR(50) NOT NULL,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                config_parameters JSONB NOT NULL DEFAULT '{}',
                target_scope JSONB NOT NULL DEFAULT '{}',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                last_checked_at TIMESTAMP WITH TIME ZONE,
                last_triggered_at TIMESTAMP WITH TIME ZONE
            )";
        
        // Create webhook_event_triggers table
        var createWebhookEventTriggersTable = @"
            CREATE TABLE IF NOT EXISTS webhook_event_triggers (
                id VARCHAR(36) PRIMARY KEY,
                event_config_id VARCHAR(36) NOT NULL,
                webhook_id VARCHAR(36) NOT NULL,
                campaign_id VARCHAR(36) NOT NULL,
                campaign_name VARCHAR(255) NOT NULL,
                trigger_data JSONB NOT NULL DEFAULT '{}',
                status_code INTEGER,
                response_body TEXT,
                error_message TEXT,
                is_success BOOLEAN NOT NULL DEFAULT FALSE,
                attempt_count INTEGER NOT NULL DEFAULT 1,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                delivered_at TIMESTAMP WITH TIME ZONE
            )";
        
        var indexes = new[]
        {
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_configs_admin_uuid ON webhook_event_configs(admin_uuid)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_configs_webhook_id ON webhook_event_configs(webhook_id)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_configs_event_type ON webhook_event_configs(event_type)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_configs_admin_active ON webhook_event_configs(admin_uuid, is_active)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_configs_webhook_active ON webhook_event_configs(webhook_id, is_active)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_triggers_event_config_id ON webhook_event_triggers(event_config_id)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_triggers_webhook_id ON webhook_event_triggers(webhook_id)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_triggers_campaign_id ON webhook_event_triggers(campaign_id)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_triggers_created_desc ON webhook_event_triggers(created_at DESC)",
            "CREATE INDEX IF NOT EXISTS idx_webhook_event_triggers_campaign_created ON webhook_event_triggers(campaign_id, created_at DESC)"
        };
        
        var foreignKeys = new[]
        {
            @"ALTER TABLE webhook_event_configs 
              ADD CONSTRAINT IF NOT EXISTS fk_webhook_event_configs_webhook_id 
              FOREIGN KEY (webhook_id) REFERENCES webhooks(id) ON DELETE CASCADE",
            @"ALTER TABLE webhook_event_triggers 
              ADD CONSTRAINT IF NOT EXISTS fk_webhook_event_triggers_event_config_id 
              FOREIGN KEY (event_config_id) REFERENCES webhook_event_configs(id) ON DELETE CASCADE",
            @"ALTER TABLE webhook_event_triggers 
              ADD CONSTRAINT IF NOT EXISTS fk_webhook_event_triggers_webhook_id 
              FOREIGN KEY (webhook_id) REFERENCES webhooks(id) ON DELETE CASCADE"
        };
        
        try
        {
            // Create tables
            await connection.ExecuteAsync(createWebhookEventConfigsTable);
            Console.WriteLine("✓ webhook_event_configs table created or already exists");
            
            await connection.ExecuteAsync(createWebhookEventTriggersTable);
            Console.WriteLine("✓ webhook_event_triggers table created or already exists");
            
            // Create indexes
            foreach (var indexSql in indexes)
            {
                try
                {
                    await connection.ExecuteAsync(indexSql);
                    Console.WriteLine("✓ Index created or already exists");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error creating index: {ex.Message}");
                }
            }
            
            // Create foreign keys
            foreach (var foreignKeySql in foreignKeys)
            {
                try
                {
                    await connection.ExecuteAsync(foreignKeySql);
                    Console.WriteLine("✓ Foreign key created or already exists");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error creating foreign key: {ex.Message}");
                }
            }
            
            Console.WriteLine("✓ Webhook event tables migration completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error applying webhook event tables migration: {ex.Message}");
            throw;
        }
    }
}