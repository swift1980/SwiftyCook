-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Unit reference data if not already present
-- Safe to run multiple times
-- =========================================================

INSERT IGNORE INTO Unit (Id, Name, PluralName, Description, Abbreviation) VALUES
(1, 'sgl', 'sgls', 'Generic single item', NULL),
(2, 'gram', 'grams', 'Metric weight', 'g'),
(3, 'kilogram', 'kilograms', 'Metric weight', 'kg'),
(4, 'milligram', 'milligrams', 'Metric weight', 'mg'),
(5, 'ounce', 'ounces', 'Fluid ounce', 'oz'),
(6, 'pound', 'pounds', 'Imperial weight', 'lb'),
(7, 'milliliter', 'milliliters', 'Metric volume', 'ml'),
(8, 'liter', 'liters', 'Metric volume', 'l'),
(9, 'teaspoon', 'teaspoons', 'Small spoon measure', 'tsp'),
(10, 'tablespoon', 'tablespoons', 'Large spoon measure', 'tbsp'),
(11, 'cup', 'cups', 'Cooking cup measure', 'cup'),
(12, 'piece', 'pieces', 'Generic count unit', NULL),
(13, 'slice', 'slices', 'Thinly cut piece', NULL),
(14, 'clove', 'cloves', 'Garlic or similar', NULL),
(15, 'pinch', 'pinches', 'Small seasoning amount', NULL),
(16, 'dash', 'dashes', 'Small liquid measure', NULL);