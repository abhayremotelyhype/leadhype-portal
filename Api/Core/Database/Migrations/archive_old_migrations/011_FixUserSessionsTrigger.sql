-- Fix user_sessions trigger issue
-- The update_user_sessions_updated_at trigger is trying to update an 'updated_at' column that doesn't exist

-- Drop the incorrect trigger that references non-existent updated_at column
DROP TRIGGER IF EXISTS update_user_sessions_updated_at ON user_sessions;

-- Since user_sessions tracks last_accessed_at instead of updated_at, we don't need this trigger
-- The last_accessed_at is managed programmatically when sessions are accessed