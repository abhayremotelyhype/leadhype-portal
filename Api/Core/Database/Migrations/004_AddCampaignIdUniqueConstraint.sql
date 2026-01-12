-- Migration: Add unique constraint to campaigns.campaign_id
-- Purpose: Prevent duplicate campaign records and ensure data integrity
-- Date: 2026-01-10

-- Add unique constraint to campaign_id column
-- This ensures each Smartlead campaign_id can only appear once in the database
ALTER TABLE campaigns
ADD CONSTRAINT unique_campaign_id UNIQUE (campaign_id);

-- Note: The existing index idx_campaigns_campaign_id will be used by this constraint
-- for efficient uniqueness checking
