# SwiftCook

Recipe database with a Vue.js frontend, ASP.NET Core API, and MariaDB.

## Functionality

1. Search recipes by name.
2. Search recipes by mandatory and optional ingredient combinations, with an optional-match threshold and pagination.
3. Browse cocktails separately (Recipe CategoryId `1`) and search them by Cocktail-category ingredients only.
4. Open a recipe or cocktail card to view its complete ingredient list and instructions.
5. Track ingredients in a cupboard and search recipes using cupboard ingredients.

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
