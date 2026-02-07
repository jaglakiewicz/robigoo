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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

#endregion

namespace Server.Controllers
{
    /// <summary>
    /// Controller for user management operations.
    /// </summary>
    /// <remarks>
    /// Authorization Requirements:
    /// - Requirement 8.2: Non-admin users get 403 for admin-only operations
    /// - Requirement 8.4: Users can only modify their own profile data unless admin
    /// 
    /// Property 23: Role-Based Access Control
    /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
    /// 
    /// Property 24: Resource Ownership Authorization
    /// For any resource with an owner, non-admin users SHALL only be able to access/modify resources they own.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly Services.IAuthorizationService _authorizationService;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly IFileValidationService _fileValidationService;
        private readonly ILogger<UsersController> _logger;
        private const int MIN_PASSWORD_LENGTH = 6;
        private const long MAX_AVATAR_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] ALLOWED_AVATAR_CONTENT_TYPES = new[] { "image/jpeg", "image/png", "image/gif" };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the UsersController.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="passwordService">The password service for hashing and verification.</param>
        /// <param name="authorizationService">The authorization service for access control checks.</param>
        /// <param name="securityAuditService">The security audit service for logging sensitive data access.</param>
        /// <param name="fileValidationService">The file validation service for validating file uploads.</param>
        /// <param name="logger">The logger for this controller.</param>
        public UsersController(
            AppDbContext context, 
            IPasswordService passwordService,
            Services.IAuthorizationService authorizationService,
            ISecurityAuditService securityAuditService,
            IFileValidationService fileValidationService,
            ILogger<UsersController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _authorizationService = authorizationService;
            _securityAuditService = securityAuditService;
            _fileValidationService = fileValidationService;
            _logger = logger;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <summary>
        /// Get all active users (admin only)
        /// </summary>
        /// <remarks>
        /// Requirement 8.2: Non-admin users get 403 for admin-only operations
        /// Requirement 9.7: Log sensitive data access for compliance purposes
        /// 
        /// Property 23: Role-Based Access Control
        /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetUsers()
        {
            var currentUserId = GetCurrentUserId();
            
            // Log sensitive data access for compliance
            // Requirement 9.7: IF sensitive data is accessed, THEN THE Backend SHALL log the access for compliance purposes
            await _securityAuditService.LogDataAccessAsync(currentUserId, "User", "all", "ListAll");
            
            var users = _context.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Login,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Phone,
                    u.PermissionNumber,
                    u.Role,
                    u.CreatedAt
                })
                .ToList();

            return Ok(users);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Login i hasło są wymagane" });

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return BadRequest(new { message = "Imię i nazwisko są wymagane" });

            // Validate password length
            if (dto.Password.Length < MIN_PASSWORD_LENGTH)
                return BadRequest(new { message = $"Hasło musi mieć co najmniej {MIN_PASSWORD_LENGTH} znaków" });

            // Check if login already exists
            if (_context.Users.Any(u => u.Login == dto.Login))
                return BadRequest(new { message = "Użytkownik z takim loginem już istnieje" });

            var user = new User
            {
                Login = dto.Login,
                PasswordHash = _passwordService.HashPassword(dto.Password),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email?.Trim() ?? string.Empty,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                PermissionNumber = dto.PermissionNumber?.Trim() ?? string.Empty,
                Role = dto.Role == "admin" ? "admin" : "user",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Language = "pl",
                Theme = "light"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new
            {
                user.Id,
                user.Login,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                user.PermissionNumber,
                user.Role
            });
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        /// <remarks>
        /// Requirement 9.7: Log sensitive data access for compliance purposes
        /// </remarks>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = _context.Users.Find(userId);

            if (user == null || !user.IsActive)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            // Log sensitive data access for compliance
            // Requirement 9.7: IF sensitive data is accessed, THEN THE Backend SHALL log the access for compliance purposes
            await _securityAuditService.LogDataAccessAsync(userId, "User", userId.ToString(), "ViewProfile");

            var avatarBase64 = user.AvatarData != null 
                ? Convert.ToBase64String(user.AvatarData) 
                : null;

            return Ok(new UserProfileResponseDto
            {
                UserId = user.Id,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PermissionNumber = user.PermissionNumber,
                Language = user.Language,
                Theme = user.Theme,
                AvatarBase64 = avatarBase64
            });
        }

        /// <summary>
        /// Update current user's profile (first name, last name, email, phone, etc.)
        /// </summary>
        /// <remarks>
        /// Requirement 8.4: Users can only modify their own profile data unless admin
        /// 
        /// Property 24: Resource Ownership Authorization
        /// For any resource with an owner, non-admin users SHALL only be able to access/modify resources they own.
        /// </remarks>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return BadRequest(new { message = "Imię i nazwisko są wymagane" });

            var userId = GetCurrentUserId();
            
            // Use authorization service to verify user can modify their own profile
            // Requirement 8.4: Users can only modify their own profile data unless admin
            var canModify = await _authorizationService.CanModifyUserAsync(userId, userId);
            if (!canModify)
            {
                _logger.LogWarning(
                    "User {UserId} denied permission to modify their own profile",
                    userId);
                return StatusCode(403, new { message = "Brak uprawnień do modyfikacji profilu" });
            }
            
            var user = _context.Users.Find(userId);

            if (user == null || !user.IsActive)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Email = dto.Email?.Trim() ?? string.Empty;
            user.Phone = dto.Phone?.Trim() ?? string.Empty;
            user.PermissionNumber = dto.PermissionNumber?.Trim() ?? string.Empty;
            user.Language = dto.Language ?? "pl";
            user.Theme = dto.Theme ?? "light";

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} successfully updated their profile",
                userId);

            var avatarBase64 = user.AvatarData != null 
                ? Convert.ToBase64String(user.AvatarData) 
                : null;

            return Ok(new UserProfileResponseDto
            {
                UserId = user.Id,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PermissionNumber = user.PermissionNumber,
                Language = user.Language,
                Theme = user.Theme,
                AvatarBase64 = avatarBase64
            });
        }

        /// <summary>
        /// Update another user's profile (admin only)
        /// </summary>
        /// <remarks>
        /// Requirement 8.2: Non-admin users get 403 for admin-only operations
        /// Requirement 8.4: Users can only modify their own profile data unless admin
        /// 
        /// Property 23: Role-Based Access Control
        /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
        /// 
        /// Property 24: Resource Ownership Authorization
        /// For any resource with an owner, non-admin users SHALL only be able to access/modify resources they own.
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto dto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return BadRequest(new { message = "Imię i nazwisko są wymagane" });

            var currentUserId = GetCurrentUserId();
            
            // Use authorization service to verify admin can modify this user
            // Requirement 8.4: Users can only modify their own profile data unless admin
            // Property 24: Resource Ownership Authorization
            var canModify = await _authorizationService.CanModifyUserAsync(currentUserId, id);
            if (!canModify)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} denied permission to modify user {TargetUserId}",
                    currentUserId, id);
                return StatusCode(403, new { message = "Brak uprawnień do modyfikacji tego użytkownika" });
            }
            
            var user = _context.Users.Find(id);

            if (user == null || !user.IsActive)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Email = dto.Email?.Trim() ?? string.Empty;
            user.Phone = dto.Phone?.Trim() ?? string.Empty;
            user.PermissionNumber = dto.PermissionNumber?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Admin {CurrentUserId} successfully updated user {TargetUserId}",
                currentUserId, id);

            return Ok(new
            {
                user.Id,
                user.Login,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                user.PermissionNumber,
                user.Role
            });
        }

        /// <summary>
        /// Change password for current user
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Stare hasło i nowe hasło są wymagane" });

            if (dto.NewPassword.Length < MIN_PASSWORD_LENGTH)
                return BadRequest(new { message = $"Nowe hasło musi mieć co najmniej {MIN_PASSWORD_LENGTH} znaków" });

            var userId = GetCurrentUserId();
            var user = _context.Users.Find(userId);

            if (user == null || !user.IsActive)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            // Verify old password
            if (!_passwordService.VerifyPassword(dto.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Stare hasło jest niepoprawne" });

            // Update password
            user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Hasło zmieniono pomyślnie" });
        }

        /// <summary>
        /// Upload avatar for current user (with password confirmation)
        /// </summary>
        /// <remarks>
        /// Requirement 6.6: THE Backend SHALL validate file uploads (type, size, content) server-side before processing
        /// 
        /// Property 17: File Upload Validation
        /// For any file upload, the Backend SHALL validate: file size is within configured limits,
        /// content type matches allowed types, and file content matches the declared type (magic bytes validation).
        /// 
        /// This endpoint validates:
        /// 1. File presence and non-empty
        /// 2. File size (max 5MB)
        /// 3. Content type (JPEG, PNG, GIF)
        /// 4. Magic bytes to verify actual file type matches declared content type
        /// </remarks>
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            // Validate file presence
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Plik nie został przesłany" });
            }

            // Use FileValidationService for comprehensive validation including magic bytes
            // Requirement 6.6: Validate file uploads (type, size, content) server-side
            var validationResult = await _fileValidationService.ValidateFileAsync(
                file, 
                ALLOWED_AVATAR_CONTENT_TYPES, 
                MAX_AVATAR_SIZE);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Avatar upload validation failed: {ErrorCode} - {ErrorMessage}",
                    validationResult.ErrorCode,
                    validationResult.ErrorMessage);
                
                return BadRequest(new { message = validationResult.ErrorMessage });
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    var userId = GetCurrentUserId();
                    var user = _context.Users.Find(userId);

                    if (user == null || !user.IsActive)
                    {
                        return NotFound(new { message = "Użytkownik nie znaleziony" });
                    }

                    user.AvatarData = fileBytes;
                    await _context.SaveChangesAsync();

                    var avatarBase64 = Convert.ToBase64String(fileBytes);

                    _logger.LogInformation(
                        "User {UserId} successfully uploaded avatar. DetectedType: {DetectedType}, Size: {Size}",
                        userId,
                        validationResult.DetectedContentType,
                        fileBytes.Length);

                    return Ok(new
                    {
                        message = "Avatar przesłany pomyślnie",
                        avatarBase64 = avatarBase64
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user");
                return StatusCode(500, new { message = "Błąd podczas przesyłania avatara" });
            }
        }

        /// <summary>
        /// Check if current user can delete another user
        /// </summary>
        /// <remarks>
        /// Requirement 8.2: Non-admin users get 403 for admin-only operations
        /// Requirement 8.5: Only admins can delete users
        /// 
        /// Property 23: Role-Based Access Control
        /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
        /// </remarks>
        [HttpGet("{id}/can-delete")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CanDeleteUser(long id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = _context.Users.Find(currentUserId);
            var userToDelete = _context.Users.Find(id);

            if (userToDelete == null)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            // Use authorization service to check if user can delete
            // Requirement 8.5: Only admins can delete users
            var canDelete = await _authorizationService.CanDeleteUserAsync(currentUserId, id);
            
            if (!canDelete)
            {
                // Determine the specific reason for denial
                if (currentUserId == id)
                {
                    return Ok(new CanDeleteUserResponseDto
                    {
                        CanDelete = false,
                        Reason = "Nie możesz usunąć swojego konta"
                    });
                }
                
                return Ok(new CanDeleteUserResponseDto
                {
                    CanDelete = false,
                    Reason = "Brak uprawnień do usuwania użytkowników"
                });
            }

            // Additional check: Regular admin cannot delete another admin (only master admin can)
            var isMasterAdmin = currentUser?.Login == "admin";
            if (!isMasterAdmin && userToDelete.Role == "admin")
            {
                return Ok(new CanDeleteUserResponseDto
                {
                    CanDelete = false,
                    Reason = "Zwykły administrator nie może usunąć innego administratora"
                });
            }

            return Ok(new CanDeleteUserResponseDto
            {
                CanDelete = true,
                Reason = "OK"
            });
        }

        /// <summary>
        /// Delete user with password confirmation
        /// </summary>
        /// <remarks>
        /// Requirement 8.2: Non-admin users get 403 for admin-only operations
        /// Requirement 8.5: Only admins can delete users
        /// 
        /// Property 23: Role-Based Access Control
        /// For any protected API endpoint requiring admin role, requests from non-admin users SHALL receive HTTP 403 Forbidden.
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(long id, [FromBody] ConfirmPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Hasło jest wymagane" });

            var currentUserId = GetCurrentUserId();
            var currentUser = _context.Users.Find(currentUserId);
            var userToDelete = _context.Users.Find(id);

            if (userToDelete == null)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            // Use authorization service to check if user can delete
            // Requirement 8.5: Only admins can delete users
            // Property 23: Role-Based Access Control
            var canDelete = await _authorizationService.CanDeleteUserAsync(currentUserId, id);
            if (!canDelete)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} denied permission to delete user {TargetUserId}",
                    currentUserId, id);
                return StatusCode(403, new { message = "Brak uprawnień do usunięcia tego użytkownika" });
            }

            // Additional check: Regular admin cannot delete another admin (only master admin can)
            var isMasterAdmin = currentUser?.Login == "admin";
            if (!isMasterAdmin && userToDelete.Role == "admin")
            {
                _logger.LogWarning(
                    "Non-master admin {CurrentUserId} attempted to delete admin user {TargetUserId}",
                    currentUserId, id);
                return BadRequest(new { message = "Zwykły administrator nie może usunąć innego administratora" });
            }

            // Verify current user's password
            if (currentUser == null || !_passwordService.VerifyPassword(dto.Password, currentUser.PasswordHash))
                return BadRequest(new { message = "Hasło jest niepoprawne" });

            // Soft delete
            userToDelete.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {CurrentUserId} successfully deleted user {TargetUserId}",
                currentUserId, id);

            return Ok(new { message = "Użytkownik został usunięty" });
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Helper method to get current user ID from JWT claims
        /// </summary>
        private long GetCurrentUserId()
        {
            // First try the custom "userId" claim used by TokenService
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                return userId;
            
            // Fallback to standard NameIdentifier claim
            var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdClaim != null && long.TryParse(nameIdClaim.Value, out var nameIdUserId))
                return nameIdUserId;

            throw new InvalidOperationException("Nie można pobrać ID użytkownika z tokena");
        }

        #endregion
    }
}

