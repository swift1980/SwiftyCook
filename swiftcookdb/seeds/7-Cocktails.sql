-- Seed Old Fashioned Recipe

INSERT IGNORE INTO Category (Name) VALUES ('Cocktail');

INSERT INTO Recipe (Name, Description, PrepTime, CookTime, Yield, NameNormalized, DescriptionNormalized)
VALUES (
  'Old Fashioned',
  'Classic American whiskey cocktail with bitters, sugar, and orange peel.',
  5, 0, 1,
  'old fashioned',
  'bourbon bitters sugar'
);
-- Insert Margarita Recipe
INSERT INTO Recipe (Name, Description, Image, PrepTime, CookTime, Yield, NameNormalized, DescriptionNormalized)
VALUES (
  'Margarita',
  'Classic tequila-based cocktail with lime and triple sec. Rim is salted for flavor balance.',
  NULL,
  5, 0, 1,
  'margarita',
  'classic tequila cocktail'
);

-- Mojito Recipe
INSERT INTO Recipe (Name, Description, PrepTime, CookTime, Yield, NameNormalized, DescriptionNormalized)
VALUES (
  'Mojito',
  'Refreshing Cuban cocktail made with rum, lime, mint, sugar, and soda water.',
  5, 0, 1,
  'mojito',
  'rum lime mint soda'
);

INSERT INTO Recipe (Name, Description, PrepTime, CookTime, Yield, NameNormalized, DescriptionNormalized)
VALUES (
  'Negroni',
  'Italian cocktail made with equal parts gin, Campari, and sweet vermouth.',
  3, 0, 1,
  'negroni',
  'gin campari vermouth'
);

-- Link Recipes to Category
INSERT INTO RecipeCategory (RecipeId, CategoryId)
VALUES (
  (SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
  (SELECT Id FROM Category WHERE Name = 'Cocktail')
);

INSERT INTO RecipeCategory (RecipeId, CategoryId)
VALUES (
  (SELECT Id FROM Recipe WHERE Name = 'Margarita'),
  (SELECT Id FROM Category WHERE Name = 'Cocktail')
);

INSERT INTO RecipeCategory (RecipeId, CategoryId)
VALUES (
  (SELECT Id FROM Recipe WHERE Name = 'Mojito'),
  (SELECT Id FROM Category WHERE Name = 'Cocktail')
);

INSERT INTO RecipeCategory (RecipeId, CategoryId)
VALUES (
  (SELECT Id FROM Recipe WHERE Name = 'Negroni'),
  (SELECT Id FROM Category WHERE Name = 'Cocktail')
);

-- Link Recipes to Ingredients
INSERT INTO RecipeIngredient (RecipeId, IngredientId, Amount, UnitId, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 (SELECT Id FROM Ingredient WHERE Name = 'Bourbon'),
 2.0, (SELECT Id FROM Unit WHERE Name = 'ounce'), 1),

((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 (SELECT Id FROM Ingredient WHERE Name = 'Sugar cube'),
 1.0, (SELECT Id FROM Unit WHERE Name = 'sgl'), 2),

((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 (SELECT Id FROM Ingredient WHERE Name = 'Angostura bitters'),
 2.0, (SELECT Id FROM Unit WHERE Name = 'dash'), 3),

((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 (SELECT Id FROM Ingredient WHERE Name = 'Orange peel'),
 NULL, (SELECT Id FROM Unit WHERE Name = 'sgl'), 4);

INSERT INTO RecipeIngredient (RecipeId, IngredientId, Amount, UnitId, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 (SELECT Id FROM Ingredient WHERE Name = 'Tequila'),
 1.5,
 (SELECT Id FROM Unit WHERE Name = 'ounce'),
 1),

((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 (SELECT Id FROM Ingredient WHERE Name = 'Triple sec'),
 0.5,
 (SELECT Id FROM Unit WHERE Name = 'ounce'),
 2),

((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 (SELECT Id FROM Ingredient WHERE Name = 'Lime juice'),
 1.0,
 (SELECT Id FROM Unit WHERE Name = 'ounce'),
 3),

((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 (SELECT Id FROM Ingredient WHERE Name = 'Salt'),
 NULL,
 (SELECT Id FROM Unit WHERE Name = 'sgl'),
 4);

INSERT INTO RecipeIngredient (RecipeId, IngredientId, Amount, UnitId, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 (SELECT Id FROM Ingredient WHERE Name = 'White rum'),
 2.0, (SELECT Id FROM Unit WHERE Name = 'ounce'), 1),

((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 (SELECT Id FROM Ingredient WHERE Name = 'Lime juice'),
 1.0, (SELECT Id FROM Unit WHERE Name = 'ounce'), 2),

((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 (SELECT Id FROM Ingredient WHERE Name = 'Sugar'),
 2.0, (SELECT Id FROM Unit WHERE Name = 'sgl'), 3),

((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 (SELECT Id FROM Ingredient WHERE Name = 'Mint leaf'),
 6.0, (SELECT Id FROM Unit WHERE Name = 'sgl'), 4),

((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 (SELECT Id FROM Ingredient WHERE Name = 'Soda water'),
 NULL, (SELECT Id FROM Unit WHERE Name = 'dash'), 5);
 
 INSERT INTO RecipeIngredient (RecipeId, IngredientId, Amount, UnitId, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 (SELECT Id FROM Ingredient WHERE Name = 'Gin'),
 1.0, (SELECT Id FROM Unit WHERE Name = 'ounce'), 1),

((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 (SELECT Id FROM Ingredient WHERE Name = 'Campari'),
 1.0, (SELECT Id FROM Unit WHERE Name = 'ounce'), 2),

((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 (SELECT Id FROM Ingredient WHERE Name = 'Sweet vermouth'),
 1.0, (SELECT Id FROM Unit WHERE Name = 'ounce'), 3),

((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 (SELECT Id FROM Ingredient WHERE Name = 'Orange slice'),
 NULL, NULL, 4);
 
 -- Link Recipes to Instructions
INSERT INTO RecipeInstruction (RecipeId, Step, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 'Muddle sugar cube with bitters and a splash of water in a glass.', 1),
((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 'Add bourbon and stir with ice.', 2),
((SELECT Id FROM Recipe WHERE Name = 'Old Fashioned'),
 'Garnish with an orange peel.', 3);
 
INSERT INTO RecipeInstruction (RecipeId, Step, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 'Rub the rim of the glass with the lime slice to make the salt stick to it. Take care to moisten only the outer rim and sprinkle the salt on it.', 1),

((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 'The salt should present to the lips of the imbiber and never mix into the cocktail.', 2),

((SELECT Id FROM Recipe WHERE Name = 'Margarita'),
 'Shake the other ingredients with ice, then carefully pour into the glass.', 3);
 
INSERT INTO RecipeInstruction (RecipeId, Step, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 'Muddle mint leaves with sugar and lime juice.', 1),
((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 'Add rum and fill the glass with ice.', 2),
((SELECT Id FROM Recipe WHERE Name = 'Mojito'),
 'Top with soda water and garnish with mint.', 3);
 
 INSERT INTO RecipeInstruction (RecipeId, Step, Position)
VALUES
((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 'Combine gin, Campari, and vermouth in a mixing glass with ice.', 1),
((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 'Stir well and strain into a chilled glass.', 2),
((SELECT Id FROM Recipe WHERE Name = 'Negroni'),
 'Garnish with an orange slice.', 3);
