-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Unit reference data if not already present
-- Safe to run multiple times
--
-- Ticket 21: Dimension + ToBaseFactor drive unit conversion. Only Mass
-- (base: gram) and Volume (base: millilitre) convert, within their own
-- dimension. Count/Other units have no factor and are compared by identity.
-- UK/metric values: tsp 5 ml, tbsp 15 ml, cup 250 ml, fl oz 28.413063 ml,
-- pint 568.26125 ml, oz 28.349523 g, lb 453.59237 g.
-- =========================================================

INSERT IGNORE INTO Unit (Id, Name, PluralName, Description, Abbreviation, Dimension, ToBaseFactor) VALUES
(1, 'sgl', 'sgls', 'Generic single item', NULL, 'Count', NULL),
(2, 'gram', 'grams', 'Metric weight', 'g', 'Mass', 1),
(3, 'kilogram', 'kilograms', 'Metric weight', 'kg', 'Mass', 1000),
(4, 'milligram', 'milligrams', 'Metric weight', 'mg', 'Mass', 0.001),
(5, 'ounce', 'ounces', 'Imperial weight', 'oz', 'Mass', 28.349523),
(6, 'pound', 'pounds', 'Imperial weight', 'lb', 'Mass', 453.59237),
(7, 'milliliter', 'milliliters', 'Metric volume', 'ml', 'Volume', 1),
(8, 'liter', 'liters', 'Metric volume', 'l', 'Volume', 1000),
(9, 'teaspoon', 'teaspoons', 'Small spoon measure', 'tsp', 'Volume', 5),
(10, 'tablespoon', 'tablespoons', 'Large spoon measure', 'tbsp', 'Volume', 15),
(11, 'cup', 'cups', 'Cooking cup measure', 'cup', 'Volume', 250),
(12, 'piece', 'pieces', 'Generic count unit', NULL, 'Count', NULL),
(13, 'slice', 'slices', 'Thinly cut piece', NULL, 'Count', NULL),
(14, 'clove', 'cloves', 'Garlic or similar', NULL, 'Count', NULL),
(15, 'pinch', 'pinches', 'Small seasoning amount', NULL, 'Other', NULL),
(16, 'dash', 'dashes', 'Small liquid measure', NULL, 'Other', NULL),
(17, 'fluid ounce', 'fluid ounces', 'UK fluid ounce', 'fl oz', 'Volume', 28.413063),
(18, 'pint', 'pints', 'UK pint', 'pt', 'Volume', 568.26125);
