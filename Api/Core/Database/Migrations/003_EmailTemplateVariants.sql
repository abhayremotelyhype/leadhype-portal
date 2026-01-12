-- Migration 003: Add email template variants table
-- Purpose: Support A/B testing variants for email templates
-- Safe to run: Yes (only adds new table, no data changes)

-- Create email template variants table
CREATE TABLE IF NOT EXISTS email_template_variants (
    id VARCHAR(50) PRIMARY KEY,
    template_id VARCHAR(50) NOT NULL,
    smartlead_variant_id BIGINT NOT NULL,
    variant_label VARCHAR(10) NOT NULL,
    subject TEXT NOT NULL DEFAULT '',
    body TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_email_template_variants_template_id
        FOREIGN KEY (template_id)
        REFERENCES email_templates(id)
        ON DELETE CASCADE,
    CONSTRAINT uq_template_variant_label
        UNIQUE (template_id, variant_label)
);

-- Create index for efficient variant lookups by template
CREATE INDEX IF NOT EXISTS idx_email_template_variants_template_id
ON email_template_variants(template_id);

-- Create index for efficient lookups by Smartlead variant ID
CREATE INDEX IF NOT EXISTS idx_email_template_variants_smartlead_id
ON email_template_variants(smartlead_variant_id);

-- Add comments for documentation
COMMENT ON TABLE email_template_variants IS 'Stores A/B test variants for email templates';
COMMENT ON COLUMN email_template_variants.template_id IS 'Foreign key to email_templates table';
COMMENT ON COLUMN email_template_variants.smartlead_variant_id IS 'Variant ID from Smartlead API';
COMMENT ON COLUMN email_template_variants.variant_label IS 'Variant label (A, B, C, etc.)';
