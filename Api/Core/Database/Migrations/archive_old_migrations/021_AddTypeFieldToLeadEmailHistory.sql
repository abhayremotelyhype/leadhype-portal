-- Add type field to lead_email_history table to distinguish between SENT and REPLY emails
ALTER TABLE lead_email_history ADD COLUMN IF NOT EXISTS type VARCHAR(10) DEFAULT 'SENT';

-- Add comment for the new field
COMMENT ON COLUMN lead_email_history.type IS 'Type of email: SENT (outbound campaign email) or REPLY (inbound response)';

-- Add check constraint to ensure only valid values
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_lead_email_history_type') THEN
        ALTER TABLE lead_email_history ADD CONSTRAINT chk_lead_email_history_type 
        CHECK (type IN ('SENT', 'REPLY'));
    END IF;
END $$;

-- Create index for better query performance on type field
CREATE INDEX IF NOT EXISTS idx_lead_email_history_type ON lead_email_history(type);

-- Update existing records to have SENT type (default for existing data)
UPDATE lead_email_history SET type = 'SENT' WHERE type IS NULL;