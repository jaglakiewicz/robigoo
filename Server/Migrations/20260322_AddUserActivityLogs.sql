-- Migration: Add UserActivityLogs table
-- Date: 2026-03-22
-- Description: Creates the UserActivityLogs table for comprehensive user activity tracking

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

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS "IX_UserActivityLogs_UserId" ON "UserActivityLogs" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserActivityLogs_Timestamp" ON "UserActivityLogs" ("Timestamp");
CREATE INDEX IF NOT EXISTS "IX_UserActivityLogs_UserId_Timestamp" ON "UserActivityLogs" ("UserId", "Timestamp");
