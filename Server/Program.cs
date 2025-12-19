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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

#endregion

#region Application Build & Configuration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add authentication services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Add JWT Authentication
var jwtKey = "SuperSecretKeyForJWTTokenGenerationThisIsVeryLongAndSecure2025!";
var key = Encoding.ASCII.GetBytes(jwtKey);

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
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add Antiforgery service  
builder.Services.AddAntiforgery();

// Allow Angular dev server (or other clients) to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=app.db"));

var app = builder.Build();

// ensure database exists and seed master admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    db.Database.EnsureCreated();

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

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ==================== AUTH ENDPOINTS ====================
app.MapPost("/api/auth/login", async (AppDbContext db, IPasswordService passwordService, ITokenService tokenService, Server.Models.LoginDto dto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Login == dto.Login && u.IsActive);
    
    if (user == null || !passwordService.VerifyPassword(dto.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }
    // Update last login
    user.LastLoginAt = DateTime.UtcNow;
    
    // Generate token
    var token = tokenService.GenerateToken(user.Id, user.Login, user.Role);
    
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

    return Results.Ok(new { token, user = response });
});

app.MapGet("/api/auth/me", async (AppDbContext db, HttpContext context) =>
{
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

app.MapPost("/api/auth/logout", (HttpContext context) =>
{
    // Token will be removed on client side
    return Results.Ok();
}).RequireAuthorization();

// ==================== USER MANAGEMENT ENDPOINTS ====================
app.MapPost("/api/users", async (AppDbContext db, IPasswordService passwordService, HttpContext context, Server.Models.CreateUserDto dto) =>
{
    // Check if current user is admin
    var roleClaim = context.User.FindFirst(ClaimTypes.Role);
    if (roleClaim?.Value != "admin")
    {
        return Results.Forbid();
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
    var userIdClaim = context.User.FindFirst("userId");
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.FindAsync(userId);
    if (user == null)
        return Results.NotFound();

    if (!passwordService.VerifyPassword(dto.OldPassword, user.PasswordHash))
        return Results.BadRequest(new { message = "Stare haslo jest niepoprawne" });

    user.PasswordHash = passwordService.HashPassword(dto.NewPassword);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Haslo zostalo zmienione" });
}).RequireAuthorization();

app.MapPost("/api/users/avatar", async (AppDbContext db, HttpContext context, IFormFile file) =>
{
    try
    {
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

// ==================== ORIGINAL ENDPOINTS ====================

app.MapGet("/api/inspections", async (AppDbContext db) =>
{
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
    return response;
});

app.MapPost("/api/inspections", async (AppDbContext db, Server.Models.InspectionCreateDto dto) =>
{
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
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

#endregion
