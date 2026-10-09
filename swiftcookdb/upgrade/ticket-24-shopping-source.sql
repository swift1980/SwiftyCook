-- Ticket 24: marks shopping list rows generated from the meal planner. Safe to re-run.
ALTER TABLE ShoppingList ADD COLUMN IF NOT EXISTS Source varchar(100) NULL;

