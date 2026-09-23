-- =========================================================
-- SwiftCook Reseed Script
-- Inserts Ingredient Type reference data if not already present
-- Safe to run multiple times
--
-- CategoryId values map each type to its family (Ticket 9 backfill):
-- 1=Carbs, 2=Protein, 3=Dairy, 4=Produce, 5=Pantry, 6=Cocktail
-- (see 2-IngredientCategory.sql)
-- =========================================================
INSERT IGNORE INTO IngredientType (Id, Name, CategoryId) VALUES
(1,'Flour',1),
(2,'Grain',1),
(3,'Pasta',1),
(4,'Rice',1),
(5,'Legume',1),

(6,'Meat',2),
(7,'Poultry',2),
(8,'Seafood',2),
(9,'Plant Protein',2),
(10,'Egg',2),

(11,'Dairy',3),
(12,'Cheese',3),
(13,'Butter',3),
(14,'Yogurt',3),
(15,'Non-Dairy Milk',3),

(16,'Vegetable',4),
(17,'Fruit',4),
(18,'Herb',4),
(19,'Spice',4),

(20,'Oil',5),
(21,'Fat',5),
(22,'Sweetener',5),
(23,'Salt',5),
(24,'Condiment',5),

(25,'Spirit',6),
(26,'Liqueur',6),
(27,'Bitter',6),
(28,'Mixer',6),
(29,'Garnish',6);