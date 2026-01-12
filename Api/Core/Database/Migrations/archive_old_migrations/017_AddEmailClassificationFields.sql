-- Add RevReply classification fields to lead_email_history table
ALTER TABLE lead_email_history 
ADD COLUMN IF NOT EXISTS classification_result VARCHAR(255),
ADD COLUMN IF NOT EXISTS classified_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS is_classified BOOLEAN DEFAULT FALSE;

-- Create index for classification queries
CREATE INDEX IF NOT EXISTS idx_lead_email_history_classification ON lead_email_history(is_classified, classified_at);

-- Add comments for the new fields
COMMENT ON COLUMN lead_email_history.classification_result IS 'RevReply API classification result for this email';
COMMENT ON COLUMN lead_email_history.classified_at IS 'Timestamp when the email was classified by RevReply API';
COMMENT ON COLUMN lead_email_history.is_classified IS 'Flag indicating if this email has been analyzed by RevReply API';