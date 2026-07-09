# Clear Database Data (Keep Admin User)
# This script will clear all data from tables but keep the admin user

Write-Host "=== Clear Database Data (Keep Admin) ===" -ForegroundColor Cyan
Write-Host ""

$dbPath = "app_v2.db"
$sqlFile = "clear-data.sql"

# Check if database exists
if (-not (Test-Path $dbPath)) {
    Write-Host "Database file not found: $dbPath" -ForegroundColor Red
    Write-Host ""
    exit
}

Write-Host "Found database: $dbPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "This will DELETE all data except the admin user:" -ForegroundColor Yellow
Write-Host "  - All inspections and protocols" -ForegroundColor Yellow
Write-Host "  - All clients" -ForegroundColor Yellow
Write-Host "  - All crop sprayers" -ForegroundColor Yellow
Write-Host "  - All users except admin" -ForegroundColor Yellow
Write-Host "  - All sessions" -ForegroundColor Yellow
Write-Host "  - All login attempts" -ForegroundColor Yellow
Write-Host "  - All security events" -ForegroundColor Yellow
Write-Host "  - All change logs" -ForegroundColor Yellow
Write-Host ""

# Confirm deletion
$confirmation = Read-Host "Type YES to confirm"

if ($confirmation -ne "YES") {
    Write-Host ""
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    Write-Host ""
    exit
}

Write-Host ""
Write-Host "Clearing data..." -ForegroundColor Yellow
Write-Host ""

# Check if sqlite3 is available
$sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue

if ($sqlite3) {
    # Use sqlite3 command line tool
    Write-Host "Using sqlite3 command line tool..." -ForegroundColor Cyan
    sqlite3 $dbPath < $sqlFile
    
    Write-Host ""
    Write-Host "=== Data Cleared Successfully ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Admin user preserved." -ForegroundColor Cyan
    Write-Host "Login: admin / admin" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "sqlite3 command not found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please use one of these methods:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Method 1: Install sqlite3 and run:" -ForegroundColor Cyan
    Write-Host "  sqlite3 app_v2.db < clear-data.sql" -ForegroundColor White
    Write-Host ""
    Write-Host "Method 2: Use DB Browser for SQLite:" -ForegroundColor Cyan
    Write-Host "  1. Open app_v2.db in DB Browser" -ForegroundColor White
    Write-Host "  2. Go to Execute SQL tab" -ForegroundColor White
    Write-Host "  3. Open and run clear-data.sql" -ForegroundColor White
    Write-Host ""
    Write-Host "Method 3: Full database reset:" -ForegroundColor Cyan
    Write-Host "  .\reset-database.ps1" -ForegroundColor White
    Write-Host ""
}
