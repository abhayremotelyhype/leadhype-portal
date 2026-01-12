-- Performance indexes for email accounts table
-- These indexes will significantly speed up sorting and filtering operations

-- Index for email column (most common sort field)
CREATE INDEX IF NOT EXISTS idx_email_accounts_email ON email_accounts(email);

-- Index for name column
CREATE INDEX IF NOT EXISTS idx_email_accounts_name ON email_accounts(name);

-- Index for status column (commonly filtered)
CREATE INDEX IF NOT EXISTS idx_email_accounts_status ON email_accounts(status);

-- Index for client_id (user access control filtering)
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_id ON email_accounts(client_id);

-- Index for created_at (default sort)
CREATE INDEX IF NOT EXISTS idx_email_accounts_created_at ON email_accounts(created_at);

-- Index for updated_at
CREATE INDEX IF NOT EXISTS idx_email_accounts_updated_at ON email_accounts(updated_at);

-- Composite indexes for common query patterns
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_email ON email_accounts(client_id, email);
CREATE INDEX IF NOT EXISTS idx_email_accounts_status_email ON email_accounts(status, email);

-- Index for text search on client_name
CREATE INDEX IF NOT EXISTS idx_email_accounts_client_name ON email_accounts(client_name);

-- GIN index for tags JSONB column (for tag searches)
CREATE INDEX IF NOT EXISTS idx_email_accounts_tags ON email_accounts USING GIN(tags);

-- Index for campaign relationships (improve campaign count queries)
-- Using GIN index on JSONB email_ids array for efficient array operations
CREATE INDEX IF NOT EXISTS idx_campaigns_email_ids ON campaigns USING GIN(email_ids);