-- Migration 002: Add lead sync tracking and progress tables
-- Purpose: Enable safe resume capability and prevent partial data corruption
-- Safe to run: Yes (only adds columns, no data changes)

-- Add tracking columns to lead_conversations
ALTER TABLE lead_conversations
ADD COLUMN IF NOT EXISTS last_synced_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS sync_status VARCHAR(20) DEFAULT 'pending' CHECK (sync_status IN ('pending', 'in_progress', 'completed', 'failed'));

-- Create index for efficient querying of sync status
CREATE INDEX IF NOT EXISTS idx_lead_conversations_sync_status
ON lead_conversations(campaign_id, sync_status, last_synced_at);

-- Create sync progress tracking table
CREATE TABLE IF NOT EXISTS campaign_sync_progress (
    campaign_id INTEGER PRIMARY KEY,
    last_processed_lead_id VARCHAR(50),
    last_processed_lead_email VARCHAR(500),
    total_leads_in_campaign INTEGER DEFAULT 0,
    leads_processed INTEGER DEFAULT 0,
    sync_started_at TIMESTAMP WITH TIME ZONE,
    sync_completed_at TIMESTAMP WITH TIME ZONE,
    sync_status VARCHAR(20) DEFAULT 'not_started' CHECK (sync_status IN ('not_started', 'in_progress', 'completed', 'failed', 'partial')),
    error_message TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create index for efficient progress lookups
CREATE INDEX IF NOT EXISTS idx_campaign_sync_progress_status
ON campaign_sync_progress(sync_status, sync_started_at);

-- Add comment for documentation
COMMENT ON TABLE campaign_sync_progress IS 'Tracks campaign lead sync progress for resume capability';
COMMENT ON COLUMN lead_conversations.last_synced_at IS 'Timestamp of last successful sync for this lead';
COMMENT ON COLUMN lead_conversations.sync_status IS 'Current sync status: pending, in_progress, completed, failed';
