-- Migration: Add AlcRegistration table for tracking AssemblyLoadContext instances per user session
-- Date: 2025-02-07
-- Requirements: 5.1, 5.4 - Per-user session tracking of load contexts

-- Create AlcRegistrations table
CREATE TABLE IF NOT EXISTS "AlcRegistrations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AlcRegistrations" PRIMARY KEY AUTOINCREMENT,
    "AlcId" TEXT NOT NULL,
    "UserSessionId" INTEGER NOT NULL,
    "ModuleName" TEXT NOT NULL,
    "TabIdentifier" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UnregisteredAt" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "FK_AlcRegistrations_UserSessions_UserSessionId" FOREIGN KEY ("UserSessionId") REFERENCES "UserSessions" ("Id") ON DELETE CASCADE
);

-- Create index for efficient lookup by AlcId
CREATE INDEX IF NOT EXISTS "IX_AlcRegistrations_AlcId" ON "AlcRegistrations" ("AlcId");

-- Create index for efficient lookup by UserSessionId and active status
CREATE INDEX IF NOT EXISTS "IX_AlcRegistrations_UserSessionId_IsActive" ON "AlcRegistrations" ("UserSessionId", "IsActive");

-- Record migration in EF migrations history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250207_AddAlcRegistration', '10.0.1');
