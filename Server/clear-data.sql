-- Clear Database Data (Keep Admin User)
-- Run this with: sqlite3 app_v2.db < clear-data.sql

-- Disable foreign key constraints temporarily
PRAGMA foreign_keys = OFF;

BEGIN TRANSACTION;

-- Delete inspection-related data
DELETE FROM InspectionItems;
DELETE FROM InspectionProtocols;
DELETE FROM Inspections;

-- Delete clients and machines
DELETE FROM Clients;
DELETE FROM Machines;

-- Delete activity and audit logs
DELETE FROM UserActivityLogs;
DELETE FROM UserSessions;
DELETE FROM LoginAttempts;
DELETE FROM SecurityEventLogs;
DELETE FROM ChangeLogs;

-- Delete all users except admin
DELETE FROM Users WHERE Login != 'admin';

-- Reset auto-increment counters (optional)
DELETE FROM sqlite_sequence WHERE name IN (
    'InspectionItems',
    'InspectionProtocols', 
    'Inspections',
    'Clients',
    'Machines',
    'UserActivityLogs',
    'UserSessions',
    'LoginAttempts',
    'SecurityEventLogs',
    'ChangeLogs'
);

COMMIT;

-- Re-enable foreign key constraints
PRAGMA foreign_keys = ON;

-- Show remaining data
SELECT 'Users remaining:' as Info, COUNT(*) as Count FROM Users;
SELECT 'Admin user:' as Info, Login, FirstName, LastName FROM Users WHERE Login = 'admin';
