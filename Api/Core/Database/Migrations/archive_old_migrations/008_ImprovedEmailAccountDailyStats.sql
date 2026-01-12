-- Improved relational design for email account daily statistics
-- Replace JSONB dictionary approach with proper relational model

-- Create new improved email account daily stats table
CREATE TABLE IF NOT EXISTS email_account_daily_stat_entries (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    email_account_id BIGINT NOT NULL,
    stat_date DATE NOT NULL,
    
    -- Daily statistics (individual columns instead of JSONB)
    sent INTEGER NOT NULL DEFAULT 0,
    opened INTEGER NOT NULL DEFAULT 0,
    clicked INTEGER NOT NULL DEFAULT 0,
    replied INTEGER NOT NULL DEFAULT 0,
    unsubscribed INTEGER NOT NULL DEFAULT 0,
    bounced INTEGER NOT NULL DEFAULT 0,
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT unique_email_account_date UNIQUE (email_account_id, stat_date),
    CONSTRAINT fk_email_account_stat_entries_account FOREIGN KEY (email_account_id) REFERENCES email_accounts (id) ON DELETE CASCADE
);

-- Create indexes for optimal performance
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_admin_uuid ON email_account_daily_stat_entries(admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_email_account_id ON email_account_daily_stat_entries(email_account_id);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_stat_date ON email_account_daily_stat_entries(stat_date);
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_date_range ON email_account_daily_stat_entries(email_account_id, stat_date);

-- Composite index for common queries (account + date range)
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_account_date_composite ON email_account_daily_stat_entries(email_account_id, stat_date DESC);

-- Index for aggregation queries
CREATE INDEX IF NOT EXISTS idx_email_account_stat_entries_aggregation ON email_account_daily_stat_entries(admin_uuid, stat_date, email_account_id);

-- Add comment to table
COMMENT ON TABLE email_account_daily_stat_entries IS 'Improved relational model for email account daily statistics - one row per email account per date';