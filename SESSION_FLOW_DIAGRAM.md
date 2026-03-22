# Session Management Flow Diagrams

## Current Flow (PROBLEMATIC)

### Login Flow - Current
```
User enters credentials
        ↓
Client sends POST /api/auth/login
        ↓
Server checks credentials ✓
        ↓
Server checks for existing sessions
        ↓
    [Session exists?]
        ↓ YES
Server returns 409 Conflict
        ↓
Client shows "Break session?" dialog
        ↓
User clicks "Yes"
        ↓
Client sends POST /api/auth/login with force=true
        ↓
Server invalidates old session
        ↓
Server creates new session
        ↓
Client stores tokens
        ↓
User logged in ✓
```

### Logout Flow - Current (BROKEN)
```
User clicks logout
        ↓
Client clears sessionStorage
        ↓
Client redirects to login
        ↓
[Server session still active!] ❌
        ↓
Next login shows false conflict ❌
```

---

## New Flow (FIXED)

### Login Flow - New (Simplified)
```
User enters credentials
        ↓
Client sends POST /api/auth/login
        ↓
Server checks credentials ✓
        ↓
Server checks for existing sessions
        ↓
    [Session exists?]
        ↓ YES
Server auto-invalidates old session
        ↓ (no user interaction needed)
Server creates new session
        ↓
Server logs activity: "Login"
        ↓
Client stores tokens
        ↓
User logged in ✓
```

### Logout Flow - New (Fixed)
```
User clicks logout
        ↓
Client sends POST /api/auth/logout
        ↓
Server invalidates session in DB
        ↓
Server logs activity: "Logout"
        ↓
Server returns success
        ↓
Client clears sessionStorage
        ↓
Client redirects to login
        ↓
[Server session properly closed] ✓
        ↓
Next login works smoothly ✓
```


## Admin Session Management Flow

### View Active Sessions
```
Admin opens Settings
        ↓
Admin clicks "Sesje użytkowników" tab
        ↓
Client sends GET /api/sessions/active
        ↓
Server queries UserSessions table
        ↓
Server calculates:
  - Session duration
  - Idle time
  - Active status
        ↓
Server returns session list
        ↓
Client displays table with:
  - User info
  - Duration
  - Last activity
  - IP address
  - Action buttons
```

### Terminate User Session
```
Admin clicks "Wyrzuć" button
        ↓
Client shows confirmation dialog
        ↓
Admin confirms
        ↓
Client sends POST /api/sessions/{id}/terminate
        ↓
Server invalidates session
        ↓
Server logs activity: "SessionTerminated"
        ↓
Server returns success
        ↓
Client refreshes session list
        ↓
[User's session is terminated]
        ↓
User's next API call gets 401
        ↓
User redirected to login
```

## Activity Logging Flow

### User Performs Action
```
User performs any action
(e.g., creates inspection)
        ↓
Controller method executes
        ↓
Business logic completes
        ↓
Controller calls UserActivityService.LogActivityAsync()
        ↓
Service creates UserActivityLog entry:
  - UserId
  - ActivityType (e.g., "Create")
  - EntityType (e.g., "Inspection")
  - EntityId
  - Description
  - IP Address
  - User Agent
  - Timestamp
        ↓
Service saves to database
        ↓
[Activity logged for audit]
```

### Admin Views Activity Log
```
Admin opens Settings
        ↓
Admin clicks "Dziennik aktywności" tab
        ↓
Admin selects filters:
  - User (optional)
  - Date from (optional)
  - Date to (optional)
        ↓
Admin clicks "Szukaj"
        ↓
Client sends GET /api/activitylogs/all
  with query parameters
        ↓
Server queries UserActivityLogs table
  with filters applied
        ↓
Server returns activity list
        ↓
Client displays table with:
  - Timestamp
  - User
  - Activity type (color-coded)
  - Entity
  - Description
  - IP address
```


## Data Flow Architecture

### Session Management Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                        CLIENT (Angular)                      │
├─────────────────────────────────────────────────────────────┤
│  LoginComponent          │  SettingsComponent                │
│  - login()               │  - loadActiveSessions()           │
│  - (no conflict dialog)  │  - terminateSession()             │
│                          │  - loadUserActivities()           │
├─────────────────────────────────────────────────────────────┤
│  AuthService                                                 │
│  - login()                                                   │
│  - logout() → calls server                                   │
│  - refreshToken()                                            │
└─────────────────────────────────────────────────────────────┘
                            ↕ HTTP/HTTPS
┌─────────────────────────────────────────────────────────────┐
│                      SERVER (.NET Core)                      │
├─────────────────────────────────────────────────────────────┤
│  Controllers                                                 │
│  ┌──────────────────┬──────────────────┬─────────────────┐  │
│  │ AuthController   │ SessionsController│ ActivityLogs    │  │
│  │ - Login          │ - GetActive      │ Controller      │  │
│  │ - Logout (NEW)   │ - Terminate      │ - GetUser       │  │
│  │ - Refresh        │ - TerminateAll   │ - GetAll        │  │
│  └──────────────────┴──────────────────┴─────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  Services                                                    │
│  ┌──────────────────────┬──────────────────────────────┐    │
│  │ SessionManagement    │ UserActivityService (NEW)    │    │
│  │ Service              │ - LogActivityAsync()         │    │
│  │ - CheckExisting      │ - GetUserActivities()        │    │
│  │ - CreateSession      │ - GetAllActivities()         │    │
│  │ - InvalidateSession  │                              │    │
│  │ - InvalidateAll      │                              │    │
│  └──────────────────────┴──────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│  Data Layer (Entity Framework Core)                         │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                    DATABASE (SQL Server)                     │
├─────────────────────────────────────────────────────────────┤
│  Tables:                                                     │
│  - Users                                                     │
│  - UserSessions                                              │
│  - UserActivityLogs (NEW)                                    │
│  - LoginAttempts                                             │
│  - SecurityEventLogs                                         │
│  - ChangeLogs                                                │
└─────────────────────────────────────────────────────────────┘
```

### Activity Logging Integration Points
```
┌─────────────────────────────────────────────────────────────┐
│                    All Controllers                           │
│  (Auth, Users, Inspections, Clients, CropSprayers, etc.)    │
└─────────────────────────────────────────────────────────────┘
                            ↓
              Every significant action triggers
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              UserActivityService.LogActivityAsync()          │
│                                                              │
│  Parameters:                                                 │
│  - userId: Who performed the action                         │
│  - activityType: What type (Login, Create, Update, etc.)    │
│  - entityType: What entity (User, Inspection, etc.)         │
│  - entityId: Which specific record                          │
│  - description: Human-readable description                  │
│  - ipAddress: Where from                                    │
│  - userAgent: What device/browser                           │
│  - metadata: Additional JSON data (optional)                │
└─────────────────────────────────────────────────────────────┘
                            ↓
                  Saved to database
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   UserActivityLogs Table                     │
│                                                              │
│  Queryable by:                                               │
│  - Admin (all logs)                                          │
│  - User (own logs only)                                      │
│                                                              │
│  Filterable by:                                              │
│  - User ID                                                   │
│  - Date range                                                │
│  - Activity type                                             │
│  - Entity type                                               │
└─────────────────────────────────────────────────────────────┘
```


## Security Model

### Authorization Matrix
```
┌──────────────────────┬─────────┬────────────┬──────────────┐
│ Endpoint             │ User    │ Admin      │ Master Admin │
├──────────────────────┼─────────┼────────────┼──────────────┤
│ POST /auth/login     │ ✓       │ ✓          │ ✓            │
│ POST /auth/logout    │ ✓       │ ✓          │ ✓            │
│ POST /auth/refresh   │ ✓       │ ✓          │ ✓            │
├──────────────────────┼─────────┼────────────┼──────────────┤
│ GET /sessions/active │ ✗       │ ✓          │ ✓            │
│ POST /sessions/{id}  │ ✗       │ ✓          │ ✓            │
│   /terminate         │         │            │              │
│ POST /sessions/user/ │ ✗       │ ✓          │ ✓            │
│   {id}/terminate-all │         │            │              │
├──────────────────────┼─────────┼────────────┼──────────────┤
│ GET /activitylogs/   │ ✓ (own) │ ✓ (all)    │ ✓ (all)      │
│   user/{id}          │         │            │              │
│ GET /activitylogs/   │ ✗       │ ✓          │ ✓            │
│   all                │         │            │              │
└──────────────────────┴─────────┴────────────┴──────────────┘
```

### Session Validation Flow
```
Every API Request
        ↓
JWT Bearer Token in Authorization header
        ↓
JwtBearer Middleware validates token:
  - Signature valid?
  - Not expired?
  - Issuer/Audience correct?
        ↓ YES
OnTokenValidated event fires
        ↓
SessionManagementService.IsSessionValidAsync()
        ↓
Checks UserSessions table:
  - IsActive = true?
  - InvalidatedAt = null?
  - RefreshTokenExpiresAt > now?
  - LastActivityAt + timeout > now?
        ↓ YES
Updates LastActivityAt to now
        ↓
Request proceeds to controller
        ↓
Controller executes
        ↓
UserActivityService logs action
        ↓
Response returned to client

[If any validation fails → 401 Unauthorized]
```

### One-User-One-Session Enforcement
```
User attempts login
        ↓
Credentials validated ✓
        ↓
SessionManagementService.CheckExistingSessionsAsync(userId)
        ↓
Query: SELECT * FROM UserSessions 
       WHERE UserId = @userId 
       AND IsActive = true
       AND InvalidatedAt IS NULL
       AND RefreshTokenExpiresAt > NOW()
        ↓
    [Active session found?]
        ↓ YES
SessionManagementService.InvalidateAllUserSessionsAsync(userId)
        ↓
UPDATE UserSessions 
SET IsActive = false,
    InvalidatedAt = NOW(),
    InvalidationReason = 'New login'
WHERE UserId = @userId
        ↓
Log activity: "Previous session terminated due to new login"
        ↓
Create new session
        ↓
User logged in with single active session ✓
```

---

## Summary

### Key Improvements
1. ✅ **Automatic session cleanup** on logout
2. ✅ **One session per user** enforced automatically
3. ✅ **No user confusion** with conflict dialogs
4. ✅ **Full admin visibility** into active sessions
5. ✅ **Complete audit trail** of all user actions
6. ✅ **Remote session management** by admins
7. ✅ **Compliance-ready** logging system

### Technical Benefits
- Cleaner code architecture
- Better separation of concerns
- Centralized activity logging
- Improved security posture
- Enhanced troubleshooting capabilities
- Regulatory compliance support
