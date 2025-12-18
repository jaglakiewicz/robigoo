/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

#endregion

namespace Server.Models
{
    #region DTOs - Auth & Users

    public class LoginDto
    {
        #region Properties

        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

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

        #endregion
    }

    public class CreateUserDto
    {
        #region Properties

        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PermissionNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "user";

        #endregion
    }

    public class UpdateUserDto
    {
        #region Properties

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PermissionNumber { get; set; } = string.Empty;

        #endregion
    }

    public class ChangePasswordDto
    {
        #region Properties

        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;

        #endregion
    }

    public class ConfirmPasswordDto
    {
        #region Properties

        public string Password { get; set; } = string.Empty;

        #endregion
    }

    public class UpdateProfileDto
    {
        #region Properties

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PermissionNumber { get; set; } = string.Empty;
        public string Language { get; set; } = "pl";
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

        #endregion
    }

    public class CanDeleteUserResponseDto
    {
        #region Properties

        public bool CanDelete { get; set; }
        public string Reason { get; set; } = string.Empty;

        #endregion
    }

    #endregion
}
