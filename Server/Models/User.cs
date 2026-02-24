#region Imports

#endregion

namespace Server.Models
{
    #region User Model

    public class User
    {
        #region Declarations

        #endregion

        #region Constructor

        #endregion

        #region Properties

        public long Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PermissionNumber { get; set; } = string.Empty;
        public byte[]? AvatarData { get; set; }
        public byte[]? SignatureData { get; set; }
        public string Role { get; set; } = "user"; // "admin" lub "user"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string Language { get; set; } = "pl";
        public string Theme { get; set; } = "light";

        #endregion

        #region Methods - Public

        #endregion

        #region Methods - Private

        #endregion
    }

    #endregion
}
