-- Remove clicked and unsubscribed columns from email_account_daily_stat_entries table
-- These metrics are not actively used and were requested to be removed

-- Drop the clicked and unsubscribed columns
ALTER TABLE email_account_daily_stat_entries 
DROP COLUMN IF EXISTS clicked,
DROP COLUMN IF EXISTS unsubscribed;

-- Add comment to document the change
COMMENT ON TABLE email_account_daily_stat_entries IS 'Email account daily statistics - removed clicked and unsubscribed columns as they are not actively used';