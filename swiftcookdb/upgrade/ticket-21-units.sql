-- Ticket 21: unit dimensions/conversion, ounce split, Amount -> DECIMAL.
-- Applies to an EXISTING database (fresh databases get all of this from
-- init.sql + seeds). Safe to run multiple times. Take a backup first; the
-- matching rollback is ticket-21-down.sql.

-- 1. Amount columns: FLOAT -> DECIMAL(12,4)
ALTER TABLE RecipeIngredient MODIFY COLUMN Amount DECIMAL(12,4) NULL;
ALTER TABLE ShoppingList MODIFY COLUMN Amount DECIMAL(12,4) NULL;
ALTER TABLE Cupboard MODIFY COLUMN Amount DECIMAL(12,4) NULL;

-- 2. Unit conversion columns
ALTER TABLE Unit ADD COLUMN IF NOT EXISTS Dimension VARCHAR(10) NOT NULL DEFAULT 'Other';
ALTER TABLE Unit ADD COLUMN IF NOT EXISTS ToBaseFactor DECIMAL(18,6) NULL;

-- 3. New units (fluid ounce, pint) before remapping rows onto them
INSERT IGNORE INTO Unit (Id, Name, PluralName, Description, Abbreviation) VALUES
(17, 'fluid ounce', 'fluid ounces', 'UK fluid ounce', 'fl oz'),
(18, 'pint', 'pints', 'UK pint', 'pt');

-- 4. Remap existing 'ounce' rows used by cocktail-category ingredients (IngredientCategory 6)
--    to 'fluid ounce'. (Run only while Unit 5 is still the single 'ounce'.)
UPDATE RecipeIngredient ri
  JOIN Ingredient i ON i.Id = ri.IngredientId
  JOIN IngredientType t ON t.Id = i.TypeId
SET ri.UnitId = 17
WHERE ri.UnitId = 5 AND t.CategoryId = 6;

-- Other ounce lines in recipes that contain a cocktail-category ingredient (e.g. lime juice) are fluid too
UPDATE RecipeIngredient ri
SET ri.UnitId = 17
WHERE ri.UnitId = 5 AND EXISTS (
  SELECT 1 FROM RecipeIngredient x
    JOIN Ingredient xi ON xi.Id = x.IngredientId
    JOIN IngredientType xt ON xt.Id = xi.TypeId
  WHERE x.RecipeId = ri.RecipeId AND xt.CategoryId = 6);

UPDATE ShoppingList s
  JOIN Ingredient i ON i.Id = s.IngredientId
  JOIN IngredientType t ON t.Id = i.TypeId
SET s.UnitId = 17
WHERE s.UnitId = 5 AND t.CategoryId = 6;

UPDATE Cupboard c
  JOIN Ingredient i ON i.Id = c.IngredientId
  JOIN IngredientType t ON t.Id = i.TypeId
SET c.UnitId = 17
WHERE c.UnitId = 5 AND t.CategoryId = 6;

-- 5. Dimension data
UPDATE Unit SET Description = 'Imperial weight' WHERE Id = 5;
UPDATE Unit SET Dimension = 'Count', ToBaseFactor = NULL WHERE Id IN (1, 12, 13, 14);
UPDATE Unit SET Dimension = 'Other', ToBaseFactor = NULL WHERE Id IN (15, 16);
UPDATE Unit SET Dimension = 'Mass', ToBaseFactor = 1 WHERE Id = 2;
UPDATE Unit SET Dimension = 'Mass', ToBaseFactor = 1000 WHERE Id = 3;
UPDATE Unit SET Dimension = 'Mass', ToBaseFactor = 0.001 WHERE Id = 4;
UPDATE Unit SET Dimension = 'Mass', ToBaseFactor = 28.349523 WHERE Id = 5;
UPDATE Unit SET Dimension = 'Mass', ToBaseFactor = 453.59237 WHERE Id = 6;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 1 WHERE Id = 7;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 1000 WHERE Id = 8;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 5 WHERE Id = 9;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 15 WHERE Id = 10;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 250 WHERE Id = 11;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 28.413063 WHERE Id = 17;
UPDATE Unit SET Dimension = 'Volume', ToBaseFactor = 568.26125 WHERE Id = 18;
