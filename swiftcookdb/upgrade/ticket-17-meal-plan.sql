-- Ticket 17: meal planner. Safe to re-run.
CREATE TABLE IF NOT EXISTS MealPlanEntry (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Date DATE NOT NULL,
    MealType VARCHAR(20) NOT NULL,
    RecipeId int NOT NULL,
    Servings int NOT NULL,
    SortOrder int NOT NULL DEFAULT 0,
    CookLogId int NULL,
    INDEX IX_MealPlanEntry_Date (Date),
    CONSTRAINT CK_MealPlanEntry_MealType CHECK (MealType IN ('Breakfast','Lunch','Dinner','Snack','Cocktail')),
    CONSTRAINT FK_MealPlanEntry_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE RESTRICT
);
