-- Migration: 018_ClassifiedEmailsTable.sql
-- Description: Create classified_emails table for tracking email classifications to avoid duplicate RevReply API calls
-- Date: 2024-09-18

-- Create classified_emails table
CREATE TABLE IF NOT EXISTS classified_emails (
    id VARCHAR(255) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    message_id VARCHAR(255) NOT NULL,
    lead_email VARCHAR(500) NOT NULL,
    email_type VARCHAR(50) NOT NULL DEFAULT 'REPLY',
    email_time TIMESTAMP WITH TIME ZONE NOT NULL,
    email_body_hash VARCHAR(64) NOT NULL, -- SHA256 hash
    classification_result VARCHAR(255) NOT NULL,
    classified_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for efficient lookups
CREATE INDEX IF NOT EXISTS idx_classified_emails_message_id ON classified_emails(message_id);
CREATE INDEX IF NOT EXISTS idx_classified_emails_email_body_hash ON classified_emails(email_body_hash);
CREATE INDEX IF NOT EXISTS idx_classified_emails_campaign_id ON classified_emails(campaign_id);
CREATE INDEX IF NOT EXISTS idx_classified_emails_lead_email ON classified_emails(lead_email);
CREATE INDEX IF NOT EXISTS idx_classified_emails_classified_at ON classified_emails(classified_at);
CREATE INDEX IF NOT EXISTS idx_classified_emails_admin_uuid ON classified_emails(admin_uuid);

-- Add unique constraint to prevent duplicate message IDs
CREATE UNIQUE INDEX IF NOT EXISTS idx_classified_emails_message_id_unique ON classified_emails(message_id);

-- Add composite index for common queries
CREATE INDEX IF NOT EXISTS idx_classified_emails_campaign_lead ON classified_emails(campaign_id, lead_email);

-- Add updated_at trigger
CREATE OR REPLACE FUNCTION update_classified_emails_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_classified_emails_updated_at ON classified_emails;
CREATE TRIGGER trigger_classified_emails_updated_at
    BEFORE UPDATE ON classified_emails
    FOR EACH ROW
    EXECUTE FUNCTION update_classified_emails_updated_at();

-- Add comments for documentation
COMMENT ON TABLE classified_emails IS 'Tracks emails that have been classified to avoid duplicate RevReply API calls and save credits';
COMMENT ON COLUMN classified_emails.message_id IS 'Unique message ID from Smartlead API for primary deduplication';
COMMENT ON COLUMN classified_emails.email_body_hash IS 'SHA256 hash of email body content for secondary deduplication';
COMMENT ON COLUMN classified_emails.classification_result IS 'Result from RevReply classification API';
COMMENT ON COLUMN classified_emails.email_type IS 'Type of email: SENT, REPLY, etc.';