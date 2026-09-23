-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Tags reference data if not already present
-- Safe to run multiple times
-- =========================================================
INSERT IGNORE INTO Tag (Id, Name) VALUES
(1, 'Vegetarian'),
(2, 'Vegan'),
(3, 'Gluten-Free'),
(4, 'Dairy-Free'),
(5, 'Low-Carb'),
(6, 'High-Protein'),
(7, 'Keto'),
(8, 'Paleo'),
(9, 'Spicy'),
(10, 'Quick');