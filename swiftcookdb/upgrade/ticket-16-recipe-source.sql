-- Ticket 16: optional source attribution for imported recipes. Safe to re-run.
ALTER TABLE Recipe ADD COLUMN IF NOT EXISTS SourceUrl VARCHAR(500) NULL;
ALTER TABLE Recipe ADD COLUMN IF NOT EXISTS SourceTitle VARCHAR(255) NULL;
ALTER TABLE Recipe ADD COLUMN IF NOT EXISTS SourceAuthor VARCHAR(255) NULL;
