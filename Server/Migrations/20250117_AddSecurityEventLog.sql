-- Migration: Add SecurityEventLog table for security event tracking
-- Requirement 7.2: Log all authentication attempts with IP address and user agent
-- Date: 2025-01-17

-- Create SecurityEventLog table
CREATE TABLE IF NOT EXISTS SecurityEventLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventType INTEGER NOT NULL,
    Details TEXT,
    IpAddress TEXT,
    UserId INTEGER,
    Login TEXT,
    UserAgent TEXT,
    OccurredAt TEXT NOT NULL DEFAULT (datetime('now')),
    CorrelationId TEXT,
    Metadata TEXT
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS IX_SecurityEventLogs_OccurredAt ON SecurityEventLogs(OccurredAt);
CREATE INDEX IF NOT EXISTS IX_SecurityEventLogs_EventType_OccurredAt ON SecurityEventLogs(EventType, OccurredAt);
CREATE INDEX IF NOT EXISTS IX_SecurityEventLogs_IpAddress_OccurredAt ON SecurityEventLogs(IpAddress, OccurredAt);
