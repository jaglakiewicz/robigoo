#region Imports

using System.ComponentModel.DataAnnotations;

#endregion

namespace Server.Models
{
    #region DTOs - Auth & Users

    /// <summary>
    /// DTO for user login requests.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class LoginDto
    {
        #region Properties

        /// <summary>
        /// User login name.
        /// </summary>
        [Required(ErrorMessage = "Login is required")]
        [MaxLength(100, ErrorMessage = "Login cannot exceed 100 characters")]
        [MinLength(3, ErrorMessage = "Login must be at least 3 characters")]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// User password.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Force login even if another session exists.
        /// </summary>
        public bool Force { get; set; } = false;

        #endregion
    }

    public class LoginResponseDto
    {
        #region Properties

        public long UserId { get; set; }
        public string Login { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PermissionNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? AvatarBase64 { get; set; }
        public string? SignatureBase64 { get; set; }

        #endregion
    }

    /// <summary>
    /// DTO for creating a new user.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class CreateUserDto
    {
        #region Properties

        /// <summary>
        /// User login name.
        /// </summary>
        [Required(ErrorMessage = "Login is required")]
        [MaxLength(100, ErrorMessage = "Login cannot exceed 100 characters")]
        [MinLength(3, ErrorMessage = "Login must be at least 3 characters")]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// User password.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// User's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address.
        /// </summary>
        [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's phone number.
        /// </summary>
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// User's permission/license number.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Permission number cannot exceed 50 characters")]
        public string PermissionNumber { get; set; } = string.Empty;

        /// <summary>
        /// User role: "user", "admin", or "master_admin".
        /// </summary>
        [MaxLength(20, ErrorMessage = "Role cannot exceed 20 characters")]
        [RegularExpression("^(user|admin|master_admin)?$", ErrorMessage = "Role must be 'user', 'admin', or 'master_admin'")]
        public string Role { get; set; } = "user";

        #endregion
    }

    /// <summary>
    /// DTO for updating an existing user.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class UpdateUserDto
    {
        #region Properties

        /// <summary>
        /// User's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's phone number.
        /// </summary>
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// User's permission/license number.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Permission number cannot exceed 50 characters")]
        public string PermissionNumber { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// DTO for changing user password.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class ChangePasswordDto
    {
        #region Properties

        /// <summary>
        /// Current password.
        /// </summary>
        [Required(ErrorMessage = "Current password is required")]
        [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// New password.
        /// </summary>
        [Required(ErrorMessage = "New password is required")]
        [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// DTO for password confirmation.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class ConfirmPasswordDto
    {
        #region Properties

        /// <summary>
        /// Password to confirm.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
        public string Password { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// DTO for updating user profile.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class UpdateProfileDto
    {
        #region Properties

        /// <summary>
        /// User's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's phone number.
        /// </summary>
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// User's permission/license number.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Permission number cannot exceed 50 characters")]
        public string PermissionNumber { get; set; } = string.Empty;

        /// <summary>
        /// User's preferred language.
        /// </summary>
        [Required(ErrorMessage = "Language is required")]
        [MaxLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
        [RegularExpression("^(pl|en|de)$", ErrorMessage = "Language must be 'pl', 'en', or 'de'")]
        public string Language { get; set; } = "pl";

        /// <summary>
        /// User's preferred theme.
        /// </summary>
        [Required(ErrorMessage = "Theme is required")]
        [MaxLength(10, ErrorMessage = "Theme cannot exceed 10 characters")]
        [RegularExpression("^(light|dark)$", ErrorMessage = "Theme must be 'light' or 'dark'")]
        public string Theme { get; set; } = "light";

        #endregion
    }

    public class UserProfileResponseDto
    {
        #region Properties

        public long UserId { get; set; }
        public string Login { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PermissionNumber { get; set; } = string.Empty;
        public string Language { get; set; } = "pl";
        public string Theme { get; set; } = "light";
        public string? AvatarBase64 { get; set; }
        public string? SignatureBase64 { get; set; }

        #endregion
    }

    public class CanDeleteUserResponseDto
    {
        #region Properties

        public bool CanDelete { get; set; }
        public string Reason { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// DTO for updating user role.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class UpdateUserRoleDto
    {
        #region Properties

        /// <summary>
        /// New role for the user.
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        [MaxLength(20, ErrorMessage = "Role cannot exceed 20 characters")]
        [RegularExpression("^(user|admin|master_admin)$", ErrorMessage = "Role must be 'user', 'admin', or 'master_admin'")]
        public string Role { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// DTO for token refresh requests.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class RefreshTokenDto
    {
        #region Properties

        /// <summary>
        /// Current access token.
        /// </summary>
        [Required(ErrorMessage = "Access token is required")]
        [MaxLength(2000, ErrorMessage = "Access token cannot exceed 2000 characters")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Current refresh token.
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required")]
        [MaxLength(256, ErrorMessage = "Refresh token cannot exceed 256 characters")]
        public string RefreshToken { get; set; } = string.Empty;

        #endregion
    }

    public class TokenResponseDto
    {
        #region Properties

        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }

        #endregion
    }

    public class AuthResponseDto
    {
        #region Properties

        public TokenResponseDto Token { get; set; } = new();
        public LoginResponseDto User { get; set; } = new();

        #endregion
    }

    public class RefreshTokenResponseDto
    {
        #region Properties

        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }

        #endregion
    }

    #endregion
}
