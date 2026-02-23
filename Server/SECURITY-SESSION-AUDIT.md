## Unified security, session, and audit design

This document describes how **authentication, session management, and auditing** are unified in the backend to provide a state-of-the-art ERP-grade security model:

- Every request is tied to a **user session**.
- Every data modification is recorded in a **generic audit trail**.
- Security events (login, lockout, authorization failures, sensitive data access) are recorded for **forensic analysis**.

It builds on the existing `SessionManagementService`, `SecurityAuditService`, and `AppDbContext` change logging.

---

## 1. Authentication and sessions

### 1.1 Authentication flow

- **Login**:
  - `AuthController` validates credentials and uses:
    - `TokenService` to generate **access** and **refresh** JWT tokens.
    - `SessionManagementService` to **create a UserSession** record with:
      - `UserId`, `SessionToken` (access token), `RefreshToken`, `RefreshTokenExpiresAt`,
      - `CreatedAt`, `LastActivityAt`, `IsActive`,
      - `IpAddress`, `UserAgent`, and `SessionTimeoutMinutes`.
    - `SecurityAuditService` to log:
      - Login attempts (success/failure, IP, User-Agent).
      - Suspicious patterns (rapid attempts, non-existent user logins, multiple failures from same IP).

- **Access tokens**:
  - Configured via `AddAuthentication().AddJwtBearer(...)` in `Program.cs`:
    - Validates issuer, audience, signing key (256-bit minimum) and lifetime.
  - `OnTokenValidated`:
    - Looks up `UserSessions` by `SessionToken`.
    - Ensures `IsActive` is true.
    - Updates `LastActivityAt` to `DateTime.UtcNow`.
    - Fails validation if session is not active, causing a `401 Unauthorized`.

- **Refresh tokens**:
  - Managed by `AuthController` and `SessionManagementService`:
    - `RefreshToken` and `RefreshTokenExpiresAt` are stored per session.
    - On refresh, tokens are rotated; old tokens can be invalidated if needed.

### 1.2 Central session logic (`SessionManagementService`)

`SessionManagementService` is the **single source of truth** for session validity, encapsulating all rules:

- A session is **active** only if:
  - `IsActive` is true.
  - `InvalidatedAt` is `null`.
  - `RefreshTokenExpiresAt` is `null` or in the future.
  - `LastActivityAt + SessionTimeoutMinutes` is in the future.

- Key methods:
  - `CheckExistingSessionsAsync(userId)` – used before login to detect conflicts and decide if a “force login” prompt is needed.
  - `CreateSessionAsync(userId, accessToken, refreshToken, ip, userAgent)` – creates a new active session.
  - `InvalidateSessionAsync(sessionToken)` – logout for one session.
  - `InvalidateAllUserSessionsAsync(userId)` – force logout for all sessions.
  - `CleanupExpiredSessionsAsync()` – background cleanup of expired/idle sessions.
  - `IsSessionValidAsync(sessionToken)` – validates a session by token.

**JwtBearer integration (target model)**:

- Replace any ad-hoc helpers (like `IsSessionActiveAsync` in `Program.cs`) with calls to `SessionManagementService`:
  - In `OnTokenValidated`, use `IsSessionValidAsync(sessionToken)` instead of querying `UserSessions` directly.
  - Controllers/endpoints do **not** touch `UserSessions` directly; they trust the authentication layer.

This keeps session semantics centralized and consistent.

---

## 2. Authorization and security events

### 2.1 Authorization

- Controllers use `[Authorize]` and role-based attributes:
  - Example: `[Authorize(Roles = "admin")]` for admin-only operations.
- `AuthorizationService` adds **fine-grained checks**:
  - `IsAdminAsync`, `CanModifyUserAsync`, `CanDeleteUserAsync`, `CanAccessResourceAsync`, `ValidateRoleChange`.
  - Logs failures via `SecurityAuditService.LogAuthorizationFailureAsync`.

### 2.2 Security events (`SecurityAuditService`)

`SecurityAuditService` is the **logging hub** for security-related activity:

- **Login attempts**:
  - `LogLoginAttemptAsync` writes to `LoginAttempts` table:
    - `Login`, `IpAddress`, `UserAgent`, `Success`, `FailureReason`, `AttemptedAt`.
  - Used by:
    - `GetRecentFailedAttemptsCountAsync`.
    - `IsLockedOutAsync`, `RecordFailedLoginAsync`, `ResetFailedAttemptsAsync`.

- **Account lockout & delays**:
  - Configurable via `AccountLockout:*` and `ProgressiveDelay:*` settings.
  - Progressive delay (`CalculateProgressiveDelay` + `ApplyProgressiveDelayAsync`) enforces exponential waiting times after repeated failures.

- **Suspicious activity**:
  - `DetectAndLogSuspiciousActivityAsync`:
    - Non-existent user login attempts.
    - Rapid login attempts from same IP.
    - Multiple failed attempts from same IP.
  - Writes to `SecurityEventLogs` with structured metadata.

- **Sensitive data access & authorization failures**:
  - `LogDataAccessAsync(userId, entityType, entityId, action)` logs reads/updates of sensitive entities.
  - `LogAuthorizationFailureAsync(userId, resource, action)` logs denied access attempts.

All security event logs include timestamps and enough metadata to reconstruct **who did what, from where, and when**.

---

## 3. Generic audit trail for data changes

### 3.1 Change logging in `AppDbContext`

`AppDbContext` implements a **generic change logger** using EF Core’s change tracker and the `ChangeLogs` table.

- On `SaveChangesAsync`:

  1. `OnBeforeSaveChanges()`:
     - Iterates all tracked entities (except `ChangeLog` itself).
     - Builds an `AuditEntry` per entity:
       - `TableName` – the entity type name (e.g. `CropSprayer`, `Client`, `InspectionProtocol`).
       - `UserId` – the current user login, from `HttpContext.User` (or `"Unknown"` if not authenticated).
       - `KeyValues` – primary key values.
       - `Changes` – list of human-readable changes.
     - For **Added** entities:
       - Adds entries like `'<Property>': <CurrentValue>`.
     - For **Modified** entities:
       - Adds entries like:
         - `'<Property>': <OriginalValue> -> <CurrentValue>` for each changed property.
     - For **Deleted** entities:
       - Records only that the entity was deleted (no field dump).

  2. `OnAfterSaveChanges(auditEntries)`:
     - Resolves temporary properties (e.g. database-generated keys).
     - Converts each `AuditEntry` into a `ChangeLog`:
       - `EntityName` – table/entity name (e.g. `CropSprayer`).
       - `EntityId` – concatenated primary key values.
       - `Who` – current user.
       - `When` – `DateTime.UtcNow`.
       - `Changes` – string like:
         - `"Utworzono: 'FieldA': 10, 'FieldB': 'foo'"` or
         - `"Zmodyfikowano: 'BoomWidth': 21 -> 24, 'TankCapacity': 1000 -> 1200"`.

This delivers exactly what is needed for an ERP-grade audit trail:

- **Who**: `ChangeLog.Who`.
- **When**: `ChangeLog.When`.
- **What record**: `ChangeLog.EntityName` + `ChangeLog.EntityId`.
- **What changed**:
  - For each property: `original -> new` representation.

Example: if someone changes the **boom width** of a crop sprayer:

- A `ChangeLog` entry for `EntityName = "CropSprayer"` is created with:
  - `EntityId` = serial number (primary key).
  - `Changes` including:
    - `"'BoomWidth': 21 -> 24"`.

### 3.2 History API for all entities

- Minimal API in `Program.cs` (`/api/History/{entityName}/{entityId}`) and `HistoryController`:
  - Query `ChangeLogs` filtered by `EntityName` + `EntityId`.
  - Join with `Users` to enrich with:
    - `UserFirstName`, `UserLastName`, `UserAvatarData`.
  - Order descending by `When`.

This gives a **generic history endpoint** for any entity:

- To fetch the full change trail of a Crop Sprayer, Clients, Inspection Protocols, etc., the frontend just calls:
  - `/api/history/CropSprayer/{serialNumber}`
  - or `/api/history/Client/{clientId}`, etc.

In the layered architecture, this logic moves into:

- `Application/History/HistoryService` and `Infrastructure/Persistence/Repositories/HistoryRepository`.
- `Api/Endpoints/HistoryEndpoints` or `HistoryController`.

---

## 4. Logging interfaces and layering

For a cleaner architecture and easier testing, the following abstractions can be introduced:

- **Session abstraction**
  - `ISessionManagementService` (already defined) used by:
    - `AuthController` / auth endpoints.
    - JwtBearer `OnTokenValidated` handler.
    - Background cleanup (`SessionCleanupService`).

- **Audit abstractions**
  - `ISecurityAuditService` (already defined) used by:
    - Auth flows (logins, lockouts, suspicious activity).
    - Authorization logic (`AuthorizationService`).
    - Sensitive data access points (user details, client PII, etc.).
  - `IAuditLogger` (optional future interface) could wrap `ChangeLogs` persistence if needed beyond `AppDbContext`.

In the target structure:

- **Domain layer**:
  - Defines `UserSession`, `ChangeLog`, `SecurityEventLog`, etc., as pure entities.

- **Infrastructure layer**:
  - Implements `ISessionManagementService`, `ISecurityAuditService`, and EF-based auditing in `AppDbContext`.

- **Application layer**:
  - Uses the services only via their interfaces, never directly touching `UserSessions` or log tables.

- **Api layer**:
  - Uses `[Authorize]`, `SessionManagementService`, and `SecurityAuditService` to enforce and surface security behavior.

---

## 5. Summary

With the existing building blocks and the layering described above, the backend already provides and can further evolve into:

- **ERP-grade audit trail**:
  - Every change to any tracked entity records who changed it, when, and from which value to which.
  - A generic history API to retrieve this trail per record.

- **Robust session and security model**:
  - Per-session JWT validation with activity tracking and timeouts.
  - Account lockout, progressive delays, and suspicious-activity detection.
  - Structured logging of logins, authorization failures, and sensitive data access.

The remaining work is mainly **restructuring** into the proposed layers and ensuring all new endpoints and features consistently use these centralized services instead of duplicating logic.

