/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Server.Data;
using Server.Exceptions;
using Server.Middleware;
using Server.Models;
using Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

#endregion

#region Application Build & Configuration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// Add authentication services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddScoped<IProtocolXmlService, ProtocolXmlService>();

// Add security services
builder.Services.AddScoped<ISqlInjectionDetector, SqlInjectionDetector>();
builder.Services.AddScoped<IInputValidationService, InputValidationService>();
builder.Services.AddScoped<ISessionManagementService, SessionManagementService>();
// Requirement 10.6: Content-Disposition headers for file downloads
builder.Services.AddScoped<IFileDownloadService, FileDownloadService>();
// Requirement 6.6: File upload validation (type, size, content) server-side
builder.Services.AddScoped<IFileValidationService, FileValidationService>();

// Add authorization service
// Requirement 8.1: Role-based access control (Admin, Inspector roles)
// Requirement 8.3: Resource ownership checks (users can only modify their own data unless admin)
// Requirement 8.5: Prevent privilege escalation (non-admin cannot grant admin role)
// Requirement 8.6: Log all authorization failures
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// Add concurrency control service
// Requirement 4.2, 4.3, 4.5: Concurrency detection, conflict response, and retry with exponential backoff
builder.Services.AddSingleton<IConcurrencyController, ConcurrencyController>();

// Add database write queue service
// Requirement 5.1: THE Concurrency_Controller SHALL implement a write queue for critical database operations
// Requirement 5.2: WHEN multiple write requests arrive for the same entity, THE Concurrency_Controller SHALL process them in order
builder.Services.AddSingleton<DatabaseWriteQueue>();
builder.Services.AddSingleton<IDatabaseWriteQueue>(sp => sp.GetRequiredService<DatabaseWriteQueue>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseWriteQueue>());

// Add session cleanup background service
// Requirement 3.5: Clean up expired sessions based on token expiration times
builder.Services.AddHostedService<SessionCleanupService>();

// Add rate limit blocking service
// Requirement 12.6: IF sustained rate limit violations occur, THEN THE Backend SHALL temporarily block the source
builder.Services.AddSingleton<IRateLimitBlockingService, RateLimitBlockingService>();

// Add global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Get JWT configuration from appsettings
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key not configured. Set Jwt:Key in appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Robigoo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RobigooUsers";
var key = Encoding.UTF8.GetBytes(jwtKey);

// Validate key length
if (key.Length < 32)
{
    throw new InvalidOperationException("JWT Key must be at least 256 bits (32 characters)");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Fail("Missing bearer token");
                return;
            }

            var rawToken = authHeader.Substring("Bearer ".Length).Trim();

            var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == rawToken && s.IsActive);
            if (session == null)
            {
                context.Fail("Session is not active");
                return;
            }

            session.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    };
});

builder.Services.AddAuthorization();

// Add Controllers
builder.Services.AddControllers();

// Add Antiforgery service  
builder.Services.AddAntiforgery();

// Configure CORS with security restrictions
// Requirement 10.5: THE Backend SHALL implement proper CORS configuration restricting origins in production
var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200" };

// Define allowed headers for production CORS policy
// These are the standard headers needed for API communication
var allowedHeaders = new[]
{
    "Content-Type",           // Required for JSON API requests
    "Authorization",          // Required for JWT authentication
    "Accept",                 // Standard HTTP header
    "Accept-Language",        // For internationalization
    "X-Requested-With",       // For AJAX requests
    "X-Correlation-Id",       // For request tracing
    "Cache-Control",          // For cache control
    "Pragma"                  // For HTTP/1.0 cache control
};

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // More permissive in development for easier testing
            // Allows any origin, header, and method during development
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Restrictive in production - Requirement 10.5
            // Only allow specific origins from configuration
            policy.WithOrigins(allowedOrigins)
                  // Explicitly specify allowed headers for security
                  .WithHeaders(allowedHeaders)
                  // Explicitly specify allowed HTTP methods
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  // Allow credentials (cookies, authorization headers)
                  .AllowCredentials()
                  // Expose headers that the client may need to read
                  .WithExposedHeaders("X-Correlation-Id", "Content-Disposition");
        }
    });
});

// Configure Rate Limiting
var loginPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginPermitLimit", 5);
var loginWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:LoginWindowMinutes", 1);
var generalPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:GeneralPermitLimit", 100);
var generalWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:GeneralWindowMinutes", 1);
var authenticatedUserPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthenticatedUserPermitLimit", 200);
var authenticatedUserWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:AuthenticatedUserWindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Requirement 12.7: THE Backend SHALL log rate limit violations for security monitoring
    // Requirement 12.6: IF sustained rate limit violations occur, THEN THE Backend SHALL temporarily block the source
    // Log all rate limit violations with source details (IP address, user ID, endpoint, timestamp)
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Get services from DI
        var auditService = context.HttpContext.RequestServices.GetService<ISecurityAuditService>();
        var blockingService = context.HttpContext.RequestServices.GetService<IRateLimitBlockingService>();
        
        // Extract source details
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
        var userId = context.HttpContext.User?.FindFirst("userId")?.Value;
        var endpoint = context.HttpContext.Request.Path.ToString();
        var method = context.HttpContext.Request.Method;
        var userAgent = context.HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
        
        // Determine the identifier for blocking (prefer user ID, fall back to IP)
        var blockIdentifier = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{ipAddress}";
        
        // Record the violation for potential blocking (Requirement 12.6)
        if (blockingService != null)
        {
            await blockingService.RecordViolationAsync(blockIdentifier);
        }
        
        if (auditService != null)
        {
            // Build details message
            var details = $"Rate limit exceeded for endpoint: {method} {endpoint}";
            if (!string.IsNullOrEmpty(userId))
            {
                details += $" by user ID: {userId}";
            }
            
            // Build metadata JSON with all relevant information
            var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter.TotalSeconds : 0;
            var isBlocked = blockingService != null && await blockingService.IsBlockedAsync(blockIdentifier);
            var violationCount = blockingService != null ? await blockingService.GetViolationCountAsync(blockIdentifier) : 0;
            var metadata = $"{{\"endpoint\": \"{endpoint}\", \"method\": \"{method}\", \"userId\": \"{userId ?? "anonymous"}\", \"retryAfter\": \"{retryAfterSeconds}s\", \"isBlocked\": {isBlocked.ToString().ToLower()}, \"violationCount\": {violationCount}}}";
            
            // Log the rate limit violation
            await auditService.LogSecurityEventAsync(
                SecurityEventType.RateLimitExceeded,
                details,
                ipAddress,
                login: null,
                userAgent,
                metadata);
        }
        
        // Set Retry-After header as per Requirement 12.3
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfterValue.TotalSeconds).ToString();
        }
    };
    
    // Rate limiter for login endpoint
    // Requirement 12.4: Sliding window rate limiting for more accurate throttling
    options.AddSlidingWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(loginWindowMinutes);
        opt.PermitLimit = loginPermitLimit;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
        opt.SegmentsPerWindow = 4; // Divide window into 4 segments for smoother rate limiting
    });
    
    // General rate limiter for API
    // Requirement 12.4: Sliding window rate limiting for more accurate throttling
    options.AddSlidingWindowLimiter("general", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(generalWindowMinutes);
        opt.PermitLimit = generalPermitLimit;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
        opt.SegmentsPerWindow = 4; // Divide window into 4 segments for smoother rate limiting
    });
    
    // Per-user rate limiter for authenticated users
    // Requirement 12.2: THE Backend SHALL implement per-user rate limiting in addition to per-IP limiting
    // Requirement 12.4: Sliding window rate limiting for more accurate throttling
    // Uses user ID from JWT claims as partition key, falls back to IP for unauthenticated requests
    options.AddPolicy("authenticated", context =>
    {
        // Try to get user ID from JWT claims for authenticated users
        var userId = context.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Authenticated user: partition by user ID
            // This ensures a single user cannot exhaust the rate limit for other users sharing the same IP
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"user:{userId}",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(authenticatedUserWindowMinutes),
                    PermitLimit = authenticatedUserPermitLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    SegmentsPerWindow = 4
                });
        }
        else
        {
            // Unauthenticated request: fall back to IP-based limiting
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"ip:{ipAddress}",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(generalWindowMinutes),
                    PermitLimit = generalPermitLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    SegmentsPerWindow = 4
                });
        }
    });
    
    // Global fallback
    // Requirement 12.4: Sliding window rate limiting for more accurate throttling
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 200,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                SegmentsPerWindow = 4
            });
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=app_v2.db"));

var app = builder.Build();

// Add global exception handler middleware (must be early in the pipeline)
app.UseExceptionHandler();

// Add correlation ID middleware (early in pipeline for request tracing)
app.UseCorrelationId();

// Security Headers Middleware
// Requirement 10.1: Content-Security-Policy header
// Requirement 10.2: X-Content-Type-Options: nosniff
// Requirement 10.3: X-Frame-Options: DENY
// Requirement 10.4: Strict-Transport-Security for production
// Requirement 10.7: Cache-Control headers for sensitive endpoints
app.UseSecurityHeaders();

// Map Controllers
app.MapControllers();

// Helper: verify that the JWT token belongs to an active user session
static async Task<bool> IsSessionActiveAsync(AppDbContext db, HttpContext context)
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var rawToken = authHeader.Substring("Bearer ".Length).Trim();

    var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == rawToken && s.IsActive);
    if (session == null)
    {
        return false;
    }

    session.LastActivityAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return true;
}

// ensure database exists and seed master admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    // UÅ¼ywamy EnsureCreated, aby nie wchodziÄ‡ w konflikt z istniejÄ…cymi migracjami,
    // a brakujÄ…ce tabele (UserSessions, Machines) tworzymy rÄ™cznie poniÅ¼ej.
    db.Database.EnsureCreated();

    // Requirement 4.7: THE Database_Access_Layer SHALL use SQLite WAL mode for improved concurrent read/write performance
    // WAL (Write-Ahead Logging) mode allows readers to continue while a write is in progress,
    // improving concurrent read/write performance for SQLite databases.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

    // Zapewnij istnienie tabeli UserSessions (jeÅ›li baza powstaÅ‚a wczeÅ›niej bez tej tabeli).
    // SQLite wspiera CREATE TABLE IF NOT EXISTS, wiÄ™c operacja jest idempotentna.
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""UserSessions"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_UserSessions"" PRIMARY KEY AUTOINCREMENT,
        ""UserId"" INTEGER NOT NULL,
        ""SessionToken"" TEXT NOT NULL,
        ""RefreshToken"" TEXT NULL,
        ""RefreshTokenExpiresAt"" TEXT NULL,
        ""CreatedAt"" TEXT NOT NULL,
        ""LastActivityAt"" TEXT NOT NULL,
        ""IsActive"" INTEGER NOT NULL,
        ""IpAddress"" TEXT NULL,
        ""UserAgent"" TEXT NULL,
        CONSTRAINT ""FK_UserSessions_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
    );");

    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_UserSessions_UserId"" ON ""UserSessions"" (""UserId"");");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserSessions_SessionToken"" ON ""UserSessions"" (""SessionToken"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_UserSessions_RefreshToken"" ON ""UserSessions"" (""RefreshToken"");");

    // Add new columns to UserSessions if they don't exist (migration for existing databases)
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""RefreshToken"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""RefreshTokenExpiresAt"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""IpAddress"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""UserAgent"" TEXT NULL;"); } catch { }
    // Session management enhancements (Requirements 3.4, 3.7)
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""SessionTimeoutMinutes"" INTEGER NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""InvalidatedAt"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserSessions"" ADD COLUMN ""InvalidationReason"" TEXT NULL;"); } catch { }

    // Create LoginAttempts table for security auditing
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""LoginAttempts"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_LoginAttempts"" PRIMARY KEY AUTOINCREMENT,
        ""Login"" TEXT NOT NULL,
        ""IpAddress"" TEXT NULL,
        ""UserAgent"" TEXT NULL,
        ""Success"" INTEGER NOT NULL,
        ""FailureReason"" TEXT NULL,
        ""AttemptedAt"" TEXT NOT NULL
    );");

    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_LoginAttempts_Login"" ON ""LoginAttempts"" (""Login"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_LoginAttempts_AttemptedAt"" ON ""LoginAttempts"" (""AttemptedAt"");");

    // Zapewnij istnienie tabeli Machines (jeÅ›li baza powstaÅ‚a wczeÅ›niej bez tej tabeli).
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""Machines"" (
        ""SerialNumber"" TEXT NOT NULL CONSTRAINT ""PK_Machines"" PRIMARY KEY,
        ""SprayerName"" TEXT NOT NULL,
        ""Type"" TEXT NOT NULL,
        ""Kind"" TEXT NOT NULL,
        ""Manufacturer"" TEXT NOT NULL,
        ""ProductionYear"" TEXT NOT NULL,
        ""PurchaseDate"" TEXT NULL,
        ""PumpPiston"" INTEGER NOT NULL,
        ""PumpDiaphragm"" INTEGER NOT NULL,
        ""PumpOther"" INTEGER NOT NULL,
        ""PumpOtherType"" TEXT NULL,
        ""PumpFlowRate"" TEXT NULL,
        ""TankCapacity"" TEXT NULL,
        ""HasFlushing"" INTEGER NOT NULL,
        ""HasDiluter"" INTEGER NOT NULL,
        ""HasWashingDevice"" INTEGER NOT NULL,
        ""HasManometer"" INTEGER NOT NULL,
        ""HasComputer"" INTEGER NOT NULL,
        ""BoomWidth"" TEXT NULL,
        ""BoomWet"" INTEGER NOT NULL,
        ""BoomDry"" INTEGER NOT NULL,
        ""BoomDampeningMechanism"" INTEGER NOT NULL,
        ""SectionCount"" INTEGER NULL,
        ""NozzlesFieldFeatures"" TEXT NULL,
        ""NozzlesGardenFeatures"" TEXT NULL,
        ""FanType"" TEXT NULL,
        ""OwnerId"" TEXT NULL,
        ""OwnerName"" TEXT NULL,
        ""RowVersion"" BLOB NULL,
        ""CreatedAt"" TEXT NOT NULL,
        ""UpdatedAt"" TEXT NULL
    );");

    // Add missing columns to Machines if they don't exist (migration for existing databases)
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Machines"" ADD COLUMN ""OwnerId"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Machines"" ADD COLUMN ""OwnerName"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Machines"" ADD COLUMN ""RowVersion"" BLOB NULL;"); } catch { }

    // Create InspectionProtocols table for protocol data
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""InspectionProtocols"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_InspectionProtocols"" PRIMARY KEY AUTOINCREMENT,
        ""ProtocolNumber"" TEXT NOT NULL,
        ""InspectionDate"" TEXT NOT NULL,
        ""InspectionLocation"" TEXT NULL,
        ""InspectorName"" TEXT NOT NULL,
        ""InspectorLicenseNumber"" TEXT NULL,
        ""ClientId"" TEXT NULL,
        ""ClientName"" TEXT NULL,
        ""ClientAddress"" TEXT NULL,
        ""ClientTaxId"" TEXT NULL,
        ""CropSprayerSerialNumber"" TEXT NULL,
        ""CropSprayerName"" TEXT NULL,
        ""CropSprayerType"" TEXT NULL,
        ""CropSprayerKind"" TEXT NULL,
        ""CropSprayerManufacturer"" TEXT NULL,
        ""CropSprayerProductionYear"" TEXT NULL,
        ""TankCapacity"" TEXT NULL,
        ""BoomWidth"" TEXT NULL,
        ""SectionCount"" INTEGER NULL,
        ""GeneralConditionPassed"" INTEGER NULL,
        ""MarkingsReadablePassed"" INTEGER NULL,
        ""EquipmentCompletePassed"" INTEGER NULL,
        ""GeneralSectionNotes"" TEXT NULL,
        ""PumpOperationPassed"" INTEGER NULL,
        ""PumpSealingPassed"" INTEGER NULL,
        ""PressurePulsationPassed"" INTEGER NULL,
        ""PumpSectionNotes"" TEXT NULL,
        ""AgitatorOperationPassed"" INTEGER NULL,
        ""AgitatorSectionNotes"" TEXT NULL,
        ""TankConditionPassed"" INTEGER NULL,
        ""TankSealingPassed"" INTEGER NULL,
        ""LevelIndicatorPassed"" INTEGER NULL,
        ""FlushingSystemPassed"" INTEGER NULL,
        ""TankSectionNotes"" TEXT NULL,
        ""ManometerPassed"" INTEGER NULL,
        ""ManometerReading2Bar"" TEXT NULL,
        ""ManometerReading4Bar"" TEXT NULL,
        ""ManometerReading6Bar"" TEXT NULL,
        ""ManometerDialSizePassed"" INTEGER NULL,
        ""MeasuringSectionNotes"" TEXT NULL,
        ""PipesConditionPassed"" INTEGER NULL,
        ""ConnectionsSealingPassed"" INTEGER NULL,
        ""PipingSectionNotes"" TEXT NULL,
        ""SuctionFilterPassed"" INTEGER NULL,
        ""PressureFilterPassed"" INTEGER NULL,
        ""NozzleFiltersPassed"" INTEGER NULL,
        ""FiltrationSectionNotes"" TEXT NULL,
        ""FieldBoomConditionPassed"" INTEGER NULL,
        ""BoomStabilityPassed"" INTEGER NULL,
        ""BoomHeightPassed"" INTEGER NULL,
        ""BoomSymmetryPassed"" INTEGER NULL,
        ""OrchardSprayerConditionPassed"" INTEGER NULL,
        ""AirStreamDirectionPassed"" INTEGER NULL,
        ""BoomSectionNotes"" TEXT NULL,
        ""NozzleUniformityPassed"" INTEGER NULL,
        ""NozzleFlowRatePassed"" INTEGER NULL,
        ""NozzleConditionPassed"" INTEGER NULL,
        ""NozzleMeasurements"" TEXT NULL,
        ""NozzlesSectionNotes"" TEXT NULL,
        ""TransverseDistributionPassed"" INTEGER NULL,
        ""CoefficientOfVariation"" TEXT NULL,
        ""DistributionSectionNotes"" TEXT NULL,
        ""FinalResult"" INTEGER NULL,
        ""ValidUntil"" TEXT NULL,
        ""ControlStickerNumber"" TEXT NULL,
        ""GeneralNotes"" TEXT NULL,
        ""CreatedAt"" TEXT NOT NULL,
        ""UpdatedAt"" TEXT NULL,
        ""ProtocolXml"" TEXT NULL,
        ""XslTemplateVersion"" TEXT NULL,
        ""XmlGeneratedAt"" TEXT NULL,
        ""RowVersion"" BLOB NULL,
        ""Version"" INTEGER NOT NULL DEFAULT 0
    );");

    // Add missing columns to InspectionProtocols if they don't exist (migration for existing databases)
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""InspectionProtocols"" ADD COLUMN ""RowVersion"" BLOB NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""InspectionProtocols"" ADD COLUMN ""Version"" INTEGER NOT NULL DEFAULT 0;"); } catch { }

    // Create SecurityEventLogs table for security auditing
    // Requirement 9.7: Log sensitive data access for compliance purposes
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""SecurityEventLogs"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SecurityEventLogs"" PRIMARY KEY AUTOINCREMENT,
        ""EventType"" INTEGER NOT NULL,
        ""Details"" TEXT NULL,
        ""IpAddress"" TEXT NULL,
        ""UserId"" INTEGER NULL,
        ""Login"" TEXT NULL,
        ""UserAgent"" TEXT NULL,
        ""CorrelationId"" TEXT NULL,
        ""Metadata"" TEXT NULL,
        ""OccurredAt"" TEXT NOT NULL
    );");
    
    // Add missing columns to SecurityEventLogs if they don't exist (ignore errors if columns already exist)
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SecurityEventLogs"" ADD COLUMN ""UserId"" INTEGER NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SecurityEventLogs"" ADD COLUMN ""CorrelationId"" TEXT NULL;"); } catch { }

    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_SecurityEventLogs_OccurredAt"" ON ""SecurityEventLogs"" (""OccurredAt"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_SecurityEventLogs_EventType_OccurredAt"" ON ""SecurityEventLogs"" (""EventType"", ""OccurredAt"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_SecurityEventLogs_IpAddress_OccurredAt"" ON ""SecurityEventLogs"" (""IpAddress"", ""OccurredAt"");");

    // Seed master admin if not exists
    if (!db.Users.Any(u => u.Login == "admin"))
    {
        var masterAdmin = new Server.Models.User
        {
            Login = "admin",
            PasswordHash = passwordService.HashPassword("admin123"),
            FirstName = "Master",
            LastName = "Administrator",
            Email = "admin@robigoo.local",
            Phone = "",
            PermissionNumber = "MASTER_ADMIN_001",
            Role = "admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Language = "pl",
            Theme = "light"
        };
        db.Users.Add(masterAdmin);
        db.SaveChanges();
    }
}

app.UseCors();

// Add Rate Limiting middleware
app.UseRateLimiter();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add rate limit blocking middleware (checks if source is blocked due to sustained violations)
// Requirement 12.6: IF sustained rate limit violations occur, THEN THE Backend SHALL temporarily block the source
// This runs after authentication so we can check both IP and user-based blocks
app.UseRateLimitBlocking();

app.UseAntiforgery();

// Add input validation middleware (validates all incoming request bodies)
// Requirement 2.7: Returns 400 Bad Request with specific validation error messages on failure
app.UseInputValidation();

// ==================== CLIENTS (CUSTOMERS) ENDPOINTS ====================

// Get clients list with filters
app.MapGet("/api/clients", async (AppDbContext db, HttpContext context,
    [FromQuery] string? q,
    [FromQuery] string? clientType,
    [FromQuery] string? city) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var queryable = db.Clients.AsQueryable();

    if (!string.IsNullOrWhiteSpace(q))
    {
        var term = q.Trim().ToLowerInvariant();
        queryable = queryable.Where(c =>
            c.DisplayName.ToLower().Contains(term) ||
            (c.City != null && c.City.ToLower().Contains(term)) ||
            (c.Nip != null && c.Nip.Contains(term)) ||
            (c.Pesel != null && c.Pesel.Contains(term)) ||
            (c.FirstName != null && c.FirstName.ToLower().Contains(term)) ||
            (c.LastName != null && c.LastName.ToLower().Contains(term)) ||
            (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
    }

    if (!string.IsNullOrWhiteSpace(clientType))
    {
        queryable = queryable.Where(c => c.ClientType == clientType);
    }

    if (!string.IsNullOrWhiteSpace(city))
    {
        var cityTerm = city.Trim().ToLowerInvariant();
        queryable = queryable.Where(c => c.City != null && c.City.ToLower().Contains(cityTerm));
    }

    var clients = await queryable
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();

    var result = clients.Select(c => new ClientListItemDto
    {
        Id = c.Id,
        ClientType = c.ClientType,
        DisplayName = c.DisplayName,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Pesel = c.Pesel,
        CompanyName = c.CompanyName,
        Nip = c.Nip,
        Regon = c.Regon,
        Voivodeship = c.Voivodeship,
        City = c.City,
        Street = c.Street,
        BuildingNumber = c.BuildingNumber,
        ApartmentNumber = c.ApartmentNumber,
        ZipCode = c.ZipCode,
        CreatedAt = c.CreatedAt
    }).ToList();

    return Results.Ok(result);
}).RequireAuthorization();

// Get single client by ID
app.MapGet("/api/clients/{id}", async (AppDbContext db, HttpContext context, string id) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var client = await db.Clients.FindAsync(id);
    if (client == null)
    {
        return Results.NotFound();
    }

    var detail = new ClientDetailDto
    {
        Id = client.Id,
        ClientType = client.ClientType,
        FirstName = client.FirstName,
        LastName = client.LastName,
        Pesel = client.Pesel,
        CompanyName = client.CompanyName,
        Nip = client.Nip,
        Regon = client.Regon,
        Voivodeship = client.Voivodeship,
        City = client.City,
        Street = client.Street,
        BuildingNumber = client.BuildingNumber,
        ApartmentNumber = client.ApartmentNumber,
        ZipCode = client.ZipCode,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt
    };

    return Results.Ok(detail);
}).RequireAuthorization();

// Create new client
app.MapPost("/api/clients", async (AppDbContext db, HttpContext context, [FromBody] ClientCreateUpdateDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Compute display name
    var displayName = dto.ClientType == "company"
        ? dto.CompanyName ?? string.Empty
        : $"{dto.FirstName} {dto.LastName}".Trim();

    var client = new Client
    {
        Id = Guid.NewGuid().ToString(),
        ClientType = dto.ClientType,
        DisplayName = displayName,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Pesel = dto.Pesel,
        CompanyName = dto.CompanyName,
        Nip = dto.Nip,
        Regon = dto.Regon,
        Voivodeship = dto.Voivodeship,
        City = dto.City,
        Street = dto.Street,
        BuildingNumber = dto.BuildingNumber,
        ApartmentNumber = dto.ApartmentNumber,
        ZipCode = dto.ZipCode,
        CreatedAt = DateTime.UtcNow
    };

    db.Clients.Add(client);
    await db.SaveChangesAsync();

    var detail = new ClientDetailDto
    {
        Id = client.Id,
        ClientType = client.ClientType,
        FirstName = client.FirstName,
        LastName = client.LastName,
        Pesel = client.Pesel,
        CompanyName = client.CompanyName,
        Nip = client.Nip,
        Regon = client.Regon,
        Voivodeship = client.Voivodeship,
        City = client.City,
        Street = client.Street,
        BuildingNumber = client.BuildingNumber,
        ApartmentNumber = client.ApartmentNumber,
        ZipCode = client.ZipCode,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt
    };

    return Results.Created($"/api/clients/{client.Id}", detail);
}).RequireAuthorization();

// Update existing client
app.MapPut("/api/clients/{id}", async (AppDbContext db, HttpContext context, string id, [FromBody] ClientCreateUpdateDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var client = await db.Clients.FindAsync(id);
    if (client == null)
    {
        return Results.NotFound();
    }

    // Compute display name
    var displayName = dto.ClientType == "company"
        ? dto.CompanyName ?? string.Empty
        : $"{dto.FirstName} {dto.LastName}".Trim();

    client.ClientType = dto.ClientType;
    client.DisplayName = displayName;
    client.FirstName = dto.FirstName;
    client.LastName = dto.LastName;
    client.Pesel = dto.Pesel;
    client.CompanyName = dto.CompanyName;
    client.Nip = dto.Nip;
    client.Regon = dto.Regon;
    client.Voivodeship = dto.Voivodeship;
    client.City = dto.City;
    client.Street = dto.Street;
    client.BuildingNumber = dto.BuildingNumber;
    client.ApartmentNumber = dto.ApartmentNumber;
    client.ZipCode = dto.ZipCode;
    client.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    var detail = new ClientDetailDto
    {
        Id = client.Id,
        ClientType = client.ClientType,
        FirstName = client.FirstName,
        LastName = client.LastName,
        Pesel = client.Pesel,
        CompanyName = client.CompanyName,
        Nip = client.Nip,
        Regon = client.Regon,
        Voivodeship = client.Voivodeship,
        City = client.City,
        Street = client.Street,
        BuildingNumber = client.BuildingNumber,
        ApartmentNumber = client.ApartmentNumber,
        ZipCode = client.ZipCode,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt
    };

    return Results.Ok(detail);
}).RequireAuthorization();

// Delete client
app.MapDelete("/api/clients/{id}", async (AppDbContext db, HttpContext context, string id) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var client = await db.Clients.FindAsync(id);
    if (client == null)
    {
        return Results.NotFound();
    }

    // Optionally: Check if client has associated machines and handle accordingly
    // For now, just delete the client

    db.Clients.Remove(client);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

// ==================== ORIGINAL ENDPOINTS ====================

// Inspections API - wymaga autoryzacji i aktywnej sesji
app.MapGet("/api/inspections", async (AppDbContext db, HttpContext context) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var inspections = await db.Inspections
        .Include(i => i.Items)
        .OrderByDescending(i => i.Id)
        .ToListAsync();
    var response = inspections.Select(i => new Server.Models.InspectionResponseDto(
        i.Id,
        i.VehiclePlate,
        i.InspectorName,
        i.InspectionDate,
        i.Notes,
        i.Items.Select(it => new Server.Models.InspectionItemDto(it.Description, it.Passed)).ToList()
    ));
    return Results.Ok(response);
}).RequireAuthorization();

app.MapPost("/api/inspections", async (AppDbContext db, HttpContext context, Server.Models.InspectionCreateDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var inspection = new Server.Models.Inspection
    {
        VehiclePlate = dto.VehiclePlate,
        InspectorName = dto.InspectorName,
        InspectionDate = dto.InspectionDate,
        Notes = dto.Notes
    };
    foreach (var it in dto.Items ?? new List<Server.Models.InspectionItemDto>())
    {
        inspection.Items.Add(new Server.Models.InspectionItem { Description = it.Description, Passed = it.Passed });
    }
    db.Inspections.Add(inspection);
    await db.SaveChangesAsync();
    var response = new Server.Models.InspectionResponseDto(
        inspection.Id,
        inspection.VehiclePlate,
        inspection.InspectorName,
        inspection.InspectionDate,
        inspection.Notes,
        inspection.Items.Select(it => new Server.Models.InspectionItemDto(it.Description, it.Passed)).ToList()
    );
    return Results.Created($"/api/inspections/{inspection.Id}", response);
}).RequireAuthorization();

// ==================== HISTORY ENDPOINTS ====================
app.MapGet("/api/History/{entityName}/{entityId}", async (string entityName, string entityId, AppDbContext db, HttpContext context) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Join ChangeLogs with Users to get current user details
    var logs = await db.ChangeLogs
        .Where(l => l.EntityName == entityName && l.EntityId == entityId)
        .GroupJoin(
            db.Users,
            log => log.Who,
            user => user.Login,
            (log, users) => new { log, users }
        )
        .SelectMany(
            x => x.users.DefaultIfEmpty(),
            (x, user) => new 
            {
                x.log.Id,
                x.log.EntityName,
                x.log.EntityId,
                x.log.Changes,
                x.log.When,
                Who = x.log.Who,
                // If user exists, take current data, otherwise null
                UserFirstName = user != null ? user.FirstName : null,
                UserLastName = user != null ? user.LastName : null, 
                UserAvatarData = user != null ? user.AvatarData : null
            }
        )
        .OrderByDescending(l => l.When)
        .ToListAsync();

    return Results.Ok(logs);
}).RequireAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

#endregion
