USE swiftcookdb;

-- CATEGORY
CREATE TABLE Category (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE
);
-- TAG
CREATE TABLE Tag (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE
);
-- TOOL
CREATE TABLE Tool (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE,
	IsGlass BIT
);
-- INGREDIENT CATEGORY
-- Self-referencing hierarchy above IngredientType (Ticket 9). Only 2 levels are
-- seeded today (6 families + Miscellaneous, all root-level), but ParentCategoryId
-- supports deeper nesting later without a schema change.
CREATE TABLE IngredientCategory (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE,
    ParentCategoryId INT NULL,
    -- Designated catch-all IngredientType for this category, used by
    -- AdvancedSearch.vue's manual ingredient entry (Ticket 6) so newly-created
    -- ingredients get a real type without prompting the user to pick one. FK
    -- added below (via ALTER TABLE) once IngredientType exists.
    FallbackTypeId INT NULL,
    CONSTRAINT FK_IngredientCategory_Parent FOREIGN KEY (ParentCategoryId) REFERENCES IngredientCategory(Id) ON DELETE RESTRICT
);
-- INGREDIENT TYPE
CREATE TABLE IngredientType (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE,
    CategoryId INT NOT NULL,
    CONSTRAINT FK_IngredientType_CategoryId FOREIGN KEY (CategoryId) REFERENCES IngredientCategory(Id) ON DELETE RESTRICT
);
-- FallbackTypeId's FK must be added after IngredientType exists (circular
-- reference between the two tables). ON DELETE SET NULL rather than RESTRICT,
-- since losing a fallback type shouldn't block anything else — the category
-- would just have no fallback until reassigned (no CRUD tooling for either
-- table exists yet, so this is a theoretical safeguard for now).
ALTER TABLE IngredientCategory
    ADD CONSTRAINT FK_IngredientCategory_FallbackType FOREIGN KEY (FallbackTypeId) REFERENCES IngredientType(Id) ON DELETE SET NULL;
-- INGREDIENT
CREATE TABLE Ingredient (
	Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
	Name VARCHAR(50) NOT NULL UNIQUE,
	pluralName VARCHAR(50),
	TypeId INT,
	IsStaple BOOLEAN NOT NULL DEFAULT FALSE,
	CONSTRAINT FK_Ingred_TypeId FOREIGN KEY (TypeId) REFERENCES IngredientType(Id) ON DELETE CASCADE
	
);
-- UNIT
CREATE TABLE Unit (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE,
	PluralName VARCHAR(50),
	Description VARCHAR(50),
	Abbreviation VARCHAR(5),
	-- Ticket 21: conversion data. Only Mass/Volume convert, within their dimension,
	-- via ToBaseFactor (base: gram / millilitre). Count/Other units have no factor.
	Dimension VARCHAR(10) NOT NULL DEFAULT 'Other',
	ToBaseFactor DECIMAL(18,6) NULL
);
-- RECIPE
CREATE TABLE Recipe (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Description TEXT,
    Image VARCHAR(255),
    PrepTime int,
    CookTime int,
    Yield int,
    DateAdded DATE DEFAULT CURRENT_DATE,
    DateUpdated TIMESTAMP,
    NameNormalized VARCHAR(50) NOT NULL,
    DescriptionNormalized TEXT
);
-- RECIPE CATEGORY
CREATE TABLE RecipeCategory (
    RecipeId int NOT NULL,
    CategoryId int NOT NULL,
    PRIMARY KEY (recipeId, categoryId),
    CONSTRAINT FK_RecipeCategory_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RecipeCategory_CategoryId FOREIGN KEY (CategoryId) REFERENCES Category(Id) ON DELETE CASCADE
);
-- RECIPE TAG
CREATE TABLE RecipeTag (
    RecipeId int NOT NULL,
    TagId int NOT NULL,
    PRIMARY KEY (RecipeId, TagId),
    CONSTRAINT FK_RecipeTag_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RecipeTag_TagId FOREIGN KEY (TagId) REFERENCES Tag(Id) ON DELETE CASCADE
);
-- INGREDIENT TAG
CREATE TABLE IngredientTag (
    IngredientId int NOT NULL,
    TagId int NOT NULL,
    PRIMARY KEY (IngredientId, TagId),
    CONSTRAINT FK_IngredientTag_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredient(Id) ON DELETE CASCADE,
    CONSTRAINT FK_IngredientTag_TagId FOREIGN KEY (TagId) REFERENCES Tag(Id) ON DELETE CASCADE
);
-- RECIPE TOOL
CREATE TABLE RecipeTool (
    RecipeId int NOT NULL,
    ToolId int NOT NULL,
    PRIMARY KEY (recipeId, toolId),
    CONSTRAINT FK_RecipeTool_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RecipeTool_ToolId FOREIGN KEY (ToolId) REFERENCES Tool(Id) ON DELETE CASCADE
);
-- RECIPE INGREDIENT
CREATE TABLE RecipeIngredient (
    RecipeId int NOT NULL,
    IngredientId int NOT NULL,
    Amount DECIMAL(12,4),
	UnitId int NULL,
    Position int,
	UNIQUE KEY uq_recipeingredient (RecipeId, IngredientId, UnitId),
    CONSTRAINT FK_RecipeIngredient_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE,
	CONSTRAINT FK_RecipeIngredient_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredient(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RecipeIngredient_UnitId FOREIGN KEY (UnitId) REFERENCES Unit(Id) ON DELETE CASCADE
);
-- RECIPE INSTRUCTION
CREATE TABLE RecipeInstruction (
    RecipeId int NOT NULL,
    Step TEXT NOT NULL,
    Position int NOT NULL,
	PRIMARY KEY (RecipeId, Position),
    CONSTRAINT FK_RecipeInstruction_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE
);
-- NUTRITION
CREATE TABLE Nutrition (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    RecipeId int NOT NULL,
    Calories int,
    Carbs FLOAT,
    Protein FLOAT,
    CONSTRAINT FK_Nutrition_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE
);
-- RECIPE COOKED
CREATE TABLE Cooked (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    RecipeId int NOT NULL,
    Date TIMESTAMP,
    CONSTRAINT FK_Cooked_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE
);
-- NOTE
CREATE TABLE Note (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    RecipeId int NOT NULL,
    Note TEXT,
    CONSTRAINT FK_Note_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE
);
-- SHOPPING LIST
CREATE TABLE ShoppingList (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IngredientId int NOT NULL,
	UnitId int NOT NULL,
    Amount DECIMAL(12,4),
    CONSTRAINT FK_ShoppingList_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredient(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ShoppingList_UnitId FOREIGN KEY (UnitId) REFERENCES Unit(Id) ON DELETE CASCADE
);
-- CUPBOARD
CREATE TABLE Cupboard (
    IngredientId int NOT NULL,
	UnitId int NOT NULL,
    Amount DECIMAL(12,4),
	PRIMARY KEY (IngredientId, UnitId),
	CONSTRAINT FK_Cupboard_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredient(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Cupboard_UnitId FOREIGN KEY (UnitId) REFERENCES Unit(Id) ON DELETE CASCADE
);