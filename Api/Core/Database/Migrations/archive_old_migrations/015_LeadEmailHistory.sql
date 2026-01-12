-- Create lead_email_history table
CREATE TABLE IF NOT EXISTS lead_email_history (
    id VARCHAR(36) PRIMARY KEY,
    admin_uuid VARCHAR(36) NOT NULL,
    campaign_id INTEGER NOT NULL,
    lead_id VARCHAR(50) NOT NULL,
    lead_email VARCHAR(255) NOT NULL,
    subject TEXT NOT NULL DEFAULT '',
    body TEXT NOT NULL DEFAULT '',
    sequence_number INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_lead_email_history_campaign_id ON lead_email_history(campaign_id);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_lead_id ON lead_email_history(lead_id);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_campaign_lead ON lead_email_history(campaign_id, lead_id);
CREATE INDEX IF NOT EXISTS idx_lead_email_history_sequence ON lead_email_history(sequence_number);

-- Add comments
COMMENT ON TABLE lead_email_history IS 'Stores email history for leads in campaigns';
COMMENT ON COLUMN lead_email_history.id IS 'Unique identifier for the email history record';
COMMENT ON COLUMN lead_email_history.admin_uuid IS 'UUID of the admin/user who owns this data';
COMMENT ON COLUMN lead_email_history.campaign_id IS 'ID of the campaign this email belongs to';
COMMENT ON COLUMN lead_email_history.lead_id IS 'ID of the lead from Smartlead API';
COMMENT ON COLUMN lead_email_history.lead_email IS 'Email address of the lead';
COMMENT ON COLUMN lead_email_history.subject IS 'Subject line of the email';
COMMENT ON COLUMN lead_email_history.body IS 'Body content of the email';
COMMENT ON COLUMN lead_email_history.sequence_number IS 'Sequence number of the email in the campaign';