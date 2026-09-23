-- =========================================================
-- SwiftCook Script
-- Inserts Tool reference data if not already present
-- Safe to run multiple times
-- =========================================================
INSERT IGNORE INTO Tool (Id, Name, IsGlass) VALUES
(1, 'Coupe', 1),
(2, 'Old Fashioned', 1),
(3, 'Nick & Nora', 1),
(4, 'Martini', 1),
(5, 'Collins', 1),
(6, 'Highball', 1),
(7, 'Cocktail Shaker', 0),
(8, 'Blender', 0);