-- ============================================================================
-- Consolidated Database Schema
-- Created: 2025-09-30
-- Description: Single consolidated migration file combining all previous migrations (001-021)
-- ============================================================================

-- ============================================================================
-- FUNCTIONS
-- ============================================================================

-- Function to automatically update updated_at timestamps
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE 'plpgsql';

-- Function for classified_emails updated_at
CREATE OR REPLACE FUNCTION update_classified_emails_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- CORE TABLES
-- ============================================================================

-- Clients table
CREATE TABLE IF NOT EXISTS clients (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    company VARCHAR(255),
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    color VARCHAR(10) NOT NULL DEFAULT '#3B82F6',
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id VARCHAR(255) PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'User',
    first_name VARCHAR(255),
    last_name VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP WITH TIME ZONE,
    refresh_token TEXT,
    refresh_token_expiry_time TIMESTAMP WITH TIME ZONE,
    assigned_client_ids JSONB DEFAULT '[]',
    api_key VARCHAR(255),
    api_key_created_at TIMESTAMP WITH TIME ZONE
);

-- Settings table
CREATE TABLE IF NOT EXISTS settings (
    id VARCHAR(255) PRIMARY KEY,
    key VARCHAR(255) NOT NULL UNIQUE,
    value TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- User sessions table
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
    CONSTRAINT fk_user_sessions_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

-- Email accounts table
CREATE TABLE IF NOT EXISTS email_accounts (
    id BIGINT PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL,
    client_id VARCHAR(255),
    client_name VARCHAR(255),
    client_color VARCHAR(10),
    warmup_sent INTEGER NOT NULL DEFAULT 0,
    warmup_replied INTEGER NOT NULL DEFAULT 0,
    warmup_saved_from_spam INTEGER NOT NULL DEFAULT 0,
    sent INTEGER NOT NULL DEFAULT 0,
    opened INTEGER NOT NULL DEFAULT 0,
    replied INTEGER NOT NULL DEFAULT 0,
    bounced INTEGER NOT NULL DEFAULT 0,
    tags JSONB DEFAULT '[]',
    campaign_count INTEGER NOT NULL DEFAULT 0,
    active_campaign_count INTEGER DEFAULT 0,
    is_sending_actual_emails BOOLEAN DEFAULT NULL,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_updated_at TIMESTAMP WITH TIME ZONE,
    warmup_update_datetime TIMESTAMP NULL,
    email_stats_update_datetime TIMESTAMP NULL,
    CONSTRAINT fk_email_accounts_client FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE SET NULL
);

-- Campaigns table
CREATE TABLE IF NOT EXISTS campaigns (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    name VARCHAR(255),
    client_id VARCHAR(255),
    status VARCHAR(50) DEFAULT 'active',
    total_positive_replies INTEGER,
    total_leads INTEGER DEFAULT 0,
    total_sent INTEGER DEFAULT 0,
    total_opened INTEGER DEFAULT 0,
    total_replied INTEGER DEFAULT 0,
    total_bounced INTEGER DEFAULT 0,
    total_clicked INTEGER DEFAULT 0,
    email_ids JSONB DEFAULT '[]',
    tags JSONB DEFAULT '[]',
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_updated_at TIMESTAMP WITH TIME ZONE,
    CONSTRAINT fk_campaigns_client FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE SET NULL
);

-- Campaign events table for tracking granular campaign activities
CREATE TABLE IF NOT EXISTS campaign_events (
    id SERIAL PRIMARY KEY,
    campaign_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    event_date DATE NOT NULL,
    event_count INTEGER NOT NULL DEFAULT 1,
    email_account_id VARCHAR(255),
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- STATISTICS AND ANALYTICS TABLES
-- ============================================================================

-- Warmup metrics table
CREATE TABLE IF NOT EXISTS warmup_metrics (
    id BIGINT PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    total_sent INTEGER NOT NULL DEFAULT 0,
    total_replied INTEGER NOT NULL DEFAULT 0,
    total_saved_from_spam INTEGER NOT NULL DEFAULT 0,
    warmup_sent_dictionary JSONB DEFAULT '{}',
    warmup_replied_dictionary JSONB DEFAULT '{}',
    saved_from_spam_dictionary JSONB DEFAULT '{}',
    last_updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_warmup_metrics_email_account FOREIGN KEY (id) REFERENCES email_accounts (id) ON DELETE CASCADE
);

-- Campaign daily statistics
CREATE TABLE IF NOT EXISTS campaign_daily_stat_entries (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id VARCHAR(255) NOT NULL,
    campaign_id_int INTEGER NOT NULL,
    stat_date DATE NOT NULL,
    sent INTEGER NOT NULL DEFAULT 0,
    opened INTEGER NOT NULL DEFAULT 0,
    clicked INTEGER NOT NULL DEFAULT 0,
    replied INTEGER NOT NULL DEFAULT 0,
    positive_replies INTEGER NOT NULL DEFAULT 0,
    bounced INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_campaign_date UNIQUE (campaign_id, stat_date)
);

-- Email account daily statistics
CREATE TABLE IF NOT EXISTS email_account_daily_stat_entries (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    email_account_id BIGINT NOT NULL,
    stat_date DATE NOT NULL,
    sent INTEGER NOT NULL DEFAULT 0,
    opened INTEGER NOT NULL DEFAULT 0,
    replied INTEGER NOT NULL DEFAULT 0,
    bounced INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_email_account_date UNIQUE (email_account_id, stat_date),
    CONSTRAINT fk_email_account_stat_entries_account FOREIGN KEY (email_account_id) REFERENCES email_accounts (id) ON DELETE CASCADE
);

-- Campaign analytics
CREATE TABLE IF NOT EXISTS campaign_analytics (
    id VARCHAR(255) PRIMARY KEY,
    campaign_id INTEGER NOT NULL,
    campaign_name VARCHAR(255),
    analytics_data JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- API KEY MANAGEMENT TABLES
-- ============================================================================

-- API keys
CREATE TABLE IF NOT EXISTS api_keys (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    key_hash VARCHAR(255) NOT NULL UNIQUE,
    key_prefix VARCHAR(20) NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    permissions JSONB DEFAULT '[]'::jsonb,
    rate_limit INTEGER DEFAULT 1000,
    is_active BOOLEAN DEFAULT true,
    last_used_at TIMESTAMP,
    expires_at TIMESTAMP,
    ip_whitelist JSONB DEFAULT '[]'::jsonb,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- API key usage tracking
CREATE TABLE IF NOT EXISTS api_key_usage (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    api_key_id VARCHAR(255) NOT NULL,
    endpoint VARCHAR(500),
    method VARCHAR(10),
    status_code INTEGER,
    response_time_ms INTEGER,
    ip_address VARCHAR(45),
    user_agent TEXT,
    request_body_size INTEGER,
    response_body_size INTEGER,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (api_key_id) REFERENCES api_keys(id) ON DELETE CASCADE
);

-- Webhooks
CREATE TABLE IF NOT EXISTS webhooks (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    url TEXT NOT NULL,
    events JSONB NOT NULL DEFAULT '[]'::jsonb,
    headers JSONB DEFAULT '{}'::jsonb,
    secret VARCHAR(255),
    is_active BOOLEAN DEFAULT true,
    retry_count INTEGER DEFAULT 3,
    timeout_seconds INTEGER DEFAULT 30,
    last_triggered_at TIMESTAMP,
    failure_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Webhook deliveries
CREATE TABLE IF NOT EXISTS webhook_deliveries (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    webhook_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    status_code INTEGER,
    response_body TEXT,
    error_message TEXT,
    attempt_count INTEGER DEFAULT 1,
    delivered_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (webhook_id) REFERENCES webhooks(id) ON DELETE CASCADE
);

-- Rate limits
CREATE TABLE IF NOT EXISTS rate_limits (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    api_key_id VARCHAR(255),
    ip_address VARCHAR(45),
    endpoint VARCHAR(500),
    window_start TIMESTAMP NOT NULL,
    request_count INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(api_key_id, window_start)
);

-- ============================================================================
-- CAMPAIGN CONTENT TABLES
-- ============================================================================

-- Email templates
CREATE TABLE IF NOT EXISTS email_templates (
    id VARCHAR(36) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    subject TEXT NOT NULL DEFAULT '',
    body TEXT NOT NULL DEFAULT '',
    sequence_number INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_campaign_sequence UNIQUE (campaign_id, sequence_number)
);

-- Lead conversations
CREATE TABLE IF NOT EXISTS lead_conversations (
    id VARCHAR(36) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    lead_email VARCHAR(500) NOT NULL,
    lead_first_name VARCHAR(500) NOT NULL DEFAULT '',
    lead_last_name VARCHAR(500) NOT NULL DEFAULT '',
    status VARCHAR(50) NOT NULL DEFAULT '',
    conversation_data TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_campaign_lead UNIQUE (campaign_id, lead_email)
);

-- Lead email history
CREATE TABLE IF NOT EXISTS lead_email_history (
    id VARCHAR(36) PRIMARY KEY,
    admin_uuid VARCHAR(36) NOT NULL,
    campaign_id INTEGER NOT NULL,
    lead_id VARCHAR(50) NOT NULL,
    lead_email VARCHAR(500) NOT NULL,
    subject TEXT NOT NULL DEFAULT '',
    body TEXT NOT NULL DEFAULT '',
    sequence_number INTEGER NOT NULL DEFAULT 0,
    time TIMESTAMP WITH TIME ZONE,
    type VARCHAR(10) DEFAULT 'SENT',
    classification_result VARCHAR(255),
    classified_at TIMESTAMP WITH TIME ZONE,
    is_classified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_lead_email_history_type CHECK (type IN ('SENT', 'REPLY')),
    CONSTRAINT unique_lead_email_entry UNIQUE (campaign_id, lead_id, sequence_number, type)
);

-- Classified emails
CREATE TABLE IF NOT EXISTS classified_emails (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    message_id VARCHAR(255) NOT NULL,
    lead_email VARCHAR(500) NOT NULL,
    email_type VARCHAR(50) NOT NULL DEFAULT 'REPLY',
    email_time TIMESTAMP WITH TIME ZONE NOT NULL,
    email_body_hash VARCHAR(64) NOT NULL,
    classification_result VARCHAR(255) NOT NULL,
    classified_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    email_account_id INTEGER,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Clients indexes
CREATE INDEX IF NOT EXISTS idx_clients_email ON clients (email);
CREATE INDEX IF NOT EXISTS idx_clients_status ON clients (status);
CREATE INDEX IF NOT EXISTS idx_clients_created_at ON clients (created_at);

-- Users indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON users (username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);

-- Settings indexes
CREATE INDEX IF NOT EXISTS idx_settings_key ON settings (key);

-- User sessions indexes
CREATE INDEX IF NOT EXISTS idx_user_sessions_user_id ON user_sessions (user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_refresh_token ON user_sessions (refresh_token);
CREATE INDEX IF NOT EXISTS idx_user_sessions_expiry ON user_sessions (refresh_token_expiry_time);
CREATE INDEX IF NOT EXISTS idx_user_sessions_is_active ON user_sessions (is_active);

-- Email accounts indexes
CREATE INDEX IF NOT EXISTS idx_email_accounts_email ON email_accounts (email);
CREATE INDEX IF NOT EXISTS idx_email_accounts_name ON email_accounts (name);
CREATE INDEX IF NOT EXISTS idx_email_accounts_admin_uuid ON email_accounts (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_id ON email_accounts (client_id);
CREATE INDEX IF NOT EXISTS idx_email_accounts_status ON email_accounts (status);
CREATE INDEX IF NOT EXISTS idx_email_accounts_created_at ON email_accounts (created_at);
CREATE INDEX IF NOT EXISTS idx_email_accounts_updated_at ON email_accounts (updated_at);
CREATE INDEX IF NOT EXISTS idx_email_accounts_warmup_update_datetime ON email_accounts (warmup_update_datetime);
CREATE INDEX IF NOT EXISTS idx_email_accounts_email_stats_update_datetime ON email_accounts (email_stats_update_datetime);
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_email ON email_accounts (client_id, email);
CREATE INDEX IF NOT EXISTS idx_email_accounts_status_email ON email_accounts (status, email);
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_name ON email_accounts (client_name);
CREATE INDEX IF NOT EXISTS idx_email_accounts_tags ON email_accounts USING GIN(tags);
CREATE INDEX IF NOT EXISTS idx_email_accounts_active_campaign_count ON email_accounts (active_campaign_count);
CREATE INDEX IF NOT EXISTS idx_email_accounts_is_sending_actual_emails ON email_accounts (is_sending_actual_emails);

-- Campaigns indexes
CREATE INDEX IF NOT EXISTS idx_campaigns_admin_uuid ON campaigns (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_campaigns_campaign_id ON campaigns (campaign_id);
CREATE INDEX IF NOT EXISTS idx_campaigns_client_id ON campaigns (client_id);
CREATE INDEX IF NOT EXISTS idx_campaigns_status ON campaigns (status);
CREATE INDEX IF NOT EXISTS idx_campaigns_created_at ON campaigns (created_at);
CREATE INDEX IF NOT EXISTS idx_campaigns_email_ids ON campaigns USING GIN(email_ids);

-- Campaign events indexes
CREATE INDEX IF NOT EXISTS idx_campaign_events_campaign_id ON campaign_events(campaign_id);
CREATE INDEX IF NOT EXISTS idx_campaign_events_event_date ON campaign_events(event_date);
CREATE INDEX IF NOT EXISTS idx_campaign_events_event_type ON campaign_events(event_type);
CREATE UNIQUE INDEX IF NOT EXISTS idx_campaign_events_unique ON campaign_events(campaign_id, event_type, event_date);

-- Campaign events function
CREATE OR REPLACE FUNCTION add_campaign_event(
    p_campaign_id TEXT,
    p_event_type TEXT,
    p_count INTEGER DEFAULT 1,
    p_email_account_id TEXT DEFAULT NULL,
    p_metadata JSONB DEFAULT '{}'::jsonb
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO campaign_events (
        campaign_id,
        event_type,
        event_date,
        event_count,
        email_account_id,
        metadata,
        created_at
    )
    VALUES (
        p_campaign_id,
        p_event_type,
        CURRENT_DATE,
        p_count,
        p_email_account_id,
        p_metadata,
        NOW()
    )
    ON CONFLICT (campaign_id, event_type, event_date)
    DO UPDATE SET
        event_count = p_count,
        metadata = p_metadata;
END;
$$;

-- Warmup metrics indexes
CREATE INDEX IF NOT EXISTS idx_warmup_metrics_email ON warmup_metrics (email);
CREATE INDEX IF NOT EXISTS idx_warmup_metrics_last_updated ON warmup_metrics (last_updated_at);

-- Campaign daily stats indexes
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_admin_uuid ON campaign_daily_stat_entries (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_campaign_id ON campaign_daily_stat_entries (campaign_id);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_campaign_id_int ON campaign_daily_stat_entries (campaign_id_int);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_stat_date ON campaign_daily_stat_entries (stat_date);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_date_range ON campaign_daily_stat_entries (campaign_id, stat_date);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_campaign_date_composite ON campaign_daily_stat_entries (campaign_id, stat_date DESC);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_aggregation ON campaign_daily_stat_entries (admin_uuid, stat_date, campaign_id_int);

-- Email account daily stats indexes
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_admin_uuid ON email_account_daily_stat_entries (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_email_account_id ON email_account_daily_stat_entries (email_account_id);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_stat_date ON email_account_daily_stat_entries (stat_date);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_date_range ON email_account_daily_stat_entries (email_account_id, stat_date);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_account_date_composite ON email_account_daily_stat_entries (email_account_id, stat_date DESC);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_aggregation ON email_account_daily_stat_entries (admin_uuid, stat_date, email_account_id);

-- Campaign analytics indexes
CREATE INDEX IF NOT EXISTS idx_campaign_analytics_campaign_id ON campaign_analytics (campaign_id);

-- API keys indexes
CREATE INDEX IF NOT EXISTS idx_api_keys_user_id ON api_keys (user_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_key_prefix ON api_keys (key_prefix);
CREATE INDEX IF NOT EXISTS idx_api_keys_is_active ON api_keys (is_active);

-- API key usage indexes
CREATE INDEX IF NOT EXISTS idx_api_key_usage_api_key_id ON api_key_usage (api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_created_at ON api_key_usage (created_at);

-- Webhooks indexes
CREATE INDEX IF NOT EXISTS idx_webhooks_user_id ON webhooks (user_id);
CREATE INDEX IF NOT EXISTS idx_webhooks_is_active ON webhooks (is_active);

-- Webhook deliveries indexes
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_webhook_id ON webhook_deliveries (webhook_id);
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_created_at ON webhook_deliveries (created_at);

-- Rate limits indexes
CREATE INDEX IF NOT EXISTS idx_rate_limits_api_key_id ON rate_limits (api_key_id);
CREATE INDEX IF NOT EXISTS idx_rate_limits_window_start ON rate_limits (window_start);

-- Email templates indexes
CREATE INDEX IF NOT EXISTS idx_email_templates_campaign_id ON email_templates (campaign_id);
CREATE INDEX IF NOT EXISTS idx_email_templates_admin_uuid ON email_templates (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_templates_sequence ON email_templates (campaign_id, sequence_number);

-- Lead conversations indexes
CREATE INDEX IF NOT EXISTS idx_lead_conversations_campaign_id ON lead_conversations (campaign_id);
CREATE INDEX IF NOT EXISTS idx_lead_conversations_admin_uuid ON lead_conversations (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_lead_conversations_email ON lead_conversations (campaign_id, lead_email);
CREATE INDEX IF NOT EXISTS idx_lead_conversations_updated_at ON lead_conversations (updated_at DESC);

-- Lead email history indexes
CREATE INDEX IF NOT EXISTS idx_lead_email_history_campaign_id ON lead_email_history (campaign_id);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_lead_id ON lead_email_history (lead_id);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_campaign_lead ON lead_email_history (campaign_id, lead_id);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_sequence ON lead_email_history (sequence_number);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_classification ON lead_email_history (is_classified, classified_at);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_time ON lead_email_history (time);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_type ON lead_email_history (type);

-- Classified emails indexes
CREATE INDEX IF NOT EXISTS idx_classified_emails_message_id ON classified_emails (message_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_classified_emails_message_id_unique ON classified_emails (message_id);
CREATE INDEX IF NOT EXISTS idx_classified_emails_email_body_hash ON classified_emails (email_body_hash);
CREATE INDEX IF NOT EXISTS idx_classified_emails_campaign_id ON classified_emails (campaign_id);
CREATE INDEX IF NOT EXISTS idx_classified_emails_lead_email ON classified_emails (lead_email);
CREATE INDEX IF NOT EXISTS idx_classified_emails_classified_at ON classified_emails (classified_at);
CREATE INDEX IF NOT EXISTS idx_classified_emails_admin_uuid ON classified_emails (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_classified_emails_campaign_lead ON classified_emails (campaign_id, lead_email);
CREATE INDEX IF NOT EXISTS idx_classified_emails_email_account_id ON classified_emails (email_account_id);
CREATE INDEX IF NOT EXISTS idx_classified_emails_email_account_campaign ON classified_emails (email_account_id, campaign_id);

-- ============================================================================
-- TRIGGERS
-- ============================================================================

-- Clients trigger
CREATE OR REPLACE TRIGGER update_clients_updated_at
    BEFORE UPDATE ON clients
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Email accounts trigger
CREATE OR REPLACE TRIGGER update_email_accounts_updated_at
    BEFORE UPDATE ON email_accounts
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Campaigns trigger
CREATE OR REPLACE TRIGGER update_campaigns_updated_at
    BEFORE UPDATE ON campaigns
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Users trigger
CREATE OR REPLACE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Settings trigger
CREATE OR REPLACE TRIGGER update_settings_updated_at
    BEFORE UPDATE ON settings
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Note: user_sessions does NOT have an updated_at trigger (removed in migration 011)

-- Warmup metrics trigger
CREATE OR REPLACE TRIGGER update_warmup_metrics_updated_at
    BEFORE UPDATE ON warmup_metrics
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Campaign analytics trigger
CREATE OR REPLACE TRIGGER update_campaign_analytics_updated_at
    BEFORE UPDATE ON campaign_analytics
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- API keys trigger
CREATE OR REPLACE TRIGGER update_api_keys_updated_at
    BEFORE UPDATE ON api_keys
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Webhooks trigger
CREATE OR REPLACE TRIGGER update_webhooks_updated_at
    BEFORE UPDATE ON webhooks
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Rate limits trigger
CREATE OR REPLACE TRIGGER update_rate_limits_updated_at
    BEFORE UPDATE ON rate_limits
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Email templates trigger
CREATE OR REPLACE TRIGGER trigger_email_templates_updated_at
    BEFORE UPDATE ON email_templates
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Lead conversations trigger
CREATE OR REPLACE TRIGGER trigger_lead_conversations_updated_at
    BEFORE UPDATE ON lead_conversations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Classified emails trigger
CREATE OR REPLACE TRIGGER trigger_classified_emails_updated_at
    BEFORE UPDATE ON classified_emails
    FOR EACH ROW
    EXECUTE FUNCTION update_classified_emails_updated_at();
