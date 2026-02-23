-- Migration: Add UserSession enhancements for session management
-- Date: 2025-01-15
-- Requirements: 3.4, 3.7 - Session invalidation tracking and timeout configuration

-- Add RefreshToken column if it doesn't exist
ALTER TABLE "UserSessions" ADD COLUMN "RefreshToken" TEXT NULL;

-- Add RefreshTokenExpiresAt column if it doesn't exist
ALTER TABLE "UserSessions" ADD COLUMN "RefreshTokenExpiresAt" TEXT NULL;

-- Add IpAddress column if it doesn't exist
ALTER TABLE "UserSessions" ADD COLUMN "IpAddress" TEXT NULL;

-- Add UserAgent column if it doesn't exist
ALTER TABLE "UserSessions" ADD COLUMN "UserAgent" TEXT NULL;

-- Add SessionTimeoutMinutes column for configurable session timeout
-- Requirement 3.7: IF a session's last activity exceeds the configured timeout, 
-- THEN THE Session_Manager SHALL mark it as inactive
ALTER TABLE "UserSessions" ADD COLUMN "SessionTimeoutMinutes" INTEGER NULL;

-- Add InvalidatedAt column for explicit invalidation tracking
-- Requirement 3.4: THE Session_Manager SHALL properly invalidate sessions on logout 
-- by setting IsActive to false
ALTER TABLE "UserSessions" ADD COLUMN "InvalidatedAt" TEXT NULL;

-- Add InvalidationReason column to track why a session was invalidated
-- Supports audit trail for session invalidation events
ALTER TABLE "UserSessions" ADD COLUMN "InvalidationReason" TEXT NULL;

-- Create index for efficient session queries by user and active status
CREATE INDEX IF NOT EXISTS "IX_UserSessions_UserId_IsActive" ON "UserSessions" ("UserId", "IsActive");

-- Record migration in EF migrations history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250115_AddUserSessionEnhancements', '10.0.1');
