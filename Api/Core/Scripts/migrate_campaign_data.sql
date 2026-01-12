-- MIGRATION SCRIPT: Move data from campaign_daily_stat_entries to campaign_events
-- This script will migrate only meaningful data (where any stat > 0)
-- Expected to reduce 2.6M rows to ~172K meaningful events (93% reduction)

-- First, let's check what we're working with
DO $$
DECLARE
    total_rows INTEGER;
    meaningful_rows INTEGER;
    zero_activity_rows INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_rows FROM campaign_daily_stat_entries;
    
    SELECT COUNT(*) INTO meaningful_rows 
    FROM campaign_daily_stat_entries 
    WHERE sent > 0 OR opened > 0 OR replied > 0 OR positive_replies > 0 OR bounced > 0 OR clicked > 0;
    
    zero_activity_rows := total_rows - meaningful_rows;
    
    RAISE NOTICE '=== MIGRATION ANALYSIS ===';
    RAISE NOTICE 'Total rows in old table: %', total_rows;
    RAISE NOTICE 'Rows with meaningful data: %', meaningful_rows;
    RAISE NOTICE 'Zero activity rows to skip: % (%.2f%%)', zero_activity_rows, (zero_activity_rows::float / total_rows * 100);
    RAISE NOTICE '========================';
END $$;

-- Create additional partitions for historical data
SELECT create_campaign_events_partition('2023-01-01'::date);
SELECT create_campaign_events_partition('2023-02-01'::date);
SELECT create_campaign_events_partition('2023-03-01'::date);
SELECT create_campaign_events_partition('2023-04-01'::date);
SELECT create_campaign_events_partition('2023-05-01'::date);
SELECT create_campaign_events_partition('2023-06-01'::date);
SELECT create_campaign_events_partition('2023-07-01'::date);
SELECT create_campaign_events_partition('2023-08-01'::date);
SELECT create_campaign_events_partition('2023-09-01'::date);
SELECT create_campaign_events_partition('2023-10-01'::date);
SELECT create_campaign_events_partition('2023-11-01'::date);
SELECT create_campaign_events_partition('2023-12-01'::date);
SELECT create_campaign_events_partition('2024-01-01'::date);
SELECT create_campaign_events_partition('2024-02-01'::date);
SELECT create_campaign_events_partition('2024-03-01'::date);
SELECT create_campaign_events_partition('2024-04-01'::date);
SELECT create_campaign_events_partition('2024-05-01'::date);
SELECT create_campaign_events_partition('2024-06-01'::date);
SELECT create_campaign_events_partition('2024-07-01'::date);
SELECT create_campaign_events_partition('2024-08-01'::date);
SELECT create_campaign_events_partition('2024-09-01'::date);
SELECT create_campaign_events_partition('2024-10-01'::date);
SELECT create_campaign_events_partition('2024-11-01'::date);
SELECT create_campaign_events_partition('2024-12-01'::date);
SELECT create_campaign_events_partition('2025-01-01'::date);
SELECT create_campaign_events_partition('2025-02-01'::date);
SELECT create_campaign_events_partition('2025-03-01'::date);
SELECT create_campaign_events_partition('2025-04-01'::date);
SELECT create_campaign_events_partition('2025-05-01'::date);
SELECT create_campaign_events_partition('2025-06-01'::date);
SELECT create_campaign_events_partition('2025-07-01'::date);
SELECT create_campaign_events_partition('2025-08-01'::date);

-- Start timing
\timing on

-- Clear any test data first
DELETE FROM campaign_events WHERE campaign_id LIKE 'test-campaign-%';

-- PHASE 1: Migrate SENT events
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, created_at)
SELECT 
    campaign_id,
    'sent' as event_type,
    stat_date as event_date,
    sent as event_count,
    COALESCE(created_at, NOW()) as created_at
FROM campaign_daily_stat_entries 
WHERE sent > 0;

-- Check progress
DO $$
DECLARE
    sent_events INTEGER;
BEGIN
    SELECT COUNT(*) INTO sent_events FROM campaign_events WHERE event_type = 'sent';
    RAISE NOTICE 'Migrated % SENT events', sent_events;
END $$;

-- PHASE 2: Migrate OPENED events
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, created_at)
SELECT 
    campaign_id,
    'opened' as event_type,
    stat_date as event_date,
    opened as event_count,
    COALESCE(created_at, NOW()) as created_at
FROM campaign_daily_stat_entries 
WHERE opened > 0;

-- Check progress
DO $$
DECLARE
    opened_events INTEGER;
BEGIN
    SELECT COUNT(*) INTO opened_events FROM campaign_events WHERE event_type = 'opened';
    RAISE NOTICE 'Migrated % OPENED events', opened_events;
END $$;

-- PHASE 3: Migrate REPLIED events
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, created_at)
SELECT 
    campaign_id,
    'replied' as event_type,
    stat_date as event_date,
    replied as event_count,
    COALESCE(created_at, NOW()) as created_at
FROM campaign_daily_stat_entries 
WHERE replied > 0;

-- Check progress
DO $$
DECLARE
    replied_events INTEGER;
BEGIN
    SELECT COUNT(*) INTO replied_events FROM campaign_events WHERE event_type = 'replied';
    RAISE NOTICE 'Migrated % REPLIED events', replied_events;
END $$;

-- PHASE 4: Migrate POSITIVE_REPLY events
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, created_at)
SELECT 
    campaign_id,
    'positive_reply' as event_type,
    stat_date as event_date,
    positive_replies as event_count,
    COALESCE(created_at, NOW()) as created_at
FROM campaign_daily_stat_entries 
WHERE positive_replies > 0;

-- Check progress
DO $$
DECLARE
    positive_events INTEGER;
BEGIN
    SELECT COUNT(*) INTO positive_events FROM campaign_events WHERE event_type = 'positive_reply';
    RAISE NOTICE 'Migrated % POSITIVE_REPLY events', positive_events;
END $$;

-- PHASE 5: Migrate BOUNCED events
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, created_at)
SELECT 
    campaign_id,
    'bounced' as event_type,
    stat_date as event_date,
    bounced as event_count,
    COALESCE(created_at, NOW()) as created_at
FROM campaign_daily_stat_entries 
WHERE bounced > 0;

-- Check progress
DO $$
DECLARE
    bounced_events INTEGER;
BEGIN
    SELECT COUNT(*) INTO bounced_events FROM campaign_events WHERE event_type = 'bounced';
    RAISE NOTICE 'Migrated % BOUNCED events', bounced_events;
END $$;

-- PHASE 6: Migrate CLICKED events (if any)
INSERT INTO campaign_events (campaign_id, event_type, event_date, event_count, created_at)
SELECT 
    campaign_id,
    'clicked' as event_type,
    stat_date as event_date,
    clicked as event_count,
    COALESCE(created_at, NOW()) as created_at
FROM campaign_daily_stat_entries 
WHERE clicked > 0;

-- Check progress
DO $$
DECLARE
    clicked_events INTEGER;
BEGIN
    SELECT COUNT(*) INTO clicked_events FROM campaign_events WHERE event_type = 'clicked';
    RAISE NOTICE 'Migrated % CLICKED events', clicked_events;
END $$;

-- PHASE 7: Build materialized views with migrated data
RAISE NOTICE 'Building materialized views...';
SELECT refresh_campaign_stats();

-- PHASE 8: Verification - compare totals between old and new
DO $$
DECLARE
    old_total_sent BIGINT;
    old_total_opened BIGINT;
    old_total_replied BIGINT;
    old_total_positive BIGINT;
    old_total_bounced BIGINT;
    
    new_total_sent BIGINT;
    new_total_opened BIGINT;
    new_total_replied BIGINT;
    new_total_positive BIGINT;
    new_total_bounced BIGINT;
BEGIN
    -- Get totals from old table
    SELECT 
        SUM(sent), SUM(opened), SUM(replied), SUM(positive_replies), SUM(bounced)
    INTO 
        old_total_sent, old_total_opened, old_total_replied, old_total_positive, old_total_bounced
    FROM campaign_daily_stat_entries;
    
    -- Get totals from new events
    SELECT 
        SUM(CASE WHEN event_type = 'sent' THEN event_count ELSE 0 END),
        SUM(CASE WHEN event_type = 'opened' THEN event_count ELSE 0 END),
        SUM(CASE WHEN event_type = 'replied' THEN event_count ELSE 0 END),
        SUM(CASE WHEN event_type = 'positive_reply' THEN event_count ELSE 0 END),
        SUM(CASE WHEN event_type = 'bounced' THEN event_count ELSE 0 END)
    INTO
        new_total_sent, new_total_opened, new_total_replied, new_total_positive, new_total_bounced
    FROM campaign_events;
    
    RAISE NOTICE '=== MIGRATION VERIFICATION ===';
    RAISE NOTICE 'SENT: Old=%, New=%, Match=%', old_total_sent, new_total_sent, (old_total_sent = new_total_sent);
    RAISE NOTICE 'OPENED: Old=%, New=%, Match=%', old_total_opened, new_total_opened, (old_total_opened = new_total_opened);
    RAISE NOTICE 'REPLIED: Old=%, New=%, Match=%', old_total_replied, new_total_replied, (old_total_replied = new_total_replied);
    RAISE NOTICE 'POSITIVE: Old=%, New=%, Match=%', old_total_positive, new_total_positive, (old_total_positive = new_total_positive);
    RAISE NOTICE 'BOUNCED: Old=%, New=%, Match=%', old_total_bounced, new_total_bounced, (old_total_bounced = new_total_bounced);
    RAISE NOTICE '===============================';
    
    IF old_total_sent != new_total_sent OR 
       old_total_opened != new_total_opened OR 
       old_total_replied != new_total_replied OR 
       old_total_positive != new_total_positive OR 
       old_total_bounced != new_total_bounced THEN
        RAISE EXCEPTION 'MIGRATION VERIFICATION FAILED! Data mismatch detected.';
    ELSE
        RAISE NOTICE 'MIGRATION VERIFICATION PASSED! All totals match.';
    END IF;
END $$;

-- PHASE 9: Final statistics
DO $$
DECLARE
    total_events INTEGER;
    total_campaigns INTEGER;
    total_date_range TEXT;
    old_table_size TEXT;
    new_table_size TEXT;
    new_views_size TEXT;
BEGIN
    SELECT COUNT(*) INTO total_events FROM campaign_events;
    SELECT COUNT(DISTINCT campaign_id) INTO total_campaigns FROM campaign_events;
    SELECT MIN(event_date) || ' to ' || MAX(event_date) INTO total_date_range FROM campaign_events;
    
    SELECT pg_size_pretty(pg_total_relation_size('campaign_daily_stat_entries')) INTO old_table_size;
    SELECT pg_size_pretty(pg_total_relation_size('campaign_events')) INTO new_table_size;
    SELECT pg_size_pretty(
        pg_total_relation_size('campaign_daily_stats') + 
        pg_total_relation_size('campaign_weekly_stats') + 
        pg_total_relation_size('campaign_monthly_stats')
    ) INTO new_views_size;
    
    RAISE NOTICE '=== MIGRATION COMPLETE ===';
    RAISE NOTICE 'Total events migrated: %', total_events;
    RAISE NOTICE 'Unique campaigns: %', total_campaigns;
    RAISE NOTICE 'Date range: %', total_date_range;
    RAISE NOTICE 'Old table size: %', old_table_size;
    RAISE NOTICE 'New events table size: %', new_table_size;
    RAISE NOTICE 'New materialized views size: %', new_views_size;
    RAISE NOTICE '===========================';
END $$;

\timing off

-- Create a backup reference to the old table (optional - for safety)
-- COMMENT ON TABLE campaign_daily_stat_entries IS 'MIGRATED TO campaign_events - Safe to drop after verification period';