-- Rollback for ticket-22-cupboard-units.sql. LOSSY: the old schema allows one row per ingredient,
-- so where an ingredient has several units only the row with the lowest UnitId is kept. Safe to re-run.
DELETE c FROM Cupboard c
  JOIN Cupboard keep ON keep.IngredientId = c.IngredientId AND keep.UnitId < c.UnitId;

SET @has_pk := (SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Cupboard'
    AND INDEX_NAME = 'PRIMARY' AND COLUMN_NAME = 'UnitId');
SET @sql := IF(@has_pk > 0,
  'ALTER TABLE Cupboard DROP PRIMARY KEY, ADD PRIMARY KEY (IngredientId)',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
