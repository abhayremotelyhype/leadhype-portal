-- Additional tables for complete LiteDB to PostgreSQL migration

-- Settings table for application configuration
CREATE TABLE IF NOT EXISTS settings (
    id VARCHAR(255) PRIMARY KEY,
    key VARCHAR(255) NOT NULL UNIQUE,
    value TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create index on settings key for faster lookups
CREATE INDEX IF NOT EXISTS idx_settings_key ON settings (key);

-- User sessions table for authentication tokens
CREATE TABLE IF NOT EXISTS user_sessions (
    id VARCHAR(255) PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    refresh_token TEXT NOT NULL,
    refresh_token_expiry_time TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_accessed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    device_name VARCHAR(255),
    ip_address VARCHAR(100),
    user_agent TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Foreign key constraint
    CONSTRAINT fk_user_sessions_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

-- Create indexes on user_sessions for better performance
CREATE INDEX IF NOT EXISTS idx_user_sessions_user_id ON user_sessions (user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_refresh_token ON user_sessions (refresh_token);
CREATE INDEX IF NOT EXISTS idx_user_sessions_expiry ON user_sessions (refresh_token_expiry_time);
CREATE INDEX IF NOT EXISTS idx_user_sessions_is_active ON user_sessions (is_active);

-- OLD: Campaign and Email Account daily statistics tables using JSONB
-- These have been replaced by relational tables:
--   - campaign_daily_stat_entries (see 006_ImprovedCampaignDailyStats.sql)
--   - email_account_daily_stat_entries (see 008_ImprovedEmailAccountDailyStats.sql)
-- The old tables are kept commented here for reference but are no longer used

-- -- Campaign daily statistics table (OLD - REPLACED)
-- CREATE TABLE IF NOT EXISTS campaign_daily_stats (
--     id VARCHAR(255) PRIMARY KEY,
--     admin_uuid VARCHAR(255) NOT NULL,
--     campaign_id VARCHAR(255) NOT NULL,
--     campaign_id_int INTEGER NOT NULL,
--     
--     -- Daily stats stored as JSONB (date -> count mappings)
--     sent JSONB DEFAULT '{}',
--     opened JSONB DEFAULT '{}',
--     clicked JSONB DEFAULT '{}',
--     replied JSONB DEFAULT '{}',
--     positive_replies JSONB DEFAULT '{}',
--     bounced JSONB DEFAULT '{}',
--     
--     created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
--     updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
--     
--     -- Foreign key constraint
--     CONSTRAINT fk_campaign_daily_stats_campaign FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE CASCADE
-- );

-- -- Create indexes on campaign_daily_stats
-- CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_admin_uuid ON campaign_daily_stats (admin_uuid);
-- CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_campaign_id ON campaign_daily_stats (campaign_id);
-- CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_campaign_id_int ON campaign_daily_stats (campaign_id_int);

-- -- Email account daily statistics table (OLD - REPLACED)
-- CREATE TABLE IF NOT EXISTS email_account_daily_stats (
--     id VARCHAR(255) PRIMARY KEY,
--     email_account_id BIGINT NOT NULL,
--     admin_uuid VARCHAR(255) NOT NULL,
--     
--     -- Daily stats stored as JSONB (date -> count mappings)
--     sent_emails JSONB DEFAULT '{}',
--     opened_emails JSONB DEFAULT '{}',
--     clicked_emails JSONB DEFAULT '{}',
--     replied_emails JSONB DEFAULT '{}',
--     unsubscribed_emails JSONB DEFAULT '{}',
--     bounced_emails JSONB DEFAULT '{}',
--     
--     created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
--     updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
--     
--     -- Foreign key constraint
--     CONSTRAINT fk_email_account_daily_stats_account FOREIGN KEY (email_account_id) REFERENCES email_accounts (id) ON DELETE CASCADE
-- );

-- -- Create indexes on email_account_daily_stats
-- CREATE INDEX IF NOT EXISTS idx_email_account_daily_stats_email_account_id ON email_account_daily_stats (email_account_id);
-- CREATE INDEX IF NOT EXISTS idx_email_account_daily_stats_admin_uuid ON email_account_daily_stats (admin_uuid);

-- Add triggers for the new tables to automatically update updated_at
DROP TRIGGER IF EXISTS update_settings_updated_at ON settings;
CREATE TRIGGER update_settings_updated_at BEFORE UPDATE ON settings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_user_sessions_updated_at ON user_sessions;
CREATE TRIGGER update_user_sessions_updated_at BEFORE UPDATE ON user_sessions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- OLD: Triggers for removed tables (commented out)
-- DROP TRIGGER IF EXISTS update_campaign_daily_stats_updated_at ON campaign_daily_stats;
-- CREATE TRIGGER update_campaign_daily_stats_updated_at BEFORE UPDATE ON campaign_daily_stats 
--     FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- DROP TRIGGER IF EXISTS update_email_account_daily_stats_updated_at ON email_account_daily_stats;
-- CREATE TRIGGER update_email_account_daily_stats_updated_at BEFORE UPDATE ON email_account_daily_stats 
--     FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();