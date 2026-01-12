-- Initial schema for Smartlead API PostgreSQL migration
-- This script creates all the necessary tables to replace LiteDB collections

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

-- Create index on client email for faster lookups
CREATE INDEX IF NOT EXISTS idx_clients_email ON clients (email);
CREATE INDEX IF NOT EXISTS idx_clients_status ON clients (status);
CREATE INDEX IF NOT EXISTS idx_clients_created_at ON clients (created_at);

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
    
    -- Warmup statistics
    warmup_sent INTEGER NOT NULL DEFAULT 0,
    warmup_replied INTEGER NOT NULL DEFAULT 0,
    warmup_saved_from_spam INTEGER NOT NULL DEFAULT 0,
    
    -- Email statistics
    sent INTEGER NOT NULL DEFAULT 0,
    opened INTEGER NOT NULL DEFAULT 0,
    clicked INTEGER NOT NULL DEFAULT 0,
    replied INTEGER NOT NULL DEFAULT 0,
    unsubscribed INTEGER NOT NULL DEFAULT 0,
    bounced INTEGER NOT NULL DEFAULT 0,
    
    -- Tags stored as JSON array
    tags JSONB DEFAULT '[]',
    
    -- Campaign association count
    campaign_count INTEGER NOT NULL DEFAULT 0,
    
    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_updated_at TIMESTAMP WITH TIME ZONE,
    
    -- API fetch timestamps
    warmup_update_datetime TIMESTAMP NULL,
    email_stats_update_datetime TIMESTAMP NULL,
    
    -- Foreign key constraint
    CONSTRAINT fk_email_accounts_client FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE SET NULL
);

-- Create indexes on email_accounts for better performance
CREATE INDEX IF NOT EXISTS idx_email_accounts_email ON email_accounts (email);
CREATE INDEX IF NOT EXISTS idx_email_accounts_admin_uuid ON email_accounts (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_id ON email_accounts (client_id);
CREATE INDEX IF NOT EXISTS idx_email_accounts_status ON email_accounts (status);
CREATE INDEX IF NOT EXISTS idx_email_accounts_created_at ON email_accounts (created_at);
CREATE INDEX IF NOT EXISTS idx_email_accounts_warmup_update_datetime ON email_accounts (warmup_update_datetime);
CREATE INDEX IF NOT EXISTS idx_email_accounts_email_stats_update_datetime ON email_accounts (email_stats_update_datetime);

-- Campaigns table
CREATE TABLE IF NOT EXISTS campaigns (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    name VARCHAR(255),
    client_id VARCHAR(255),
    status VARCHAR(50) DEFAULT 'active',
    
    -- Campaign metrics
    total_positive_replies INTEGER,
    total_leads INTEGER DEFAULT 0,
    total_sent INTEGER DEFAULT 0,
    total_opened INTEGER DEFAULT 0,
    total_replied INTEGER DEFAULT 0,
    total_bounced INTEGER DEFAULT 0,
    total_clicked INTEGER DEFAULT 0,
    
    -- Email IDs stored as JSON array
    email_ids JSONB DEFAULT '[]',
    
    -- Tags stored as JSON array  
    tags JSONB DEFAULT '[]',
    
    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_updated_at TIMESTAMP WITH TIME ZONE,
    
    -- Foreign key constraint
    CONSTRAINT fk_campaigns_client FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE SET NULL
);

-- Create indexes on campaigns for better performance
CREATE INDEX IF NOT EXISTS idx_campaigns_admin_uuid ON campaigns (admin_uuid);
CREATE INDEX IF NOT EXISTS idx_campaigns_campaign_id ON campaigns (campaign_id);
CREATE INDEX IF NOT EXISTS idx_campaigns_client_id ON campaigns (client_id);
CREATE INDEX IF NOT EXISTS idx_campaigns_status ON campaigns (status);
CREATE INDEX IF NOT EXISTS idx_campaigns_created_at ON campaigns (created_at);

-- Users table (based on the User model referenced in the code)
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

-- Create indexes on users
CREATE INDEX IF NOT EXISTS idx_users_username ON users (username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);

-- Create a function to automatically update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers to automatically update updated_at columns
DROP TRIGGER IF EXISTS update_clients_updated_at ON clients;
CREATE TRIGGER update_clients_updated_at BEFORE UPDATE ON clients 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_email_accounts_updated_at ON email_accounts;
CREATE TRIGGER update_email_accounts_updated_at BEFORE UPDATE ON email_accounts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_campaigns_updated_at ON campaigns;
CREATE TRIGGER update_campaigns_updated_at BEFORE UPDATE ON campaigns 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_users_updated_at ON users;
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();