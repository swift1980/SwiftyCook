-- Rollback for ticket-24-shopping-source.sql. Safe to re-run.
ALTER TABLE ShoppingList DROP COLUMN IF EXISTS Source;

