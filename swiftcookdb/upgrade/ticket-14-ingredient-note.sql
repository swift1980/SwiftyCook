-- Ticket 14: optional preparation note per recipe ingredient line (e.g. "finely chopped", "to taste").
-- Safe to re-run.
ALTER TABLE RecipeIngredient ADD COLUMN IF NOT EXISTS Note VARCHAR(255) NULL;
