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
-- INGREDIENT TYPE
CREATE TABLE IngredientType (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE
);
-- INGREDIENT
CREATE TABLE Ingredient (
	Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
	Name VARCHAR(50) NOT NULL UNIQUE,
	pluralName VARCHAR(50),
	TypeId INT,
	CONSTRAINT FK_Ingred_TypeId FOREIGN KEY (TypeId) REFERENCES IngredientType(Id) ON DELETE CASCADE
	
);
-- UNIT
CREATE TABLE Unit (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) UNIQUE,
	PluralName VARCHAR(50),
	Description VARCHAR(50),
	Abbreviation VARCHAR(5)
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
    Amount FLOAT,
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
    Amount FLOAT,
    CONSTRAINT FK_ShoppingList_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredient(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ShoppingList_UnitId FOREIGN KEY (UnitId) REFERENCES Unit(Id) ON DELETE CASCADE
);
-- CUPBOARD
CREATE TABLE Cupboard (
    IngredientId int NOT NULL PRIMARY KEY,
	UnitId int NOT NULL,
    Amount FLOAT,
	CONSTRAINT FK_Cupboard_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredient(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Cupboard_UnitId FOREIGN KEY (UnitId) REFERENCES Unit(Id) ON DELETE CASCADE
);