# Apply UserActivityLogs migration
# This script creates the UserActivityLogs table in the SQLite database

Write-Host "Applying UserActivityLogs migration..." -ForegroundColor Cyan

$sqlScript = @"
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

# Save SQL to temp file
$tempFile = [System.IO.Path]::GetTempFileName()
$sqlScript | Out-File -FilePath $tempFile -Encoding UTF8

try {
    # Try using dotnet ef to execute raw SQL
    Write-Host "Executing SQL migration..." -ForegroundColor Yellow
    
    # Load System.Data.SQLite assembly
    Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\*\System.Data.SQLite.dll" -ErrorAction SilentlyContinue
    
    # Connect to database and execute SQL
    $connectionString = "Data Source=app_v2.db"
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $command.ExecuteNonQuery() | Out-Null
    
    $connection.Close()
    
    Write-Host "Migration applied successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Run the SQL manually using a SQLite tool" -ForegroundColor Yellow
    Write-Host "SQL file location: Migrations/20260322_AddUserActivityLogs.sql" -ForegroundColor Yellow
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}
