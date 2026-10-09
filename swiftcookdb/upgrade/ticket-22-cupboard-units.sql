-- Ticket 22: Cupboard may hold several units per ingredient (primary key becomes IngredientId + UnitId).
-- Safe to re-run. Existing rows are unaffected (they are already unique per ingredient).
SET @has_pk := (SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Cupboard'
    AND INDEX_NAME = 'PRIMARY' AND COLUMN_NAME = 'UnitId');
SET @sql := IF(@has_pk = 0,
  'ALTER TABLE Cupboard DROP PRIMARY KEY, ADD PRIMARY KEY (IngredientId, UnitId)',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
