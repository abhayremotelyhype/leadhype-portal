-- Increase field sizes for lead conversations to prevent varchar length errors
-- Some leads may have very long names or email addresses that exceed 255 characters

-- Increase lead_email field size
ALTER TABLE lead_conversations 
ALTER COLUMN lead_email TYPE VARCHAR(500);

-- Increase lead_first_name field size
ALTER TABLE lead_conversations 
ALTER COLUMN lead_first_name TYPE VARCHAR(500);

-- Increase lead_last_name field size
ALTER TABLE lead_conversations 
ALTER COLUMN lead_last_name TYPE VARCHAR(500);

-- Also increase the field sizes in lead_email_history table to match
ALTER TABLE lead_email_history 
ALTER COLUMN lead_email TYPE VARCHAR(500);

-- Increase subject field size in lead_email_history as email subjects can be very long
ALTER TABLE lead_email_history 
ALTER COLUMN subject TYPE TEXT;

-- Add comments
COMMENT ON COLUMN lead_conversations.lead_email IS 'Lead email address (increased to 500 chars to handle long emails)';
COMMENT ON COLUMN lead_conversations.lead_first_name IS 'Lead first name (increased to 500 chars to handle long names)';
COMMENT ON COLUMN lead_conversations.lead_last_name IS 'Lead last name (increased to 500 chars to handle long names)';
COMMENT ON COLUMN lead_email_history.lead_email IS 'Lead email address (increased to 500 chars to handle long emails)';
COMMENT ON COLUMN lead_email_history.subject IS 'Email subject line (changed to TEXT to handle very long subjects)';