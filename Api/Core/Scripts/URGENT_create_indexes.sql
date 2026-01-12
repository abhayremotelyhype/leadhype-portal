-- URGENT: Performance optimization for campaign_daily_stat_entries table
-- This script creates essential indexes to handle 600k+ records efficiently
-- Run this immediately to fix the slow "Loading campaign trends..." issue

BEGIN;

-- 1. Most critical: Composite index for campaign_id + stat_date queries (covers dashboard trends)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_campaign_daily_stats_campaign_date 
ON campaign_daily_stat_entries(campaign_id, stat_date DESC);

-- 2. Critical: Date-only index for time range aggregation queries (covers dashboard overview)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_campaign_daily_stats_date 
ON campaign_daily_stat_entries(stat_date DESC);

-- 3. Important: Admin UUID index for filtering
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_campaign_daily_stats_admin 
ON campaign_daily_stat_entries(admin_uuid);

-- 4. Performance: Covering index to avoid table lookups for dashboard queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_campaign_daily_stats_covering 
ON campaign_daily_stat_entries(stat_date DESC) 
INCLUDE (sent, opened, clicked, replied, positive_replies, bounced);

-- Update table statistics for query planner
ANALYZE campaign_daily_stat_entries;

COMMIT;

-- Performance verification queries
-- These should now run much faster:

-- Test 1: Dashboard campaign performance trend query
EXPLAIN (ANALYZE, BUFFERS) 
SELECT 
    stat_date as Date,
    SUM(sent) as TotalSent,
    SUM(opened) as TotalOpened,
    SUM(clicked) as TotalClicked,
    SUM(replied) as TotalReplied,
    SUM(positive_replies) as TotalPositiveReplies,
    SUM(bounced) as TotalBounced
FROM campaign_daily_stat_entries 
WHERE stat_date >= CURRENT_DATE - INTERVAL '30 days' 
  AND stat_date <= CURRENT_DATE 
GROUP BY stat_date
ORDER BY stat_date ASC;

-- Test 2: Campaign-specific trend query
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM campaign_daily_stat_entries 
WHERE campaign_id = ANY(ARRAY['campaign-id-1', 'campaign-id-2']) 
  AND stat_date >= CURRENT_DATE - INTERVAL '30 days' 
  AND stat_date <= CURRENT_DATE 
ORDER BY campaign_id, stat_date DESC;

-- Show index information
SELECT 
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE tablename = 'campaign_daily_stat_entries'
ORDER BY indexname;

-- Show table size information
SELECT 
    pg_size_pretty(pg_total_relation_size('campaign_daily_stat_entries')) AS table_size,
    pg_size_pretty(pg_relation_size('campaign_daily_stat_entries')) AS data_size,
    (SELECT COUNT(*) FROM campaign_daily_stat_entries) as row_count;

SELECT 'Index creation completed! Campaign trends should now load much faster.' as status;