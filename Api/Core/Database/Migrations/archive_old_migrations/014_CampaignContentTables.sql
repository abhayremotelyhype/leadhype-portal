-- 014_CampaignContentTables.sql
-- Creates tables for caching email templates and lead conversations

-- Email Templates table
CREATE TABLE IF NOT EXISTS email_templates (
    id VARCHAR(36) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    subject TEXT NOT NULL DEFAULT '',
    body TEXT NOT NULL DEFAULT '',
    sequence_number INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Lead Conversations table  
CREATE TABLE IF NOT EXISTS lead_conversations (
    id VARCHAR(36) PRIMARY KEY,
    admin_uuid VARCHAR(255) NOT NULL,
    campaign_id INTEGER NOT NULL,
    lead_email VARCHAR(255) NOT NULL,
    lead_first_name VARCHAR(255) NOT NULL DEFAULT '',
    lead_last_name VARCHAR(255) NOT NULL DEFAULT '',
    status VARCHAR(50) NOT NULL DEFAULT '',
    conversation_data TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_email_templates_campaign_id ON email_templates(campaign_id);
CREATE INDEX IF NOT EXISTS idx_email_templates_admin_uuid ON email_templates(admin_uuid);
CREATE INDEX IF NOT EXISTS idx_email_templates_sequence ON email_templates(campaign_id, sequence_number);

CREATE INDEX IF NOT EXISTS idx_lead_conversations_campaign_id ON lead_conversations(campaign_id);
CREATE INDEX IF NOT EXISTS idx_lead_conversations_admin_uuid ON lead_conversations(admin_uuid);
CREATE INDEX IF NOT EXISTS idx_lead_conversations_email ON lead_conversations(campaign_id, lead_email);
CREATE INDEX IF NOT EXISTS idx_lead_conversations_updated_at ON lead_conversations(updated_at DESC);

-- Update triggers to automatically update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_email_templates_updated_at
    BEFORE UPDATE ON email_templates
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_lead_conversations_updated_at
    BEFORE UPDATE ON lead_conversations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();