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
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;
        private const int MIN_PASSWORD_LENGTH = 6;
        private const long MAX_AVATAR_SIZE = 5 * 1024 * 1024; // 5MB

        #endregion

        #region Constructor

        public UsersController(AppDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetUsers()
        {
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
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = _context.Users.Find(userId);

            if (user == null || !user.IsActive)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

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
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return BadRequest(new { message = "Imię i nazwisko są wymagane" });

            var userId = GetCurrentUserId();
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
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Plik nie został przesłany" });

            if (file.Length > MAX_AVATAR_SIZE)
                return BadRequest(new { message = "Plik jest za duży. Maksymalny rozmiar to 5MB" });

            // Validate file type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedContentTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Obsługiwane są tylko pliki: JPEG, PNG, GIF" });

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    var userId = GetCurrentUserId();
                    var user = _context.Users.Find(userId);

                    if (user == null || !user.IsActive)
                        return NotFound(new { message = "Użytkownik nie znaleziony" });

                    user.AvatarData = fileBytes;
                    await _context.SaveChangesAsync();

                    var avatarBase64 = Convert.ToBase64String(fileBytes);

                    return Ok(new
                    {
                        message = "Avatar przesłany pomyślnie",
                        avatarBase64 = avatarBase64
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Błąd podczas przesyłania avatara: {ex.Message}" });
            }
        }

        /// <summary>
        /// Check if current user can delete another user
        /// </summary>
        [HttpGet("{id}/can-delete")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CanDeleteUser(long id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = _context.Users.Find(currentUserId);
            var userToDelete = _context.Users.Find(id);

            if (userToDelete == null)
                return NotFound(new { message = "Użytkownik nie znaleziony" });

            // Cannot delete self
            if (currentUserId == id)
                return Ok(new CanDeleteUserResponseDto
                {
                    CanDelete = false,
                    Reason = "Nie możesz usunąć swojego konta"
                });

            // Check if current user is admin
            if (currentUser?.Role != "admin")
                return Ok(new CanDeleteUserResponseDto
                {
                    CanDelete = false,
                    Reason = "Brak uprawnień do usuwania użytkowników"
                });

            // Master admin (login: admin) can delete anyone
            if (currentUser.Login == "admin")
                return Ok(new CanDeleteUserResponseDto
                {
                    CanDelete = true,
                    Reason = "OK"
                });

            // Regular admin can only delete regular users (not admins)
            if (userToDelete.Role == "admin")
                return Ok(new CanDeleteUserResponseDto
                {
                    CanDelete = false,
                    Reason = "Zwykły administrator nie może usunąć innego administratora"
                });

            return Ok(new CanDeleteUserResponseDto
            {
                CanDelete = true,
                Reason = "OK"
            });
        }

        /// <summary>
        /// Delete user with password confirmation
        /// </summary>
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

            // Cannot delete self
            if (currentUserId == id)
                return BadRequest(new { message = "Nie możesz usunąć swojego konta" });

            // Check permissions
            if (currentUser?.Role != "admin")
                return Forbid();

            var isMasterAdmin = currentUser.Login == "admin";
            if (!isMasterAdmin && userToDelete.Role == "admin")
                return BadRequest(new { message = "Zwykły administrator nie może usunąć innego administratora" });

            // Verify current user's password
            if (!_passwordService.VerifyPassword(dto.Password, currentUser.PasswordHash))
                return BadRequest(new { message = "Hasło jest niepoprawne" });

            // Soft delete
            userToDelete.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Użytkownik został usunięty" });
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Helper method to get current user ID from JWT claims
        /// </summary>
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (long.TryParse(userIdClaim?.Value, out var userId))
                return userId;

            throw new InvalidOperationException("Nie można pobrać ID użytkownika z tokena");
        }

        #endregion
    }
}

