-- Add notes column to campaigns table
ALTER TABLE campaigns ADD COLUMN IF NOT EXISTS notes TEXT;

-- Add notes column to email_accounts table
ALTER TABLE email_accounts ADD COLUMN IF NOT EXISTS notes TEXT;