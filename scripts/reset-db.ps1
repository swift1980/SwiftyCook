Write-Host "Resetting SwiftCook MariaDB (dev mode)..."

# Stop and remove containers + volumes
docker-compose down -v

# Start fresh DB with seeds
docker-compose up -d

# Wait a few seconds for MariaDB to initialize
Start-Sleep -Seconds 10

# Check that seeds were applied

mysql.exe -h 127.0.0.1 -P 13306 -u swiftchef -pGL@D0s swiftcookdb -e "SELECT * FROM RecipeIngredient"
mysql.exe -h 127.0.0.1 -P 13306 -u swiftchef -pGL@D0s swiftcookdb -e "SELECT COUNT(*) AS Units FROM Unit; SELECT COUNT(*) AS Ingredients FROM Ingredient; SELECT COUNT(*) AS Categories FROM Category; SELECT COUNT(*) AS Tags FROM Tag;"