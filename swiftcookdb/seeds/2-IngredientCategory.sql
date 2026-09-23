-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Ingredient Category reference data if not already present
-- Safe to run multiple times
--
-- 6 root families (inferred from the original IngredientType seed file's own
-- blank-line grouping, see BACKLOG.md Ticket 8) + a 7th "Miscellaneous"
-- catch-all used as the default for newly-created IngredientTypes that don't
-- specify a category (see IngredientTypeController.Create).
--
-- FallbackTypeId is left NULL here and backfilled by 3-IngredientType.sql,
-- since each category's designated catch-all IngredientType (Ticket 6) can
-- only be created once IngredientType itself is seeded.
-- =========================================================
INSERT IGNORE INTO IngredientCategory (Id, Name, ParentCategoryId) VALUES
(1, 'Carbs', NULL),
(2, 'Protein', NULL),
(3, 'Dairy', NULL),
(4, 'Produce', NULL),
(5, 'Pantry', NULL),
(6, 'Cocktail', NULL),
(7, 'Miscellaneous', NULL);
