-- Rollback for ticket-21-units.sql. Cocktail rows go back to unit 5 ('ounce'),
-- the unit's original (ambiguous) meaning; Amount returns to FLOAT. Safe to re-run.

UPDATE RecipeIngredient SET UnitId = 5 WHERE UnitId = 17;
UPDATE ShoppingList SET UnitId = 5 WHERE UnitId = 17;
UPDATE Cupboard SET UnitId = 5 WHERE UnitId = 17;
-- Pint (18) is new and has no earlier equivalent, so it is deleted only when unused.
DELETE FROM Unit WHERE Id = 17;
DELETE u FROM Unit u
  LEFT JOIN RecipeIngredient ri ON ri.UnitId = u.Id
  LEFT JOIN ShoppingList s ON s.UnitId = u.Id
  LEFT JOIN Cupboard c ON c.UnitId = u.Id
WHERE u.Id = 18 AND ri.UnitId IS NULL AND s.UnitId IS NULL AND c.UnitId IS NULL;

UPDATE Unit SET Description = 'Fluid ounce' WHERE Id = 5;
ALTER TABLE Unit DROP COLUMN IF EXISTS ToBaseFactor;
ALTER TABLE Unit DROP COLUMN IF EXISTS Dimension;

ALTER TABLE RecipeIngredient MODIFY COLUMN Amount FLOAT NULL;
ALTER TABLE ShoppingList MODIFY COLUMN Amount FLOAT NULL;
ALTER TABLE Cupboard MODIFY COLUMN Amount FLOAT NULL;
