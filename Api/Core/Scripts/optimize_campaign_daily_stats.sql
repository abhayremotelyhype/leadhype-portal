-- Optimize campaign_daily_stat_entries table for performance
-- This script adds necessary indexes for fast querying of campaign daily statistics

-- 1. Composite index for the most common query pattern: by campaign_id and date range
CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_campaign_date 
ON campaign_daily_stat_entries(campaign_id, stat_date DESC);

-- 2. Index on stat_date for time-range queries across all campaigns
CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_date 
ON campaign_daily_stat_entries(stat_date DESC);

-- 3. Index on admin_uuid for filtering by admin
CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_admin 
ON campaign_daily_stat_entries(admin_uuid);

-- 4. Composite index for admin_uuid with date for admin-specific time ranges
CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_admin_date 
ON campaign_daily_stat_entries(admin_uuid, stat_date DESC);

-- 5. Covering index for the GetByCampaignIdsAndDateRangeAsync query
-- This includes all columns needed to avoid table lookups
CREATE INDEX IF NOT EXISTS idx_campaign_daily_stats_covering 
ON campaign_daily_stat_entries(campaign_id, stat_date DESC) 
INCLUDE (sent, opened, clicked, replied, positive_replies, bounced);

-- Analyze the table to update statistics for query planner
ANALYZE campaign_daily_stat_entries;

-- Show table statistics
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    n_live_tup AS row_estimate
FROM pg_stat_user_tables 
WHERE tablename = 'campaign_daily_stat_entries';

-- Show all indexes on the table
SELECT 
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE tablename = 'campaign_daily_stat_entries'
ORDER BY indexname;