-- Ticket 11: add Ingredient.IsStaple to an EXISTING database.
-- (Fresh databases get the column from init.sql.) Safe to run multiple times.
ALTER TABLE Ingredient ADD COLUMN IF NOT EXISTS IsStaple BOOLEAN NOT NULL DEFAULT FALSE;
UPDATE Ingredient SET IsStaple = TRUE WHERE Name IN ('Salt', 'Olive Oil') AND IsStaple = FALSE;
