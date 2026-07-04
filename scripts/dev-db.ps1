param (
    [string]$action = "up"
)

$dockerDir = Join-Path $PSScriptRoot "../docker"
$compose = "docker-compose -f `"$dockerDir/docker-compose.yml`""

switch ($action) {
    "up" {
        Write-Host "Starting MariaDB container..."
        Invoke-Expression "$compose up -d"
    }
    "down" {
        Write-Host "Stopping and removing MariaDB container..."
        Invoke-Expression "$compose down -v"
    }
    "logs" {
        Write-Host "Showing MariaDB logs..."
        docker-compose -f "$dockerDir/docker-compose.yml" logs -f
    }
	"reset" {
		Invoke-Expression "$compose down -v"
        Invoke-Expression "$compose up -d"
	}
    default {
        Write-Host "Usage: ./dev-db.ps1 [up|down|logs|reset]"
    }
}