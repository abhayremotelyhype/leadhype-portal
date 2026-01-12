-- Add time field to lead_email_history table
ALTER TABLE lead_email_history ADD COLUMN IF NOT EXISTS time TIMESTAMP WITH TIME ZONE;

-- Add comment for the new field
COMMENT ON COLUMN lead_email_history.time IS 'Timestamp when the email was sent/received from Smartlead API';

-- Create index for better query performance on time field
CREATE INDEX IF NOT EXISTS idx_lead_email_history_time ON lead_email_history(time);