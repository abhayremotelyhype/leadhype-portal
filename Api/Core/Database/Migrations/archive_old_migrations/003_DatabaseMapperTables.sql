-- Additional tables for DatabaseMapperService migration

-- Last fetched dates tracking table
CREATE TABLE IF NOT EXISTS last_fetched_dates (
    id VARCHAR(255) PRIMARY KEY,
    api_key VARCHAR(255) NOT NULL UNIQUE,
    email_accounts TIMESTAMP WITH TIME ZONE NOT NULL,
    email_stats TIMESTAMP WITH TIME ZONE NOT NULL,
    warmup_stats TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create index on api_key for faster lookups
CREATE INDEX IF NOT EXISTS idx_last_fetched_dates_api_key ON last_fetched_dates (api_key);

-- Email account stats date tracking table
CREATE TABLE IF NOT EXISTS email_account_stats_dates (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    stats_date DATE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Unique constraint to prevent duplicate entries
    CONSTRAINT uq_email_stats_date UNIQUE (admin_uuid, stats_date)
);

-- Create indexes for email_account_stats_dates
CREATE INDEX IF NOT EXISTS idx_email_account_stats_dates_admin_uuid ON email_account_stats_dates (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_account_stats_dates_stats_date ON email_account_stats_dates (stats_date);

-- Warmup metrics table
CREATE TABLE IF NOT EXISTS warmup_metrics (
    id BIGINT PRIMARY KEY, -- This is the email account ID
    email VARCHAR(255) NOT NULL,
    total_sent INTEGER NOT NULL DEFAULT 0,
    total_replied INTEGER NOT NULL DEFAULT 0,
    total_saved_from_spam INTEGER NOT NULL DEFAULT 0,
    
    -- Daily dictionaries stored as JSONB
    warmup_sent_dictionary JSONB DEFAULT '{}',
    warmup_replied_dictionary JSONB DEFAULT '{}',
    saved_from_spam_dictionary JSONB DEFAULT '{}',
    
    last_updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Foreign key to email_accounts
    CONSTRAINT fk_warmup_metrics_email_account FOREIGN KEY (id) REFERENCES email_accounts (id) ON DELETE CASCADE
);

-- Create indexes for warmup_metrics
CREATE INDEX IF NOT EXISTS idx_warmup_metrics_email ON warmup_metrics (email);
CREATE INDEX IF NOT EXISTS idx_warmup_metrics_last_updated ON warmup_metrics (last_updated_at);

-- Campaign analytics table (if needed)
CREATE TABLE IF NOT EXISTS campaign_analytics (
    id VARCHAR(255) PRIMARY KEY,
    campaign_id INTEGER NOT NULL,
    campaign_name VARCHAR(255),
    
    -- Analytics data stored as JSONB for flexibility
    analytics_data JSONB DEFAULT '{}',
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create index for campaign_analytics
CREATE INDEX IF NOT EXISTS idx_campaign_analytics_campaign_id ON campaign_analytics (campaign_id);

-- Add triggers for the new tables
DROP TRIGGER IF EXISTS update_last_fetched_dates_updated_at ON last_fetched_dates;
CREATE TRIGGER update_last_fetched_dates_updated_at BEFORE UPDATE ON last_fetched_dates 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_email_account_stats_dates_updated_at ON email_account_stats_dates;
CREATE TRIGGER update_email_account_stats_dates_updated_at BEFORE UPDATE ON email_account_stats_dates 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_warmup_metrics_updated_at ON warmup_metrics;
CREATE TRIGGER update_warmup_metrics_updated_at BEFORE UPDATE ON warmup_metrics 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_campaign_analytics_updated_at ON campaign_analytics;
CREATE TRIGGER update_campaign_analytics_updated_at BEFORE UPDATE ON campaign_analytics 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();