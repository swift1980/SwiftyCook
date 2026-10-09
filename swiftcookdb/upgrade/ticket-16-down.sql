-- Rollback for ticket-16-recipe-source.sql. Drops all source attribution. Safe to re-run.
ALTER TABLE Recipe DROP COLUMN IF EXISTS SourceUrl;
ALTER TABLE Recipe DROP COLUMN IF EXISTS SourceTitle;
ALTER TABLE Recipe DROP COLUMN IF EXISTS SourceAuthor;
