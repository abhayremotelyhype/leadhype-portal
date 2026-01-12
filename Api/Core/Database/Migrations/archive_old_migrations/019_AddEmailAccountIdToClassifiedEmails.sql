-- Migration: 019_AddEmailAccountIdToClassifiedEmails.sql
-- Description: Add email_account_id column to classified_emails table for frontend linking/mapping
-- Date: 2024-09-19

-- Add email_account_id column to classified_emails table
ALTER TABLE classified_emails 
ADD COLUMN IF NOT EXISTS email_account_id INTEGER;

-- Create index for efficient email account filtering
CREATE INDEX IF NOT EXISTS idx_classified_emails_email_account_id ON classified_emails(email_account_id);

-- Add composite index for email account and campaign queries
CREATE INDEX IF NOT EXISTS idx_classified_emails_email_account_campaign ON classified_emails(email_account_id, campaign_id);

-- Add comment for documentation
COMMENT ON COLUMN classified_emails.email_account_id IS 'Email account ID from Smartlead for linking/mapping with email accounts in frontend';