-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Category reference data if not already present
-- Safe to run multiple times
-- =========================================================
INSERT IGNORE INTO Category (Id, Name) VALUES
(1, 'Cocktail'),
(2, 'Breakfast'),
(3, 'Lunch'),
(4, 'Dinner'),
(5, 'Snack'),
(6, 'Dessert'),
(7, 'Beverage'),
(8, 'Salad'),
(9, 'Soup'),
(10, 'Appetizer'),
(11, 'Main Course'),
(12, 'Side Dish');