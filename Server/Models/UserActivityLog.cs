using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public enum ActivityType
    {
        Login,
        Logout,
        Create,
        Update,
        Delete,
        View,
        Export,
        Import,
        PasswordChange,
        SettingsChange,
        FileUpload,
        FileDownload,
        SessionTerminated
    }

    public class UserActivityLog
    {
        public long Id { get; set; }
        
        public long UserId { get; set; }
        
        [MaxLength(100)]
        public string UserLogin { get; set; } = string.Empty;
        
        public ActivityType ActivityType { get; set; }
        
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? EntityId { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public User User { get; set; } = null!;
    }
}
