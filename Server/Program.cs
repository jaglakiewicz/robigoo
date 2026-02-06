/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Server.Data;
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
var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // More permissive in development
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            // Restrictive in production
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .AllowCredentials();
        }
    });
});

// Configure Rate Limiting
var loginPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginPermitLimit", 5);
var loginWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:LoginWindowMinutes", 1);
var generalPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:GeneralPermitLimit", 100);
var generalWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:GeneralWindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Rate limiter for login endpoint
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(loginWindowMinutes);
        opt.PermitLimit = loginPermitLimit;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    
    // General rate limiter for API
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(generalWindowMinutes);
        opt.PermitLimit = generalPermitLimit;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    
    // Global fallback
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 200,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=app_v2.db"));

var app = builder.Build();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    // Content Security Policy
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'");
    
    // Prevent MIME type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // Clickjacking protection
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    
    // XSS Protection (legacy browsers)
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    // Referrer Policy
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Permissions Policy
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    
    await next();
});

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
    // Używamy EnsureCreated, aby nie wchodzić w konflikt z istniejącymi migracjami,
    // a brakujące tabele (UserSessions, Machines) tworzymy ręcznie poniżej.
    db.Database.EnsureCreated();

    // Zapewnij istnienie tabeli UserSessions (jeśli baza powstała wcześniej bez tej tabeli).
    // SQLite wspiera CREATE TABLE IF NOT EXISTS, więc operacja jest idempotentna.
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

    // Zapewnij istnienie tabeli Machines (jeśli baza powstała wcześniej bez tej tabeli).
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
        ""CreatedAt"" TEXT NOT NULL,
        ""UpdatedAt"" TEXT NULL
    );");

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
        ""XmlGeneratedAt"" TEXT NULL
    );");

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
app.UseAntiforgery();

// Helper: Get client IP address
static string? GetClientIpAddress(HttpContext context)
{
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedFor))
    {
        return forwardedFor.Split(',').FirstOrDefault()?.Trim();
    }
    return context.Connection.RemoteIpAddress?.ToString();
}

// ==================== AUTH ENDPOINTS ====================
app.MapPost("/api/auth/login", async (AppDbContext db, IPasswordService passwordService, ITokenService tokenService, ISecurityAuditService auditService, HttpContext context, Server.Models.LoginDto dto) =>
{
    var ipAddress = GetClientIpAddress(context);
    var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();

    // Check if locked out
    if (await auditService.IsLockedOutAsync(dto.Login, ipAddress))
    {
        await auditService.LogLoginAttemptAsync(dto.Login, ipAddress, userAgent, false, "Account locked out");
        return Results.Json(new { message = "Zbyt wiele nieudanych prób logowania. Spróbuj ponownie za chwilę." }, statusCode: 429);
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Login == dto.Login && u.IsActive);
    
    if (user == null)
    {
        await auditService.LogLoginAttemptAsync(dto.Login, ipAddress, userAgent, false, "User not found");
        return Results.Unauthorized();
    }

    if (!passwordService.VerifyPassword(dto.Password, user.PasswordHash))
    {
        await auditService.LogLoginAttemptAsync(dto.Login, ipAddress, userAgent, false, "Invalid password");
        return Results.Unauthorized();
    }

    // Upgrade password hash if using legacy algorithm
    if (passwordService.NeedsRehash(user.PasswordHash))
    {
        user.PasswordHash = passwordService.HashPassword(dto.Password);
    }

    // Check for existing active session for this user
    var existingSession = await db.UserSessions
        .Where(s => s.UserId == user.Id && s.IsActive)
        .OrderByDescending(s => s.LastActivityAt)
        .FirstOrDefaultAsync();

    if (existingSession != null && !dto.Force)
    {
        return Results.Conflict(new
        {
            error = "active_session_exists",
            message = "Dla tego użytkownika istnieje już aktywna sesja.",
            session = new
            {
                createdAt = existingSession.CreatedAt,
                lastActivityAt = existingSession.LastActivityAt
            }
        });
    }

    // If force takeover requested, deactivate all existing active sessions
    if (existingSession != null && dto.Force)
    {
        var activeSessions = await db.UserSessions
            .Where(s => s.UserId == user.Id && s.IsActive)
            .ToListAsync();

        foreach (var s in activeSessions)
        {
            s.IsActive = false;
            s.LastActivityAt = DateTime.UtcNow;
        }
    }

    // Update last login
    user.LastLoginAt = DateTime.UtcNow;

    var now = DateTime.UtcNow;

    // Generate tokens
    var accessToken = tokenService.GenerateAccessToken(user.Id, user.Login, user.Role);
    var refreshToken = tokenService.GenerateRefreshToken();

    // Create new session record
    var newSession = new UserSession
    {
        UserId = user.Id,
        SessionToken = accessToken,
        RefreshToken = refreshToken,
        RefreshTokenExpiresAt = now.AddDays(tokenService.GetRefreshTokenExpirationDays()),
        CreatedAt = now,
        LastActivityAt = now,
        IsActive = true,
        IpAddress = ipAddress?.Substring(0, Math.Min(ipAddress.Length, 50)),
        UserAgent = userAgent?.Substring(0, Math.Min(userAgent.Length, 500))
    };

    db.UserSessions.Add(newSession);

    // Log successful login
    await auditService.LogLoginAttemptAsync(dto.Login, ipAddress, userAgent, true);

    await db.SaveChangesAsync();

    var userResponse = new Server.Models.LoginResponseDto
    {
        UserId = user.Id,
        Login = user.Login,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        PermissionNumber = user.PermissionNumber,
        Role = user.Role,
        AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
    };

    var tokenResponse = new Server.Models.TokenResponseDto
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresIn = tokenService.GetAccessTokenExpirationMinutes() * 60
    };

    // Return in format compatible with existing frontend (token field for backward compatibility)
    return Results.Ok(new { 
        token = accessToken, 
        refreshToken = refreshToken,
        expiresIn = tokenResponse.ExpiresIn,
        user = userResponse 
    });
}).RequireRateLimiting("login");

// Refresh token endpoint
app.MapPost("/api/auth/refresh", async (AppDbContext db, ITokenService tokenService, HttpContext context, [FromBody] Server.Models.RefreshTokenDto dto) =>
{
    // Validate the expired access token to get claims
    var principal = tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
    if (principal == null)
    {
        return Results.Unauthorized();
    }

    var userIdClaim = principal.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    // Find session with matching refresh token
    var session = await db.UserSessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => 
            s.UserId == userId && 
            s.RefreshToken == dto.RefreshToken && 
            s.IsActive &&
            s.RefreshTokenExpiresAt > DateTime.UtcNow);

    if (session == null)
    {
        return Results.Unauthorized();
    }

    var user = session.User;
    if (!user.IsActive)
    {
        return Results.Unauthorized();
    }

    var now = DateTime.UtcNow;

    // Generate new tokens
    var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Login, user.Role);
    var newRefreshToken = tokenService.GenerateRefreshToken();

    // Update session
    session.SessionToken = newAccessToken;
    session.RefreshToken = newRefreshToken;
    session.RefreshTokenExpiresAt = now.AddDays(tokenService.GetRefreshTokenExpirationDays());
    session.LastActivityAt = now;

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        token = newAccessToken,
        refreshToken = newRefreshToken,
        expiresIn = tokenService.GetAccessTokenExpirationMinutes() * 60
    });
});

app.MapGet("/api/auth/me", async (AppDbContext db, HttpContext context) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var userIdClaim = context.User.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.NotFound();
    }

    var response = new Server.Models.LoginResponseDto
    {
        UserId = user.Id,
        Login = user.Login,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        PermissionNumber = user.PermissionNumber,
        Role = user.Role,
        AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
    };

    return Results.Ok(response);
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async (AppDbContext db, HttpContext context) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Mark current session as inactive on the server
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        var rawToken = authHeader.Substring("Bearer ".Length).Trim();
        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == rawToken && s.IsActive);
        if (session != null)
        {
            session.IsActive = false;
            session.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    return Results.Ok();
}).RequireAuthorization();

// ==================== USER MANAGEMENT ENDPOINTS ====================
app.MapPost("/api/users", async (AppDbContext db, IPasswordService passwordService, HttpContext context, Server.Models.CreateUserDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Check if current user is admin
    var roleClaim = context.User.FindFirst(ClaimTypes.Role);
    if (roleClaim?.Value != "admin")
    {
        return Results.Forbid();
    }

    // Validate password strength
    var passwordValidation = passwordService.ValidatePasswordStrength(dto.Password);
    if (!passwordValidation.IsValid)
    {
        return Results.BadRequest(new { error = string.Join(". ", passwordValidation.Errors) });
    }

    // Check if login already exists
    if (await db.Users.AnyAsync(u => u.Login == dto.Login))
    {
        return Results.BadRequest(new { error = "Login already exists" });
    }

    // Regular admin can create new users with any role (including admin)
    // Master admin can also create users with any role
    // No additional restrictions on role selection during creation
    var userRole = (dto.Role == "admin") ? "admin" : "user";

    var user = new Server.Models.User
    {
        Login = dto.Login,
        PasswordHash = passwordService.HashPassword(dto.Password),
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        Phone = dto.Phone,
        PermissionNumber = dto.PermissionNumber,
        Role = userRole,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var response = new Server.Models.LoginResponseDto
    {
        UserId = user.Id,
        Login = user.Login,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        PermissionNumber = user.PermissionNumber,
        Role = user.Role
    };

    return Results.Created($"/api/users/{user.Id}", response);
}).RequireAuthorization();

app.MapGet("/api/users", async (AppDbContext db, HttpContext context) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Check if current user is admin
    var roleClaim = context.User.FindFirst(ClaimTypes.Role);
    if (roleClaim?.Value != "admin")
    {
        return Results.Forbid();
    }

    var users = await db.Users.Select(u => new Server.Models.LoginResponseDto
    {
        UserId = u.Id,
        Login = u.Login,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        Phone = u.Phone,
        PermissionNumber = u.PermissionNumber,
        Role = u.Role
    }).ToListAsync();

    return Results.Ok(users);
}).RequireAuthorization();

// ==================== USER PROFILE ENDPOINTS ====================
app.MapPut("/api/users/profile", async (AppDbContext db, HttpContext context, Server.Models.UpdateUserDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var userIdClaim = context.User.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var currentUser = await db.Users.FindAsync(userId);
    if (currentUser == null)
        return Results.NotFound();

    var user = await db.Users.FindAsync(userId);
    if (user == null)
        return Results.NotFound();

    // Check if user is admin
    bool isAdmin = currentUser.Role == "admin";

    // Only admins can modify FirstName, LastName, and PermissionNumber
    if (!string.IsNullOrEmpty(dto.FirstName))
    {
        if (!isAdmin)
            return Results.Forbid();
        user.FirstName = dto.FirstName;
    }
    if (!string.IsNullOrEmpty(dto.LastName))
    {
        if (!isAdmin)
            return Results.Forbid();
        user.LastName = dto.LastName;
    }
    if (!string.IsNullOrEmpty(dto.PermissionNumber))
    {
        if (!isAdmin)
            return Results.Forbid();
        user.PermissionNumber = dto.PermissionNumber;
    }

    // All users can modify these fields
    if (!string.IsNullOrEmpty(dto.Email))
        user.Email = dto.Email;
    if (!string.IsNullOrEmpty(dto.Phone))
        user.Phone = dto.Phone;

    await db.SaveChangesAsync();

    var response = new Server.Models.LoginResponseDto
    {
        UserId = user.Id,
        Login = user.Login,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        PermissionNumber = user.PermissionNumber,
        Role = user.Role,
        AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
    };

    return Results.Ok(response);
}).RequireAuthorization();

app.MapPost("/api/users/change-password", async (AppDbContext db, IPasswordService passwordService, HttpContext context, Server.Models.ChangePasswordDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var userIdClaim = context.User.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.FindAsync(userId);
    if (user == null)
        return Results.NotFound();

    if (!passwordService.VerifyPassword(dto.OldPassword, user.PasswordHash))
        return Results.BadRequest(new { message = "Stare hasło jest niepoprawne" });

    // Validate new password strength
    var passwordValidation = passwordService.ValidatePasswordStrength(dto.NewPassword);
    if (!passwordValidation.IsValid)
    {
        return Results.BadRequest(new { message = string.Join(". ", passwordValidation.Errors) });
    }

    user.PasswordHash = passwordService.HashPassword(dto.NewPassword);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Hasło zostało zmienione" });
}).RequireAuthorization();

app.MapPost("/api/users/avatar", async (AppDbContext db, HttpContext context, IFormFile file) =>
{
    try
    {
        if (!await IsSessionActiveAsync(db, context))
        {
            return Results.Unauthorized();
        }

        var userIdClaim = context.User.FindFirst("userId");
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return Results.NotFound();

        if (file == null || file.Length == 0)
            return Results.BadRequest(new { message = "Plik jest wymagany" });

        // Validate file type
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType))
            return Results.BadRequest(new { message = "Niewspierany format pliku" });

        // Max 5MB
        if (file.Length > 5 * 1024 * 1024)
            return Results.BadRequest(new { message = "Plik jest za duzy" });

        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            user.AvatarData = ms.ToArray();
        }

        await db.SaveChangesAsync();

        var response = new Server.Models.LoginResponseDto
        {
            UserId = user.Id,
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            PermissionNumber = user.PermissionNumber,
            Role = user.Role,
            AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = "Blad podczas przesylania awatara: " + ex.Message });
    }
}).RequireAuthorization().DisableAntiforgery();

// Delete user endpoint - with password confirmation
app.MapDelete("/api/users/{userId}", async (AppDbContext db, HttpContext context, long userId, [FromBody] Server.Models.ConfirmPasswordDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Get current user ID from claims
    var userIdClaim = context.User.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var currentUserId))
    {
        return Results.Unauthorized();
    }

    // Get the user to delete
    var userToDelete = await db.Users.FindAsync(userId);
    if (userToDelete == null)
        return Results.NotFound(new { message = "Użytkownik nie znaleziony" });

    // Prevent deleting self
    if (currentUserId == userId)
        return Results.BadRequest(new { message = "Nie możesz usunąć własnego konta" });

    // Get current user to validate password and permissions
    var currentUser = await db.Users.FindAsync(currentUserId);
    if (currentUser == null)
        return Results.Unauthorized();

    // Only admin can delete users
    if (currentUser.Role != "admin")
        return Results.Forbid();

    // Check deletion permissions based on master admin and target role
    bool isMasterAdmin = currentUser.Login == "admin";
    bool isTargetAdmin = userToDelete.Role == "admin";
    
    // Master admin (login: admin) can delete anyone except themselves
    // Regular admin can only delete regular users (not admins)
    if (isTargetAdmin && !isMasterAdmin)
        return Results.BadRequest(new { message = "Tylko master administrator może usuwać administratorów" });

    // Verify password
    var passwordService = context.RequestServices.GetRequiredService<Server.Services.IPasswordService>();
    if (!passwordService.VerifyPassword(dto.Password, currentUser.PasswordHash))
    {
        return Results.BadRequest(new { message = "Błędne hasło" });
    }

    // Delete user
    db.Users.Remove(userToDelete);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = $"Użytkownik '{userToDelete.Login}' został usunięty" });
}).RequireAuthorization().DisableAntiforgery();

// Update user role endpoint - ONLY master admin can change roles
app.MapPut("/api/users/{userId}/role", async (AppDbContext db, HttpContext context, long userId, Server.Models.UpdateUserRoleDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // Get current user ID from claims
    var userIdClaim = context.User.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var currentUserId))
    {
        return Results.Unauthorized();
    }

    // Get current user to validate permissions
    var currentUser = await db.Users.FindAsync(currentUserId);
    if (currentUser == null)
        return Results.Unauthorized();

    // Only master admin (login = admin) can change roles
    if (currentUser.Login != "admin")
        return Results.BadRequest(new { message = "Tylko master administrator może zmieniać role użytkowników" });

    // Get the user to update
    var userToUpdate = await db.Users.FindAsync(userId);
    if (userToUpdate == null)
        return Results.NotFound(new { message = "Użytkownik nie znaleziony" });

    // Prevent changing self role
    if (currentUserId == userId)
        return Results.BadRequest(new { message = "Nie możesz zmienić swojej roli" });

    // Validate role value
    if (dto.Role != "admin" && dto.Role != "user")
        return Results.BadRequest(new { message = "Niepoprawna rola. Dozwolone wartości: 'admin', 'user'" });

    // Update role
    userToUpdate.Role = dto.Role;
    await db.SaveChangesAsync();

    var response = new Server.Models.LoginResponseDto
    {
        UserId = userToUpdate.Id,
        Login = userToUpdate.Login,
        FirstName = userToUpdate.FirstName,
        LastName = userToUpdate.LastName,
        Email = userToUpdate.Email,
        Phone = userToUpdate.Phone,
        PermissionNumber = userToUpdate.PermissionNumber,
        Role = userToUpdate.Role,
        AvatarBase64 = userToUpdate.AvatarData != null ? Convert.ToBase64String(userToUpdate.AvatarData) : null
    };

    return Results.Ok(response);
}).RequireAuthorization().DisableAntiforgery();

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
