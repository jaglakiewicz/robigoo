# Simple PowerShell script to apply UserActivityLogs migration
# Uses .NET SQLite provider

$ErrorActionPreference = "Stop"

Write-Host "Applying UserActivityLogs migration..." -ForegroundColor Cyan

$sql = @"
CREATE TABLE IF NOT EXISTS "UserActivityLogs" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "UserLogin" TEXT NOT NULL,
    "ActivityType" INTEGER NOT NULL,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT,
    "Description" TEXT NOT NULL,
    "IpAddress" TEXT,
    "UserAgent" TEXT,
    "Timestamp" TEXT NOT NULL,
    "Metadata" TEXT,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UserActivityLogs_UserId" ON "UserActivityLogs" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserActivityLogs_Timestamp" ON "UserActivityLogs" ("Timestamp");
CREATE INDEX IF NOT EXISTS "IX_UserActivityLogs_UserId_Timestamp" ON "UserActivityLogs" ("UserId", "Timestamp");
"@

try {
    # Load Microsoft.Data.Sqlite from NuGet packages
    $sqliteAssembly = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.data.sqlite.core" -Recurse -Filter "Microsoft.Data.Sqlite.dll" | Select-Object -First 1
    
    if ($sqliteAssembly) {
        Add-Type -Path $sqliteAssembly.FullName
        Write-Host "Loaded SQLite assembly from: $($sqliteAssembly.FullName)" -ForegroundColor Gray
    } else {
        throw "Microsoft.Data.Sqlite assembly not found in NuGet packages"
    }
    
    # Connect and execute
    $connectionString = "Data Source=app_v2.db"
    $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database" -ForegroundColor Gray
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $result = $command.ExecuteNonQuery()
    
    $connection.Close()
    
    Write-Host ""
    Write-Host "Migration applied successfully!" -ForegroundColor Green
    Write-Host "UserActivityLogs table and indexes created" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please apply the migration manually" -ForegroundColor Yellow
    exit 1
}
