-- Ticket 20: cook log. Safe to re-run.
CREATE TABLE IF NOT EXISTS CookLog (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
    RecipeId int NOT NULL,
    CookedOn DATE NOT NULL,
    Servings int NOT NULL,
    Notes VARCHAR(500) NULL,
    INDEX IX_CookLog_RecipeId_CookedOn (RecipeId, CookedOn),
    CONSTRAINT FK_CookLog_RecipeId FOREIGN KEY (RecipeId) REFERENCES Recipe(Id) ON DELETE CASCADE
);

-- Clear any dangling links before the foreign key is added.
UPDATE MealPlanEntry SET CookLogId = NULL
WHERE CookLogId IS NOT NULL AND CookLogId NOT IN (SELECT Id FROM CookLog);

ALTER TABLE MealPlanEntry
    ADD CONSTRAINT FK_MealPlanEntry_CookLogId
    FOREIGN KEY IF NOT EXISTS (CookLogId) REFERENCES CookLog(Id) ON DELETE SET NULL;