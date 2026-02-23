-- Migration: Add optimistic concurrency support to entity models
-- Date: 2025-01-16
-- Requirements: 4.1 - THE Database_Access_Layer SHALL implement optimistic concurrency control using row version tokens

-- ============================================================================
-- Add RowVersion column to Machines (CropSprayer) table
-- This column is used as an optimistic concurrency token to detect concurrent modifications
-- ============================================================================
ALTER TABLE "Machines" ADD COLUMN "RowVersion" BLOB NULL;

-- ============================================================================
-- Add RowVersion column to InspectionProtocols table
-- This column is used as an optimistic concurrency token to detect concurrent modifications
-- ============================================================================
ALTER TABLE "InspectionProtocols" ADD COLUMN "RowVersion" BLOB NULL;

-- ============================================================================
-- Add Version column to InspectionProtocols table for application-level versioning
-- This provides an additional layer of concurrency control at the application level
-- ============================================================================
ALTER TABLE "InspectionProtocols" ADD COLUMN "Version" INTEGER NOT NULL DEFAULT 0;

-- ============================================================================
-- Initialize RowVersion for existing records in Machines table
-- SQLite doesn't have a native ROWVERSION type, so we use a BLOB with a timestamp-based value
-- ============================================================================
UPDATE "Machines" SET "RowVersion" = randomblob(8) WHERE "RowVersion" IS NULL;

-- ============================================================================
-- Initialize RowVersion for existing records in InspectionProtocols table
-- ============================================================================
UPDATE "InspectionProtocols" SET "RowVersion" = randomblob(8) WHERE "RowVersion" IS NULL;

-- ============================================================================
-- Record migration in EF migrations history
-- ============================================================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250116_AddOptimisticConcurrency', '10.0.1');
