-- Rollback for ticket-14-ingredient-note.sql. Drops all ingredient notes. Safe to re-run.
ALTER TABLE RecipeIngredient DROP COLUMN IF EXISTS Note;
