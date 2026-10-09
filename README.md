# SwiftCook

Recipe database with a Vue.js frontend, ASP.NET Core API, and MariaDB.

## Functionality

1. Search recipes by name.
2. Search recipes by mandatory and optional ingredient combinations, with an optional-match threshold and pagination.
3. Browse cocktails separately (Recipe CategoryId `1`) and search them by Cocktail-category ingredients only.
4. Open a recipe or cocktail card to view its complete ingredient list and instructions.
5. Track ingredients in a cupboard (several units per ingredient, with unit conversion within mass and volume) and find recipes that can be made from it, optionally including recipes with missing ingredients.
6. Manage ingredients (including staples and notes) and recipes.
7. Import recipes from JSON with a dry-run preview; see [docs/recipe-import-format.md](docs/recipe-import-format.md).
8. Plan meals in a weekly meal planner, add recipes to it from the recipe card, and add what is missing for the week to the shopping list.
9. Keep a cook log per recipe ("last made", history) and mark planned meals as made.
10. Keep a shopping list and move purchased items to the cupboard.

## Database upgrades

`swiftcookdb/init.sql` only runs on a fresh database. For an existing database, run the matching
`swiftcookdb/upgrade/ticket-N-*.sql` script (re-runnable); each has a `ticket-N-down.sql` rollback.

## Database backups

`docker compose up -d` starts a `mariadb-backup` service after MariaDB is
healthy. It creates a compressed, transaction-consistent backup immediately,
then every 24 hours, in the named `mariadb_backups` Docker volume. Backups
older than seven days are removed automatically.

List backups:

```sh
docker compose exec mariadb-backup ls -lh /backups
```

Copy a backup to an external drive in PowerShell:

```powershell
$backupContainer = docker compose ps -q mariadb-backup
docker cp "${backupContainer}:/backups/swiftcookdb-<timestamp>.sql.gz" "E:\SwiftyCookBackups\"
```

Verify that the external copy matches the source before relying on it:

```powershell
docker compose exec -T mariadb-backup sha256sum /backups/swiftcookdb-<timestamp>.sql.gz
Get-FileHash "E:\SwiftyCookBackups\swiftcookdb-<timestamp>.sql.gz" -Algorithm SHA256
```

The two SHA-256 values must match. An external copy is independently
restorable and protects against loss of the local Docker volume or host drive.

Restore a backup into the configured database after stopping application
writes:

```sh
docker compose exec -T mariadb-backup \
  sh -c 'gunzip -c /backups/<backup-file>.sql.gz' |
  docker compose exec -T mariadb mariadb -u"$DB_USER" -p"$DB_PASSWORD" "$DB_NAME"
```
