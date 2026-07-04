-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Ingredient Type reference data if not already present
-- Safe to run multiple times
-- =========================================================
INSERT IGNORE INTO IngredientType (Id, Name) VALUES
(1,'Flour'),
(2,'Grain'),
(3,'Pasta'),
(4,'Rice'),
(5,'Legume'),

(6,'Meat'),
(7,'Poultry'),
(8,'Seafood'),
(9,'Plant Protein'),
(10,'Egg'),

(11,'Dairy'),
(12,'Cheese'),
(13,'Butter'),
(14,'Yogurt'),
(15,'Non-Dairy Milk'),

(16,'Vegetable'),
(17,'Fruit'),
(18,'Herb'),
(19,'Spice'),

(20,'Oil'),
(21,'Fat'),
(22,'Sweetener'),
(23,'Salt'),
(24,'Condiment'),

(25,'Spirit'),
(26,'Liqueur'),
(27,'Bitter'),
(28,'Mixer'),
(29,'Garnish');