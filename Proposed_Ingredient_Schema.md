# Recipe Ingredient Master Data Schema
**Recommended Enterprise-Scale Ingredient Classification Model**

This document defines a scalable ingredient master-data schema suitable for:

- Recipe databases
- Meal planning applications
- Nutritional analysis
- Inventory systems
- AI recipe generation
- Substitution engines
- Multi-language recipe platforms

---

# Design Principles

## Hierarchy vs Tags

Defines **what an ingredient is**.

Recommended rule:

Hierarchy = Classification
Tags = Characteristics

Recommended Final Architecture:

Category
↓
Ingredient
↓
Variant
↓
Product
↓
Brand

Combined with Tags

Tags
Nutrition
Aliases
Conversions
Substitutions

---

# Logical Data Model

IngredientCategory
│
▼
Ingredient
│
├── IngredientVariant
│ │
│ ▼
│ IngredientProduct
│ │
│ ├── ProductNutrition
│ ├── ProductPrice
│ └── Brand
│
├── IngredientAlias
├── IngredientTag
├── IngredientNutrition
├── IngredientConversion
└── IngredientSubstitution
 
RecipeIngredient
│
├── IngredientId
└── ProductId (optional)

---
Example Record:

Category
└─ Dairy
 
Ingredient
└─ Parmesan Cheese
 
Variant
└─ Grated Parmesan
 
Brand
└─ Galbani
 
Product
└─ Galbani Grated Parmesan 100g
 
Tags
├─ Italian
├─ Vegetarian
├─ Garnish
└─ High Protein

---
# Core Ingredient Hierarchy

## IngredientCategory

Supports unlimited category depth using self-referencing parent IDs.

## IngredientCategory
CREATE TABLE IngredientCategory
(
    CategoryId BIGINT PRIMARY KEY,
    ParentCategoryId BIGINT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    CategoryCode VARCHAR(50) UNIQUE,
    SortOrder INT DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_IngredientCategory_Parent
        FOREIGN KEY (ParentCategoryId)
        REFERENCES IngredientCategory(CategoryId)
);

## Ingredient
CREATE TABLE Ingredient
(
    IngredientId BIGINT PRIMARY KEY,
    CategoryId BIGINT NOT NULL,
    Name VARCHAR(200) NOT NULL,
    CanonicalName VARCHAR(200) NOT NULL,
    Description VARCHAR(1000),
    DefaultUnitId BIGINT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL,
    ModifiedDate DATETIME2 NOT NULL,
    CONSTRAINT FK_Ingredient_Category
        FOREIGN KEY(CategoryId)
        REFERENCES IngredientCategory(CategoryId)
);

## ProcessingType

CREATE TABLE ProcessingType
(
	ProcessingTypeId BIGINT PRIMARY KEY,
	Name VARCHAR(100) NOT NULL,
	Description VARCHAR(500)
);

## IngredientVariant

CREATE TABLE IngredientVariant
(
    VariantId BIGINT PRIMARY KEY,
    IngredientId BIGINT NOT NULL,
    VariantName VARCHAR(200) NOT NULL,
    ProcessingTypeId BIGINT NULL,
    IsPreferred BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Variant_Ingredient
        FOREIGN KEY(IngredientId)
        REFERENCES Ingredient(IngredientId)
);

## Brand

CREATE TABLE Brand
(
    BrandId BIGINT PRIMARY KEY,
    BrandName VARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

## IngredientProduct

CREATE TABLE IngredientProduct
(
    ProductId BIGINT PRIMARY KEY,

    IngredientId BIGINT NOT NULL,

    VariantId BIGINT NULL,

    BrandId BIGINT NOT NULL,

    ProductName VARCHAR(300) NOT NULL,

    PackageSize DECIMAL(18,4),

    PackageUnitId BIGINT NULL,

    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Product_Ingredient
        FOREIGN KEY (IngredientId)
        REFERENCES Ingredient(IngredientId),

    CONSTRAINT FK_Product_Variant
        FOREIGN KEY (VariantId)
        REFERENCES IngredientVariant(VariantId),

    CONSTRAINT FK_Product_Brand
        FOREIGN KEY (BrandId)
        REFERENCES Brand(BrandId)
);

## Tag Model

CREATE TABLE TagType
(
    TagTypeId BIGINT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL
);

CREATE TABLE Tag
(
    TagId BIGINT PRIMARY KEY,
    TagTypeId BIGINT NOT NULL,
    TagName VARCHAR(100) NOT NULL,
    CONSTRAINT FK_Tag_TagType
        FOREIGN KEY(TagTypeId)
        REFERENCES TagType(TagTypeId)
);

CREATE TABLE IngredientTag
(
    IngredientId BIGINT NOT NULL,
    TagId BIGINT NOT NULL,
    PRIMARY KEY (IngredientId, TagId),
	CONSTRAINT FK_Tag_Ingredient
		FOREIGN KEY (IngredientId)
		REFERENCES Ingredient(IngredientId),
	
	CONSTRAINT FK_Ingredient_Tag
		FOREIGN KEY (TagId)
		REFERENCES Tag(TagId)
);

## Nutrition

CREATE TABLE IngredientNutrition
(
    IngredientId BIGINT PRIMARY KEY,
    CaloriesPer100g DECIMAL(10,2),
    ProteinPer100g DECIMAL(10,2),
    FatPer100g DECIMAL(10,2),
    CarbsPer100g DECIMAL(10,2),
    FibrePer100g DECIMAL(10,2),
    SugarPer100g DECIMAL(10,2),
    SodiumMgPer100g DECIMAL(10,2),
	CONSTRAINT FK_Ingredient_Nutrition
		FOREIGN KEY (IngredientId)
		REFERENCES Ingredient(IngredientId)
);

CREATE TABLE ProductNutrition
(
    ProductId BIGINT PRIMARY KEY,
    CaloriesPer100g DECIMAL(10,2),
    ProteinPer100g DECIMAL(10,2),
    FatPer100g DECIMAL(10,2),
    CarbsPer100g DECIMAL(10,2),
    FibrePer100g DECIMAL(10,2),
    SugarPer100g DECIMAL(10,2),
    SodiumMgPer100g DECIMAL(10,2),
	CONSTRAINT FK_Product_Nutrition
		FOREIGN KEY (ProductId)
		REFERENCES Product(ProductId)
);

## Units
CREATE TABLE Unit
(
    UnitId BIGINT PRIMARY KEY,
    UnitName VARCHAR(100),
    Abbreviation VARCHAR(20),
    IsWeight BIT,
    IsVolume BIT
);

## Ingredient Conversion
CREATE TABLE IngredientConversion
(
    ConversionId BIGINT PRIMARY KEY,
    IngredientId BIGINT NOT NULL,
    SourceUnitId BIGINT NOT NULL,
    TargetUnitId BIGINT NOT NULL,
    ConversionFactor DECIMAL(20,8) NOT NULL
	CONSTRAINT FK_Ingredient_Conversion
		FOREIGN KEY (IngredientId)
		REFERENCES Ingredient(IngredientId)
);

## Ingredient Substitution
CREATE TABLE IngredientSubstitution
(
    IngredientId BIGINT NOT NULL,
    SubstituteIngredientId BIGINT NOT NULL,
    SimilarityScore DECIMAL(5,2),
    Notes VARCHAR(1000),
    PRIMARY KEY (IngredientId, SubstituteIngredientId)
);

## RecipeIngredient

CREATE TABLE RecipeIngredient
(
    RecipeIngredientId BIGINT PRIMARY KEY,
    RecipeId BIGINT NOT NULL,
    IngredientId BIGINT NOT NULL,
	ProductId BIGINT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    UnitId BIGINT NOT NULL,
    PreparationNote VARCHAR(500),
	CONSTRAINT FK_Recipe_Ingredient
		FOREIGN KEY (IngredientId)
		REFERENCES Ingredient(IngredientId),
	CONSTRAINT FK_Recipe_Product
		FOREIGN KEY (ProductId)
		REFERENCES Product(ProductId),
	CONSTRAINT FK_Recipe_Ingredient_Unit
		FOREIGN KEY (UnitId)
		REFERENCES UnitId(UnitId)
);
```

# Recommended Strategy

- Hierarchical categories for classification
- Canonical ingredients with variants
- Ingredient aliases for search
- Tag-based metadata for diets, cuisines and functions
- Nutrition per 100g
- Ingredient-specific measurement conversions
- Ingredient substitution mappings
- Indexed search columns for scalability