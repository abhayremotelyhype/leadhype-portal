-- Remove redundant last_fetched_dates table
-- We now use timestamps directly in the models (email_accounts.updated_at, etc.)

-- Drop the last_fetched_dates table since it's redundant
DROP TABLE IF EXISTS last_fetched_dates CASCADE;

-- Also drop the email_account_stats_dates table as it's redundant with our daily stats
DROP TABLE IF EXISTS email_account_stats_dates CASCADE;

-- Note: warmup_metrics and campaign_analytics tables are kept as they provide value
-- warmup_metrics: Stores warmup-specific data with JSONB dictionaries
-- campaign_analytics: Stores flexible analytics data

-- Add comment
COMMENT ON DATABASE smartlead IS 'Removed redundant tracking tables - using model timestamps directly';