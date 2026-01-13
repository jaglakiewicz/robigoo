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

// Helper: validate machine DTO
static string? ValidateMachineDto(MachineCreateUpdateDto dto, bool isCreate)
{
    var serial = (dto.SerialNumber ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(serial))
        return "Numer seryjny/ewidencyjny jest wymagany";

    if (string.IsNullOrWhiteSpace(dto.SprayerName))
        return "Nazwa opryskiwacza jest wymagana";

    if (string.IsNullOrWhiteSpace(dto.Type) || (dto.Type != "00" && dto.Type != "01"))
        return "Nieprawidłowy typ (dozwolone: 00 - polowy, 01 - sadowniczy)";

    if (string.IsNullOrWhiteSpace(dto.Kind) || (dto.Kind != "00" && dto.Kind != "01" && dto.Kind != "02" && dto.Kind != "03"))
        return "Nieprawidłowy rodzaj (dozwolone: 00, 01, 02, 03)";

    if (string.IsNullOrWhiteSpace(dto.Manufacturer))
        return "Producent jest wymagany";

    if (!IsFourDigitYear(dto.ProductionYear))
        return "Rok produkcji musi mieć dokładnie 4 cyfry";

    if (dto.PumpOther && string.IsNullOrWhiteSpace(dto.PumpOtherType))
        return "Dla pompy 'inna' należy podać typ";

    if (!dto.PumpPiston && !dto.PumpDiaphragm && !dto.PumpOther)
        return "Należy wybrać co najmniej jeden typ pompy";

    return null;
}

// Helper: check if string is 4-digit year
static bool IsFourDigitYear(string? year)
{
    if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
        return false;

    foreach (var ch in year)
    {
        if (!char.IsDigit(ch))
            return false;
    }

    return true;
}

// Helper: map Machine entity to detail DTO
static MachineDetailDto ToMachineDetailDto(Machine m)
{
    return new MachineDetailDto
    {
        SerialNumber = m.SerialNumber,
        SprayerName = m.SprayerName,
        Type = m.Type,
        Kind = m.Kind,
        Manufacturer = m.Manufacturer,
        ProductionYear = m.ProductionYear,
        PurchaseDate = m.PurchaseDate,
        PumpPiston = m.PumpPiston,
        PumpDiaphragm = m.PumpDiaphragm,
        PumpOther = m.PumpOther,
        PumpOtherType = m.PumpOtherType,
        PumpFlowRate = m.PumpFlowRate,
        TankCapacity = m.TankCapacity,
        HasFlushing = m.HasFlushing,
        HasDiluter = m.HasDiluter,
        HasWashingDevice = m.HasWashingDevice,
        HasManometer = m.HasManometer,
        HasComputer = m.HasComputer,
        BoomWidth = m.BoomWidth,
        BoomWet = m.BoomWet,
        BoomDry = m.BoomDry,
        BoomDampeningMechanism = m.BoomDampeningMechanism,
        SectionCount = m.SectionCount,
        NozzlesFieldFeatures = m.NozzlesFieldFeatures,
        NozzlesGardenFeatures = m.NozzlesGardenFeatures,
        FanType = m.FanType,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
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
        return Results.NotFound(new { message = "Uzytkownik nie znaleziony" });

    // Prevent deleting self
    if (currentUserId == userId)
        return Results.BadRequest(new { message = "Nie mozesz usunac wlasnego konta" });

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
        return Results.BadRequest(new { message = "Tylko master administrator moze usuwac administrator�w" });

    // Verify password
    var passwordService = context.RequestServices.GetRequiredService<Server.Services.IPasswordService>();
    if (!passwordService.VerifyPassword(dto.Password, currentUser.PasswordHash))
    {
        return Results.BadRequest(new { message = "Bledne haslo" });
    }

    // Delete user
    db.Users.Remove(userToDelete);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = $"Uzytkownik '{userToDelete.Login}' zostal usuniety" });
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
        return Results.BadRequest(new { message = "Tylko master administrator moze zmieniać role użytkowników" });

    // Get the user to update
    var userToUpdate = await db.Users.FindAsync(userId);
    if (userToUpdate == null)
        return Results.NotFound(new { message = "Uzytkownik nie znaleziony" });

    // Prevent changing self role
    if (currentUserId == userId)
        return Results.BadRequest(new { message = "Nie mozesz zmienic swojej roli" });

    // Validate role value
    if (dto.Role != "admin" && dto.Role != "user")
        return Results.BadRequest(new { message = "Niepoprawna rola. Dozwolone wartosci: 'admin', 'user'" });

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

// ==================== MACHINES ENDPOINTS ====================

// Get machines list with filters
app.MapGet("/api/machines", async (AppDbContext db, HttpContext context,
    [FromQuery] string? q,
    [FromQuery] string? type,
    [FromQuery] string? kind,
    [FromQuery] string? manufacturer,
    [FromQuery] string? yearFrom,
    [FromQuery] string? yearTo) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var queryable = db.Machines.AsQueryable();

    if (!string.IsNullOrWhiteSpace(q))
    {
        var term = q.Trim().ToLowerInvariant();
        queryable = queryable.Where(m =>
            m.SerialNumber.ToLower().Contains(term) ||
            m.SprayerName.ToLower().Contains(term) ||
            m.Manufacturer.ToLower().Contains(term) ||
            m.ProductionYear.ToLower().Contains(term));
    }

    if (!string.IsNullOrWhiteSpace(type))
    {
        queryable = queryable.Where(m => m.Type == type);
    }

    if (!string.IsNullOrWhiteSpace(kind))
    {
        queryable = queryable.Where(m => m.Kind == kind);
    }

    if (!string.IsNullOrWhiteSpace(manufacturer))
    {
        var man = manufacturer.Trim().ToLowerInvariant();
        queryable = queryable.Where(m => m.Manufacturer.ToLower().Contains(man));
    }

    if (IsFourDigitYear(yearFrom))
    {
        queryable = queryable.Where(m => string.Compare(m.ProductionYear, yearFrom, StringComparison.Ordinal) >= 0);
    }

    if (IsFourDigitYear(yearTo))
    {
        queryable = queryable.Where(m => string.Compare(m.ProductionYear, yearTo, StringComparison.Ordinal) <= 0);
    }

    var list = await queryable
        .OrderByDescending(m => m.CreatedAt)
        .Select(m => new MachineListItemDto
        {
            SerialNumber = m.SerialNumber,
            SprayerName = m.SprayerName,
            Manufacturer = m.Manufacturer,
            ProductionYear = m.ProductionYear,
            Type = m.Type,
            Kind = m.Kind,
            CreatedAt = m.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(list);
}).RequireAuthorization();

// Get single machine details
app.MapGet("/api/machines/{serialNumber}", async (AppDbContext db, HttpContext context, string serialNumber) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var machine = await db.Machines.FindAsync(serialNumber);
    if (machine == null)
    {
        return Results.NotFound();
    }

    var detail = ToMachineDetailDto(machine);
    return Results.Ok(detail);
}).RequireAuthorization();

// Create new machine
app.MapPost("/api/machines", async (AppDbContext db, HttpContext context, [FromBody] MachineCreateUpdateDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var validationError = ValidateMachineDto(dto, isCreate: true);
    if (validationError != null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var normalizedSerial = dto.SerialNumber.Trim();

    if (await db.Machines.AnyAsync(m => m.SerialNumber == normalizedSerial))
    {
        return Results.BadRequest(new { message = "Maszyna o podanym numerze już istnieje" });
    }

    var machine = new Machine
    {
        SerialNumber = normalizedSerial,
        SprayerName = dto.SprayerName.Trim(),
        Type = dto.Type,
        Kind = dto.Kind,
        Manufacturer = dto.Manufacturer.Trim(),
        ProductionYear = dto.ProductionYear,
        PurchaseDate = dto.PurchaseDate,
        PumpPiston = dto.PumpPiston,
        PumpDiaphragm = dto.PumpDiaphragm,
        PumpOther = dto.PumpOther,
        PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim(),
        PumpFlowRate = dto.PumpFlowRate,
        TankCapacity = dto.TankCapacity,
        HasFlushing = dto.HasFlushing,
        HasDiluter = dto.HasDiluter,
        HasWashingDevice = dto.HasWashingDevice,
        HasManometer = dto.HasManometer,
        HasComputer = dto.HasComputer,
        BoomWidth = dto.BoomWidth,
        BoomWet = dto.BoomWet,
        BoomDry = dto.BoomDry,
        BoomDampeningMechanism = dto.BoomDampeningMechanism,
        SectionCount = dto.SectionCount,
        NozzlesFieldFeatures = dto.NozzlesFieldFeatures,
        NozzlesGardenFeatures = dto.NozzlesGardenFeatures,
        FanType = dto.FanType,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = null
    };

    db.Machines.Add(machine);
    await db.SaveChangesAsync();

    var detail = ToMachineDetailDto(machine);
    return Results.Created($"/api/machines/{machine.SerialNumber}", detail);
}).RequireAuthorization();

// Update existing machine
app.MapPut("/api/machines/{serialNumber}", async (AppDbContext db, HttpContext context, string serialNumber, [FromBody] MachineCreateUpdateDto dto) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    // serialNumber z trasy reprezentuje aktualny numer w bazie (stary),
    // dto.SerialNumber może zawierać nowy numer po edycji.
    var machine = await db.Machines.FindAsync(serialNumber);
    if (machine == null)
    {
        return Results.NotFound();
    }

    var validationError = ValidateMachineDto(dto, isCreate: false);
    if (validationError != null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    // Zmiana numeru seryjnego / ewidencyjnego z zachowaniem unikalności.
    // Dla prostoty i pełnej zgodności z SQLite, w przypadku zmiany numeru
    // tworzymy nową encję z nowym kluczem i usuwamy starą.
    var newSerial = dto.SerialNumber.Trim();
    var isSerialChanged = !string.Equals(machine.SerialNumber, newSerial, StringComparison.Ordinal);

    if (isSerialChanged)
    {
        var serialExists = await db.Machines.AnyAsync(m => m.SerialNumber == newSerial);
        if (serialExists)
        {
            return Results.BadRequest(new { message = "Maszyna o podanym numerze już istnieje" });
        }

        var newMachine = new Machine
        {
            SerialNumber = newSerial,
            SprayerName = dto.SprayerName.Trim(),
            Type = dto.Type,
            Kind = dto.Kind,
            Manufacturer = dto.Manufacturer.Trim(),
            ProductionYear = dto.ProductionYear,
            PurchaseDate = dto.PurchaseDate,
            PumpPiston = dto.PumpPiston,
            PumpDiaphragm = dto.PumpDiaphragm,
            PumpOther = dto.PumpOther,
            PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim(),
            PumpFlowRate = dto.PumpFlowRate,
            TankCapacity = dto.TankCapacity,
            HasFlushing = dto.HasFlushing,
            HasDiluter = dto.HasDiluter,
            HasWashingDevice = dto.HasWashingDevice,
            HasManometer = dto.HasManometer,
            HasComputer = dto.HasComputer,
            BoomWidth = dto.BoomWidth,
            BoomWet = dto.BoomWet,
            BoomDry = dto.BoomDry,
            BoomDampeningMechanism = dto.BoomDampeningMechanism,
            SectionCount = dto.SectionCount,
            NozzlesFieldFeatures = dto.NozzlesFieldFeatures,
            NozzlesGardenFeatures = dto.NozzlesGardenFeatures,
            FanType = dto.FanType,
            CreatedAt = machine.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        db.Machines.Remove(machine);
        db.Machines.Add(newMachine);

        await db.SaveChangesAsync();

        var newDetail = ToMachineDetailDto(newMachine);
        return Results.Ok(newDetail);
    }
    else
    {
        machine.SprayerName = dto.SprayerName.Trim();
        machine.Type = dto.Type;
        machine.Kind = dto.Kind;
        machine.Manufacturer = dto.Manufacturer.Trim();
        machine.ProductionYear = dto.ProductionYear;
        machine.PurchaseDate = dto.PurchaseDate;
        machine.PumpPiston = dto.PumpPiston;
        machine.PumpDiaphragm = dto.PumpDiaphragm;
        machine.PumpOther = dto.PumpOther;
        machine.PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim();
        machine.PumpFlowRate = dto.PumpFlowRate;
        machine.TankCapacity = dto.TankCapacity;
        machine.HasFlushing = dto.HasFlushing;
        machine.HasDiluter = dto.HasDiluter;
        machine.HasWashingDevice = dto.HasWashingDevice;
        machine.HasManometer = dto.HasManometer;
        machine.HasComputer = dto.HasComputer;
        machine.BoomWidth = dto.BoomWidth;
        machine.BoomWet = dto.BoomWet;
        machine.BoomDry = dto.BoomDry;
        machine.BoomDampeningMechanism = dto.BoomDampeningMechanism;
        machine.SectionCount = dto.SectionCount;
        machine.NozzlesFieldFeatures = dto.NozzlesFieldFeatures;
        machine.NozzlesGardenFeatures = dto.NozzlesGardenFeatures;
        machine.FanType = dto.FanType;
        machine.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var detail = ToMachineDetailDto(machine);
        return Results.Ok(detail);
    }
}).RequireAuthorization();

// Delete machine
app.MapDelete("/api/machines/{serialNumber}", async (AppDbContext db, HttpContext context, string serialNumber) =>
{
    if (!await IsSessionActiveAsync(db, context))
    {
        return Results.Unauthorized();
    }

    var machine = await db.Machines.FindAsync(serialNumber);
    if (machine == null)
    {
        return Results.NotFound();
    }

    db.Machines.Remove(machine);
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
