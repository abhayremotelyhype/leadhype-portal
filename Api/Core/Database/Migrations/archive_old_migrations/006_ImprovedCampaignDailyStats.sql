-- Improved relational design for campaign daily statistics
-- Replace JSONB dictionary approach with proper relational model

-- Create new improved campaign daily stats table
CREATE TABLE IF NOT EXISTS campaign_daily_stat_entries (
    id VARCHAR(255) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id VARCHAR(255) NOT NULL,
    campaign_id_int INTEGER NOT NULL,
    stat_date DATE NOT NULL,
    
    -- Daily statistics (individual columns instead of JSONB)
    sent INTEGER NOT NULL DEFAULT 0,
    opened INTEGER NOT NULL DEFAULT 0,
    clicked INTEGER NOT NULL DEFAULT 0,
    replied INTEGER NOT NULL DEFAULT 0,
    positive_replies INTEGER NOT NULL DEFAULT 0,
    bounced INTEGER NOT NULL DEFAULT 0,
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT unique_campaign_date UNIQUE (campaign_id, stat_date)
);

-- Create indexes for optimal performance
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_admin_uuid ON campaign_daily_stat_entries(admin_uuid);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_campaign_id ON campaign_daily_stat_entries(campaign_id);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_campaign_id_int ON campaign_daily_stat_entries(campaign_id_int);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_stat_date ON campaign_daily_stat_entries(stat_date);
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_date_range ON campaign_daily_stat_entries(campaign_id, stat_date);

-- Composite index for common queries (campaign + date range)
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_campaign_date_composite ON campaign_daily_stat_entries(campaign_id, stat_date DESC);

-- Index for aggregation queries
CREATE INDEX IF NOT EXISTS idx_campaign_stat_entries_aggregation ON campaign_daily_stat_entries(admin_uuid, stat_date, campaign_id_int);

-- Add comment to table
COMMENT ON TABLE campaign_daily_stat_entries IS 'Improved relational model for campaign daily statistics - one row per campaign per date';