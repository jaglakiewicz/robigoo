# Session Management & User Activity Logging - Implementation Plan

## Executive Summary

This document outlines the implementation plan to fix session management issues and add comprehensive user activity logging with admin monitoring capabilities.

## Current Issues Identified

### 1. Session Management Problems
- **Issue**: Users are asked to break existing sessions even after intentional logout
- **Root Cause**: Session invalidation on logout is not properly clearing the session state
- **Impact**: Poor user experience, confusion about session state

### 2. Missing Features
- No admin interface to view active user sessions
- No ability for admins to forcefully disconnect users
- No session duration tracking visible to admins
- No comprehensive user activity logging beyond security events
- No admin interface to view user action history

## Implementation Plan

### Phase 1: Fix Session Management (Priority: CRITICAL)

#### 1.1 Fix Logout Session Invalidation
**Problem**: Client-side logout doesn't call server-side session invalidation

**Files to modify**:
- `Client/src/app/services/auth.service.ts`
- `Server/Controllers/AuthController.cs`

**Changes**:
1. Add server-side logout endpoint in `AuthController.cs`
2. Modify client `logout()` method to call server endpoint before clearing local storage
3. Ensure `SessionManagementService.InvalidateSessionAsync()` is called on logout

**Implementation**:
```csharp
// Server/Controllers/AuthController.cs
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout()
{
    var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    await _sessionManagementService.InvalidateSessionAsync(token);
    return Ok(new { message = "Logged out successfully" });
}
```


```typescript
// Client/src/app/services/auth.service.ts
logout(): Observable<void> {
  const token = this.getToken();
  
  if (token) {
    return this.http.post<void>(`${this.apiUrl}/logout`, {}).pipe(
      tap(() => this.clearLocalSession()),
      catchError(() => {
        // Even if server call fails, clear local session
        this.clearLocalSession();
        return of(void 0);
      })
    );
  } else {
    this.clearLocalSession();
    return of(void 0);
  }
}

private clearLocalSession(): void {
  this.stopRefreshTokenTimer();
  sessionStorage.removeItem('currentUser');
  sessionStorage.removeItem('token');
  sessionStorage.removeItem('refreshToken');
  sessionStorage.removeItem('tokenExpiresAt');
  this.currentUserSubject.next(null);
}
```

#### 1.2 Enforce One User - One Session Policy
**Current**: Multiple sessions can exist, force login required
**Target**: Automatically invalidate old sessions on new login

**Files to modify**:
- `Server/Controllers/AuthController.cs`

**Changes**:
- Remove the 409 Conflict response for existing sessions
- Always invalidate existing sessions before creating new one
- Remove `force` parameter requirement

**Implementation**:
```csharp
// In Login method, replace session check logic:
var sessionCheck = await _sessionManagementService.CheckExistingSessionsAsync(user.Id);

// Always invalidate existing sessions - one user, one session policy
if (sessionCheck.HasActiveSession)
{
    await _sessionManagementService.InvalidateAllUserSessionsAsync(user.Id);
    _logger.LogInformation(
        "Invalidated existing session for user {UserId} due to new login",
        user.Id);
}
```


### Phase 2: Add User Activity Logging (Priority: HIGH)

#### 2.1 Create User Activity Log Model
**Purpose**: Track all user actions for audit trail

**New file**: `Server/Models/UserActivityLog.cs`

**Implementation**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public enum ActivityType
    {
        Login,
        Logout,
        Create,
        Update,
        Delete,
        View,
        Export,
        Import,
        PasswordChange,
        SettingsChange,
        FileUpload,
        FileDownload,
        SessionTerminated
    }

    public class UserActivityLog
    {
        public long Id { get; set; }
        
        public long UserId { get; set; }
        
        [MaxLength(100)]
        public string UserLogin { get; set; } = string.Empty;
        
        public ActivityType ActivityType { get; set; }
        
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? EntityId { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public User User { get; set; } = null!;
    }
}
```


#### 2.2 Create User Activity Service
**Purpose**: Centralized service for logging user activities

**New file**: `Server/Services/UserActivityService.cs`

**Implementation**:
```csharp
using Server.Data;
using Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Server.Services
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(long userId, ActivityType activityType, string entityType, 
            string? entityId, string description, string? ipAddress, string? userAgent, 
            string? metadata = null);
        
        Task<List<UserActivityLog>> GetUserActivitiesAsync(long userId, int limit = 100);
        
        Task<List<UserActivityLog>> GetAllActivitiesAsync(DateTime? from = null, 
            DateTime? to = null, int limit = 1000);
    }

    public class UserActivityService : IUserActivityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserActivityService> _logger;

        public UserActivityService(AppDbContext context, ILogger<UserActivityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActivityAsync(long userId, ActivityType activityType, 
            string entityType, string? entityId, string description, string? ipAddress, 
            string? userAgent, string? metadata = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot log activity for non-existent user {UserId}", userId);
                    return;
                }

                var activity = new UserActivityLog
                {
                    UserId = userId,
                    UserLogin = user.Login,
                    ActivityType = activityType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    IpAddress = ipAddress?.Length > 50 ? ipAddress.Substring(0, 50) : ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                    Metadata = metadata?.Length > 2000 ? metadata.Substring(0, 2000) : metadata,
                    Timestamp = DateTime.UtcNow
                };

                _context.UserActivityLogs.Add(activity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user activity for user {UserId}", userId);
            }
        }

        public async Task<List<UserActivityLog>> GetUserActivitiesAsync(long userId, int limit = 100)
        {
            return await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<UserActivityLog>> GetAllActivitiesAsync(DateTime? from = null, 
            DateTime? to = null, int limit = 1000)
        {
            var query = _context.UserActivityLogs.AsQueryable();

            if (from.HasValue)
                query = query.Where(a => a.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.Timestamp <= to.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}
```


#### 2.3 Update Database Context
**File**: `Server/Data/AppDbContext.cs`

**Changes**:
```csharp
public DbSet<UserActivityLog> UserActivityLogs { get; set; }

// In OnModelCreating:
modelBuilder.Entity<UserActivityLog>()
    .HasIndex(e => e.UserId);

modelBuilder.Entity<UserActivityLog>()
    .HasIndex(e => e.Timestamp);

modelBuilder.Entity<UserActivityLog>()
    .HasIndex(e => new { e.UserId, e.Timestamp });
```

#### 2.4 Integrate Activity Logging
**Files to modify**:
- `Server/Controllers/AuthController.cs` - Log login/logout
- `Server/Controllers/UsersController.cs` - Log user CRUD operations
- `Server/Controllers/InspectionsController.cs` - Log inspection operations
- `Server/Controllers/ClientsController.cs` - Log client operations
- `Server/Controllers/CropSprayersController.cs` - Log machine operations

**Example Integration**:
```csharp
// In AuthController.Login (after successful login):
await _userActivityService.LogActivityAsync(
    user.Id,
    ActivityType.Login,
    "Auth",
    null,
    $"User logged in from {ipAddress}",
    ipAddress,
    userAgent
);

// In AuthController.Logout:
await _userActivityService.LogActivityAsync(
    userId,
    ActivityType.Logout,
    "Auth",
    null,
    "User logged out",
    ipAddress,
    userAgent
);

// In UsersController.CreateUser:
await _userActivityService.LogActivityAsync(
    currentUserId,
    ActivityType.Create,
    "User",
    newUser.Id.ToString(),
    $"Created user {newUser.Login}",
    ipAddress,
    userAgent
);
```


### Phase 3: Admin Session Management Interface (Priority: HIGH)

#### 3.1 Add Session Management Endpoints
**File**: `Server/Controllers/SessionsController.cs` (NEW)

**Implementation**:
```csharp
using Server.Data;
using Server.Models;
using Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class SessionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly IUserActivityService _userActivityService;
        private readonly ILogger<SessionsController> _logger;

        public SessionsController(
            AppDbContext context,
            ISessionManagementService sessionManagementService,
            IUserActivityService userActivityService,
            ILogger<SessionsController> logger)
        {
            _context = context;
            _sessionManagementService = sessionManagementService;
            _userActivityService = userActivityService;
            _logger = logger;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var now = DateTime.UtcNow;
            
            var sessions = await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.IsActive && s.InvalidatedAt == null)
                .OrderByDescending(s => s.LastActivityAt)
                .Select(s => new
                {
                    s.Id,
                    s.UserId,
                    UserLogin = s.User.Login,
                    UserFullName = s.User.FirstName + " " + s.User.LastName,
                    s.CreatedAt,
                    s.LastActivityAt,
                    SessionDuration = EF.Functions.DateDiffMinute(s.CreatedAt, now),
                    IdleTime = EF.Functions.DateDiffMinute(s.LastActivityAt, now),
                    s.IpAddress,
                    s.UserAgent
                })
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpPost("{sessionId}/terminate")]
        public async Task<IActionResult> TerminateSession(long sessionId)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound(new { message = "Session not found" });

            await _sessionManagementService.InvalidateSessionAsync(session.SessionToken);

            var currentUserId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            await _userActivityService.LogActivityAsync(
                currentUserId,
                ActivityType.SessionTerminated,
                "Session",
                sessionId.ToString(),
                $"Admin terminated session for user {session.User.Login}",
                ipAddress,
                userAgent,
                $"{{\"targetUserId\":{session.UserId},\"targetUserLogin\":\"{session.User.Login}\"}}"
            );

            return Ok(new { message = "Session terminated successfully" });
        }

        [HttpPost("user/{userId}/terminate-all")]
        public async Task<IActionResult> TerminateAllUserSessions(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            await _sessionManagementService.InvalidateAllUserSessionsAsync(userId);

            var currentUserId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            await _userActivityService.LogActivityAsync(
                currentUserId,
                ActivityType.SessionTerminated,
                "Session",
                userId.ToString(),
                $"Admin terminated all sessions for user {user.Login}",
                ipAddress,
                userAgent,
                $"{{\"targetUserId\":{userId},\"targetUserLogin\":\"{user.Login}\"}}"
            );

            return Ok(new { message = "All user sessions terminated successfully" });
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) 
                ? userId : 0;
        }
    }
}
```


#### 3.2 Add Activity Log Endpoints
**File**: `Server/Controllers/ActivityLogsController.cs` (NEW)

**Implementation**:
```csharp
using Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IUserActivityService _userActivityService;
        private readonly IAuthorizationService _authorizationService;

        public ActivityLogsController(
            IUserActivityService userActivityService,
            IAuthorizationService authorizationService)
        {
            _userActivityService = userActivityService;
            _authorizationService = authorizationService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserActivities(long userId, [FromQuery] int limit = 100)
        {
            var currentUserId = GetCurrentUserId();
            
            // Users can view their own logs, admins can view any user's logs
            if (currentUserId != userId && !await _authorizationService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var activities = await _userActivityService.GetUserActivitiesAsync(userId, limit);
            return Ok(activities);
        }

        [HttpGet("all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllActivities(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int limit = 1000)
        {
            var activities = await _userActivityService.GetAllActivitiesAsync(from, to, limit);
            return Ok(activities);
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) 
                ? userId : 0;
        }
    }
}
```


#### 3.3 Update Settings Component - Add Session Management Tab
**File**: `Client/src/app/settings/settings.component.ts`

**Changes**:
1. Add new step for session management
2. Add properties for session data
3. Add methods to load and manage sessions

**Implementation**:
```typescript
// Add to steps array:
steps: Step[] = [
  // ... existing steps
  { id: 3, key: 'sessions', label: 'Sesje użytkowników' },
  { id: 4, key: 'activity', label: 'Dziennik aktywności' }
];

// Add properties:
activeSessions: any[] = [];
loadingSessions = false;
terminatingSessionId: number | null = null;

userActivities: any[] = [];
loadingActivities = false;
selectedUserId: number | null = null;
activityDateFrom: string = '';
activityDateTo: string = '';

// Add methods:
loadActiveSessions(): void {
  this.loadingSessions = true;
  this.http.get<any[]>('/api/sessions/active').subscribe({
    next: (sessions) => {
      this.activeSessions = sessions;
      this.loadingSessions = false;
    },
    error: (error) => {
      console.error('Failed to load sessions:', error);
      this.notificationService.showError('Nie udało się załadować sesji');
      this.loadingSessions = false;
    }
  });
}

terminateSession(sessionId: number): void {
  if (!confirm('Czy na pewno chcesz zakończyć tę sesję?')) {
    return;
  }

  this.terminatingSessionId = sessionId;
  this.http.post(`/api/sessions/${sessionId}/terminate`, {}).subscribe({
    next: () => {
      this.notificationService.showSuccess('Sesja została zakończona');
      this.loadActiveSessions();
      this.terminatingSessionId = null;
    },
    error: (error) => {
      console.error('Failed to terminate session:', error);
      this.notificationService.showError('Nie udało się zakończyć sesji');
      this.terminatingSessionId = null;
    }
  });
}

terminateAllUserSessions(userId: number, userLogin: string): void {
  if (!confirm(`Czy na pewno chcesz zakończyć wszystkie sesje użytkownika ${userLogin}?`)) {
    return;
  }

  this.http.post(`/api/sessions/user/${userId}/terminate-all`, {}).subscribe({
    next: () => {
      this.notificationService.showSuccess('Wszystkie sesje użytkownika zostały zakończone');
      this.loadActiveSessions();
    },
    error: (error) => {
      console.error('Failed to terminate user sessions:', error);
      this.notificationService.showError('Nie udało się zakończyć sesji użytkownika');
    }
  });
}

loadUserActivities(userId?: number): void {
  this.loadingActivities = true;
  
  let url = '/api/activitylogs/all';
  const params: any = { limit: 500 };
  
  if (userId) {
    url = `/api/activitylogs/user/${userId}`;
  }
  
  if (this.activityDateFrom) {
    params.from = this.activityDateFrom;
  }
  
  if (this.activityDateTo) {
    params.to = this.activityDateTo;
  }

  this.http.get<any[]>(url, { params }).subscribe({
    next: (activities) => {
      this.userActivities = activities;
      this.loadingActivities = false;
    },
    error: (error) => {
      console.error('Failed to load activities:', error);
      this.notificationService.showError('Nie udało się załadować dziennika aktywności');
      this.loadingActivities = false;
    }
  });
}

formatDuration(minutes: number): string {
  if (minutes < 60) {
    return `${minutes} min`;
  }
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${hours}h ${mins}min`;
}

getActivityTypeLabel(type: string): string {
  const labels: any = {
    'Login': 'Logowanie',
    'Logout': 'Wylogowanie',
    'Create': 'Utworzenie',
    'Update': 'Aktualizacja',
    'Delete': 'Usunięcie',
    'View': 'Podgląd',
    'Export': 'Eksport',
    'Import': 'Import',
    'PasswordChange': 'Zmiana hasła',
    'SettingsChange': 'Zmiana ustawień',
    'FileUpload': 'Przesłanie pliku',
    'FileDownload': 'Pobranie pliku',
    'SessionTerminated': 'Zakończenie sesji'
  };
  return labels[type] || type;
}
```


#### 3.4 Update Settings Component HTML - Add UI for Sessions and Activity
**File**: `Client/src/app/settings/settings.component.html`

**Add after the admin section**:
```html
<!-- ── SESSION MANAGEMENT ── -->
<section id="section-3" class="settings-section" *ngIf="isCurrentUserAdmin()">
  <div class="form-section-divider"><span class="form-section-label">Sesje użytkowników</span></div>

  <div class="section-actions">
    <button class="btn btn-secondary" (click)="loadActiveSessions()" [disabled]="loadingSessions">
      <span [innerHTML]="getSafeHtml(SVG_ICONS.iconRefresh)"></span>
      {{ loadingSessions ? 'Ładowanie...' : 'Odśwież' }}
    </button>
  </div>

  <div *ngIf="loadingSessions" class="loading-message">Ładowanie sesji...</div>

  <div *ngIf="!loadingSessions && activeSessions.length === 0" class="empty-message">
    Brak aktywnych sesji
  </div>

  <table *ngIf="!loadingSessions && activeSessions.length > 0" class="data-table">
    <thead>
      <tr>
        <th>Użytkownik</th>
        <th>Login</th>
        <th>Czas trwania</th>
        <th>Ostatnia aktywność</th>
        <th>Czas bezczynności</th>
        <th>Adres IP</th>
        <th>Akcje</th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let session of activeSessions">
        <td>{{ session.userFullName }}</td>
        <td>{{ session.userLogin }}</td>
        <td>{{ formatDuration(session.sessionDuration) }}</td>
        <td>{{ session.lastActivityAt | date:'dd.MM.yyyy HH:mm:ss' }}</td>
        <td>{{ formatDuration(session.idleTime) }}</td>
        <td>{{ session.ipAddress || 'N/A' }}</td>
        <td>
          <button 
            class="btn btn-small btn-danger" 
            (click)="terminateSession(session.id)"
            [disabled]="terminatingSessionId === session.id"
            title="Zakończ sesję">
            {{ terminatingSessionId === session.id ? 'Kończenie...' : 'Wyrzuć' }}
          </button>
          <button 
            class="btn btn-small btn-warning" 
            (click)="terminateAllUserSessions(session.userId, session.userLogin)"
            title="Zakończ wszystkie sesje użytkownika"
            style="margin-left: 8px;">
            Wyrzuć wszystkie
          </button>
        </td>
      </tr>
    </tbody>
  </table>
</section>

<!-- ── ACTIVITY LOG ── -->
<section id="section-4" class="settings-section" *ngIf="isCurrentUserAdmin()">
  <div class="form-section-divider"><span class="form-section-label">Dziennik aktywności</span></div>

  <div class="form-row form-row-3">
    <div class="form-group">
      <label>Użytkownik</label>
      <select [(ngModel)]="selectedUserId" (change)="loadUserActivities(selectedUserId || undefined)">
        <option [ngValue]="null">Wszyscy użytkownicy</option>
        <option *ngFor="let user of users" [ngValue]="user.id">
          {{ user.firstName }} {{ user.lastName }} ({{ user.login }})
        </option>
      </select>
    </div>
    <div class="form-group">
      <label>Data od</label>
      <input type="datetime-local" [(ngModel)]="activityDateFrom" />
    </div>
    <div class="form-group">
      <label>Data do</label>
      <input type="datetime-local" [(ngModel)]="activityDateTo" />
    </div>
  </div>

  <div class="section-actions">
    <button class="btn btn-primary" (click)="loadUserActivities(selectedUserId || undefined)" [disabled]="loadingActivities">
      <span [innerHTML]="getSafeHtml(SVG_ICONS.iconSearch)"></span>
      {{ loadingActivities ? 'Ładowanie...' : 'Szukaj' }}
    </button>
  </div>

  <div *ngIf="loadingActivities" class="loading-message">Ładowanie dziennika...</div>

  <div *ngIf="!loadingActivities && userActivities.length === 0" class="empty-message">
    Brak aktywności do wyświetlenia
  </div>

  <table *ngIf="!loadingActivities && userActivities.length > 0" class="data-table">
    <thead>
      <tr>
        <th>Data i czas</th>
        <th>Użytkownik</th>
        <th>Typ aktywności</th>
        <th>Encja</th>
        <th>Opis</th>
        <th>Adres IP</th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let activity of userActivities">
        <td>{{ activity.timestamp | date:'dd.MM.yyyy HH:mm:ss' }}</td>
        <td>{{ activity.userLogin }}</td>
        <td>
          <span class="activity-badge" [class]="'activity-' + activity.activityType.toLowerCase()">
            {{ getActivityTypeLabel(activity.activityType) }}
          </span>
        </td>
        <td>{{ activity.entityType }}</td>
        <td>{{ activity.description }}</td>
        <td>{{ activity.ipAddress || 'N/A' }}</td>
      </tr>
    </tbody>
  </table>
</section>
```


#### 3.5 Add CSS Styles for New UI Elements
**File**: `Client/src/app/settings/settings.component.css`

**Add**:
```css
.data-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: var(--space-md);
  background: var(--card-bg);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.data-table thead {
  background: var(--primary-color);
  color: white;
}

.data-table th,
.data-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid var(--border-color);
}

.data-table tbody tr:hover {
  background: var(--hover-bg);
}

.data-table tbody tr:last-child td {
  border-bottom: none;
}

.section-actions {
  margin: var(--space-md) 0;
  display: flex;
  gap: var(--space-sm);
}

.loading-message,
.empty-message {
  padding: var(--space-lg);
  text-align: center;
  color: var(--text-secondary);
  font-style: italic;
}

.activity-badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.85em;
  font-weight: 500;
}

.activity-login {
  background: #e3f2fd;
  color: #1976d2;
}

.activity-logout {
  background: #fce4ec;
  color: #c2185b;
}

.activity-create {
  background: #e8f5e9;
  color: #388e3c;
}

.activity-update {
  background: #fff3e0;
  color: #f57c00;
}

.activity-delete {
  background: #ffebee;
  color: #d32f2f;
}

.activity-view {
  background: #f3e5f5;
  color: #7b1fa2;
}

.activity-export,
.activity-import {
  background: #e0f2f1;
  color: #00796b;
}

.activity-passwordchange,
.activity-settingschange {
  background: #fff9c4;
  color: #f57f17;
}

.activity-sessionterminated {
  background: #ffccbc;
  color: #e64a19;
}

.btn-small {
  padding: 6px 12px;
  font-size: 0.9em;
}
```


### Phase 4: Database Migration (Priority: CRITICAL)

#### 4.1 Create Migration for UserActivityLog Table

**Command to run**:
```bash
cd Server
dotnet ef migrations add AddUserActivityLogging
dotnet ef database update
```

**Expected migration content**:
```csharp
public partial class AddUserActivityLogging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserActivityLogs",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                UserLogin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ActivityType = table.Column<int>(type: "int", nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserActivityLogs_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserActivityLogs_UserId",
            table: "UserActivityLogs",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserActivityLogs_Timestamp",
            table: "UserActivityLogs",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_UserActivityLogs_UserId_Timestamp",
            table: "UserActivityLogs",
            columns: new[] { "UserId", "Timestamp" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserActivityLogs");
    }
}
```


### Phase 5: Service Registration (Priority: CRITICAL)

#### 5.1 Register Services in Program.cs
**File**: `Server/Program.cs`

**Add service registrations**:
```csharp
// Add after existing service registrations
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
```

### Phase 6: Update Client Components (Priority: HIGH)

#### 6.1 Update App Component to Call Server Logout
**File**: `Client/src/app/app.component.ts`

**Change**:
```typescript
onLogout() {
  this.authService.logout().subscribe({
    next: () => {
      // Logout successful, redirect handled by service
    },
    error: (error) => {
      console.error('Logout error:', error);
      // Even on error, we should redirect to login
      window.location.href = '/login';
    }
  });
}
```

#### 6.2 Remove Session Conflict Dialog from Login
**File**: `Client/src/app/login/login.component.ts`

**Remove**:
- `sessionConflictVisible` property
- `confirmSessionTakeover()` method
- Session conflict handling in `onLogin()` method

**Simplify login**:
```typescript
onLogin() {
  this.errorKey = '';
  this.loading = true;

  this.authService.login(this.username, this.password).subscribe(
    () => {
      this.loading = false;
      this.loginSuccess.emit();
    },
    (error) => {
      this.errorKey = 'login.errors.invalidCredentials';
      this.loading = false;
    }
  );
}
```

**File**: `Client/src/app/login/login.component.html`

**Remove**: Session conflict dialog HTML


## Implementation Checklist

### Phase 1: Fix Session Management ✓
- [ ] Add logout endpoint in `AuthController.cs`
- [ ] Update `auth.service.ts` to call server logout
- [ ] Remove session conflict dialog from login component
- [ ] Enforce one-user-one-session policy in login
- [ ] Test: Login, logout, login again - should work without conflict
- [ ] Test: Login from two browsers - second login should invalidate first

### Phase 2: Add User Activity Logging ✓
- [ ] Create `UserActivityLog.cs` model
- [ ] Create `UserActivityService.cs` service
- [ ] Update `AppDbContext.cs` with UserActivityLogs DbSet
- [ ] Create database migration
- [ ] Run migration
- [ ] Integrate logging in `AuthController` (login/logout)
- [ ] Integrate logging in `UsersController` (CRUD operations)
- [ ] Integrate logging in other controllers as needed
- [ ] Test: Perform actions and verify logs are created

### Phase 3: Admin Session Management Interface ✓
- [ ] Create `SessionsController.cs`
- [ ] Create `ActivityLogsController.cs`
- [ ] Update `settings.component.ts` with session management methods
- [ ] Update `settings.component.html` with session management UI
- [ ] Update `settings.component.html` with activity log UI
- [ ] Add CSS styles for new UI elements
- [ ] Test: View active sessions as admin
- [ ] Test: Terminate a session and verify user is logged out
- [ ] Test: View activity logs with filters

### Phase 4: Service Registration ✓
- [ ] Register `IUserActivityService` in `Program.cs`
- [ ] Verify all dependencies are properly injected

### Phase 5: Testing & Validation ✓
- [ ] Test login/logout flow
- [ ] Test one-user-one-session enforcement
- [ ] Test session termination by admin
- [ ] Test activity logging for all major operations
- [ ] Test activity log viewing with filters
- [ ] Test session duration and idle time calculations
- [ ] Performance test with multiple concurrent sessions
- [ ] Security test: Verify non-admin cannot access admin endpoints


## Technical Considerations

### Security
1. **Authorization**: All admin endpoints must verify admin role
2. **Activity Logging**: Sanitize all logged data to prevent log injection
3. **Session Termination**: Verify admin cannot terminate their own session accidentally
4. **IP Address Privacy**: Consider GDPR implications of storing IP addresses

### Performance
1. **Database Indexes**: Ensure proper indexes on UserActivityLogs for efficient querying
2. **Log Retention**: Consider implementing automatic cleanup of old activity logs (e.g., keep 90 days)
3. **Pagination**: Implement pagination for activity logs if volume is high
4. **Caching**: Consider caching active sessions list with short TTL

### User Experience
1. **Real-time Updates**: Consider SignalR for real-time session updates
2. **Notifications**: Notify users when their session is terminated by admin
3. **Export**: Add ability to export activity logs to CSV/Excel
4. **Filtering**: Add more filter options (activity type, entity type, etc.)

### Monitoring
1. **Metrics**: Track number of active sessions
2. **Alerts**: Alert when unusual activity patterns detected
3. **Audit**: Ensure all admin actions are logged
4. **Compliance**: Ensure logging meets regulatory requirements

## Future Enhancements

### Short-term (Next Sprint)
1. Add session timeout warnings to users
2. Add "Remember Me" functionality with longer session duration
3. Add session history (past sessions) view
4. Add export functionality for activity logs

### Medium-term (Next Quarter)
1. Implement real-time session monitoring with SignalR
2. Add geographic location tracking for sessions
3. Add device fingerprinting for better security
4. Implement anomaly detection for suspicious activities
5. Add dashboard with session and activity statistics

### Long-term (Future)
1. Implement multi-factor authentication
2. Add session recording/playback for audit purposes
3. Implement AI-based threat detection
4. Add compliance reporting tools

## Rollback Plan

If issues arise during deployment:

1. **Database Rollback**:
   ```bash
   dotnet ef database update <PreviousMigrationName>
   ```

2. **Code Rollback**:
   - Revert to previous Git commit
   - Redeploy previous version

3. **Data Preservation**:
   - UserActivityLogs table can be kept even if feature is rolled back
   - No data loss risk as it's append-only

## Estimated Effort

- **Phase 1 (Fix Session Management)**: 4-6 hours
- **Phase 2 (User Activity Logging)**: 6-8 hours
- **Phase 3 (Admin Interface)**: 8-10 hours
- **Phase 4 (Database Migration)**: 1 hour
- **Phase 5 (Testing)**: 4-6 hours

**Total Estimated Effort**: 23-31 hours (3-4 working days)

## Dependencies

- .NET 6+ SDK
- Entity Framework Core
- Angular 15+
- SQL Server or compatible database

## Success Criteria

1. ✓ Users can login and logout without false session conflict warnings
2. ✓ Only one active session per user at any time
3. ✓ Admins can view all active sessions with duration and idle time
4. ✓ Admins can terminate any user session
5. ✓ All user actions are logged with timestamp, IP, and description
6. ✓ Admins can view and filter activity logs
7. ✓ System performance is not degraded by logging
8. ✓ All admin actions are properly authorized and logged

---

**Document Version**: 1.0  
**Created**: 2025-03-08  
**Author**: Kiro AI Assistant  
**Status**: Ready for Implementation
