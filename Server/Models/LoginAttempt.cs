/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    /// <summary>
    /// Tracks login attempts for security auditing and brute-force protection.
    /// </summary>
    public class LoginAttempt
    {
        public long Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Login { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public bool Success { get; set; }
        
        [MaxLength(255)]
        public string? FailureReason { get; set; }
        
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    }
}
