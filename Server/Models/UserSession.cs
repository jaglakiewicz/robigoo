using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class UserSession
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        
        [MaxLength(2000)]
        public string SessionToken { get; set; } = string.Empty;
        
        [MaxLength(256)]
        public string? RefreshToken { get; set; }
        
        public DateTime? RefreshTokenExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsActive { get; set; }
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// Session timeout configuration in minutes. If null, uses the default system timeout.
        /// </summary>
        public int? SessionTimeoutMinutes { get; set; }
        
        /// <summary>
        /// Timestamp when the session was explicitly invalidated.
        /// </summary>
        public DateTime? InvalidatedAt { get; set; }
        
        /// <summary>
        /// Reason for session invalidation (e.g., "Logout", "Session timeout", "Force logout by admin").
        /// </summary>
        [MaxLength(500)]
        public string? InvalidationReason { get; set; }

        public User User { get; set; } = null!;
    }
}
