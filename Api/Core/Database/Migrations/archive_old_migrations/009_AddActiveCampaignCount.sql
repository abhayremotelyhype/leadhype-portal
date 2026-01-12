-- Add active_campaign_count column to email_accounts table
ALTER TABLE email_accounts ADD COLUMN IF NOT EXISTS active_campaign_count INTEGER DEFAULT 0;

-- Add is_sending_actual_emails column to email_accounts table (nullable boolean for 3 states)
ALTER TABLE email_accounts ADD COLUMN IF NOT EXISTS is_sending_actual_emails BOOLEAN DEFAULT NULL;

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_email_accounts_active_campaign_count ON email_accounts(active_campaign_count);
CREATE INDEX IF NOT EXISTS idx_email_accounts_is_sending_actual_emails ON email_accounts(is_sending_actual_emails);

-- Update existing records to set active_campaign_count
UPDATE email_accounts 
SET active_campaign_count = COALESCE((
    SELECT COUNT(*)
    FROM campaigns c
    WHERE jsonb_array_length(c.email_ids) > 0 
    AND c.email_ids ? email_accounts.id::text
    AND c.status = 'ACTIVE'
), 0);

-- Update existing records to set is_sending_actual_emails (3-state logic)
UPDATE email_accounts 
SET is_sending_actual_emails = CASE 
    WHEN sent > 0 THEN true
    WHEN sent = 0 AND warmup_sent > 0 THEN false
    ELSE NULL
END;