# Reset Database Script
# This script will delete the existing database and let the application recreate it fresh

Write-Host "=== Database Reset Script ===" -ForegroundColor Cyan
Write-Host ""

$dbPath = "app_v2.db"
$dbWalPath = "app_v2.db-wal"
$dbShmPath = "app_v2.db-shm"

# Check if database exists
if (Test-Path $dbPath) {
    Write-Host "Found database: $dbPath" -ForegroundColor Yellow
    
    # Confirm deletion
    $confirmation = Read-Host "Delete ALL data? Type YES to confirm"
    
    if ($confirmation -eq "YES") {
        Write-Host ""
        Write-Host "Deleting database files..." -ForegroundColor Yellow
        
        # Remove main database file
        if (Test-Path $dbPath) {
            Remove-Item $dbPath -Force
            Write-Host "Deleted: $dbPath" -ForegroundColor Green
        }
        
        # Remove WAL file if exists
        if (Test-Path $dbWalPath) {
            Remove-Item $dbWalPath -Force
            Write-Host "Deleted: $dbWalPath" -ForegroundColor Green
        }
        
        # Remove SHM file if exists
        if (Test-Path $dbShmPath) {
            Remove-Item $dbShmPath -Force
            Write-Host "Deleted: $dbShmPath" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "=== Database Reset Complete ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "Start the application to create a fresh database." -ForegroundColor Cyan
        Write-Host "Default login: admin / admin" -ForegroundColor Yellow
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "Database reset cancelled." -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Host "Database file not found: $dbPath" -ForegroundColor Red
    Write-Host ""
}
