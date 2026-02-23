#region Imports

using System.Text.RegularExpressions;

#endregion

namespace Server.Services
{
    #region Interfaces

    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        PasswordValidationResult ValidatePasswordStrength(string password);
        bool NeedsRehash(string hash);
        bool IsValidBCryptHash(string hash);
        int? GetHashWorkFactor(string hash);
    }

    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    #endregion

    public class PasswordService : IPasswordService
    {
        #region Declarations

        private readonly IConfiguration _configuration;
        private readonly int _minLength;
        private readonly bool _requireUppercase;
        private readonly bool _requireLowercase;
        private readonly bool _requireDigit;
        private readonly bool _requireSpecialChar;

        // BCrypt work factor (cost) - 12 is recommended for 2024+
        private const int WorkFactor = 12;

        #endregion

        #region Constructor

        public PasswordService(IConfiguration configuration)
        {
            _configuration = configuration;
            _minLength = configuration.GetValue<int>("Security:PasswordMinLength", 8);
            _requireUppercase = configuration.GetValue<bool>("Security:RequireUppercase", true);
            _requireLowercase = configuration.GetValue<bool>("Security:RequireLowercase", true);
            _requireDigit = configuration.GetValue<bool>("Security:RequireDigit", true);
            _requireSpecialChar = configuration.GetValue<bool>("Security:RequireSpecialChar", false);
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Hashes password using BCrypt with automatic salt generation.
        /// </summary>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        /// <summary>
        /// Verifies password against BCrypt hash.
        /// Also supports legacy SHA256 hashes for migration.
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            // Check if it's a BCrypt hash (starts with $2a$, $2b$, or $2y$)
            if (hash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }

            // Legacy SHA256 support for migration
            // After successful login, the hash should be upgraded
            return VerifyLegacySha256(password, hash);
        }

        /// <summary>
        /// Validates password strength according to configured rules.
        /// </summary>
        public PasswordValidationResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(password))
            {
                result.IsValid = false;
                result.Errors.Add("Hasło jest wymagane");
                return result;
            }

            if (password.Length < _minLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Hasło musi mieć minimum {_minLength} znaków");
            }

            if (_requireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Hasło musi zawierać co najmniej jedną wielką literę");
            }

            if (_requireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Hasło musi zawierać co najmniej jedną małą literę");
            }

            if (_requireDigit && !Regex.IsMatch(password, @"\d"))
            {
                result.IsValid = false;
                result.Errors.Add("Hasło musi zawierać co najmniej jedną cyfrę");
            }

            if (_requireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
            {
                result.IsValid = false;
                result.Errors.Add("Hasło musi zawierać co najmniej jeden znak specjalny");
            }

            return result;
        }

        /// <summary>
        /// Checks if hash needs to be upgraded (legacy SHA256 or old BCrypt cost).
        /// </summary>
        public bool NeedsRehash(string hash)
        {
            // Legacy SHA256 hashes need rehash
            if (!hash.StartsWith("$2"))
            {
                return true;
            }

            // Check if BCrypt cost factor is outdated
            // BCrypt hash format: $2a$XX$... where XX is the cost
            try
            {
                var parts = hash.Split('$');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int cost))
                {
                    return cost < WorkFactor;
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates that a hash is in valid BCrypt format.
        /// Valid BCrypt hashes start with $2a$, $2b$, or $2y$ followed by the cost factor.
        /// </summary>
        public bool IsValidBCryptHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return false;
            }

            // BCrypt hash format: $2a$XX$22-character-salt22-character-hash
            // Valid prefixes are $2a$, $2b$, $2y$
            if (!hash.StartsWith("$2a$") && !hash.StartsWith("$2b$") && !hash.StartsWith("$2y$"))
            {
                return false;
            }

            // BCrypt hash should be exactly 60 characters
            if (hash.Length != 60)
            {
                return false;
            }

            // Validate the cost factor (should be 2 digits between 04 and 31)
            var parts = hash.Split('$');
            if (parts.Length < 4)
            {
                return false;
            }

            if (!int.TryParse(parts[2], out int cost) || cost < 4 || cost > 31)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts the work factor (cost) from a BCrypt hash.
        /// Returns null if the hash is not a valid BCrypt hash.
        /// </summary>
        public int? GetHashWorkFactor(string hash)
        {
            if (string.IsNullOrEmpty(hash) || !hash.StartsWith("$2"))
            {
                return null;
            }

            try
            {
                var parts = hash.Split('$');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int cost))
                {
                    return cost;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Verifies legacy SHA256 hash (for migration purposes only).
        /// </summary>
        private bool VerifyLegacySha256(string password, string hash)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var hashOfInput = Convert.ToBase64String(hashedBytes);
                return hashOfInput.Equals(hash);
            }
        }

        #endregion
    }
}
