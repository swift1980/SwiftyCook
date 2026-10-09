-- Rollback for ticket-20-cook-log.sql. Drops all cook history. Safe to re-run.
ALTER TABLE MealPlanEntry DROP FOREIGN KEY IF EXISTS FK_MealPlanEntry_CookLogId;
UPDATE MealPlanEntry SET CookLogId = NULL;
DROP TABLE IF EXISTS CookLog;