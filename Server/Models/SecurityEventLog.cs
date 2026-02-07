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
    /// Types of security events that can be logged.
    /// </summary>
    /// <remarks>
    /// Requirement 7.2: Log all authentication attempts with IP address and user agent
    /// Used for tracking suspicious activity patterns.
    /// </remarks>
    public enum SecurityEventType
    {
        /// <summary>
        /// Detected SQL injection attempt in user input.
        /// </summary>
        SqlInjectionAttempt,
        
        /// <summary>
        /// Rate limit exceeded for an IP or user.
        /// </summary>
        RateLimitExceeded,
        
        /// <summary>
        /// Invalid or expired token was used.
        /// </summary>
        InvalidTokenUsed,
        
        /// <summary>
        /// Attempt to escalate privileges detected.
        /// </summary>
        PrivilegeEscalationAttempt,
        
        /// <summary>
        /// General suspicious activity detected.
        /// </summary>
        SuspiciousActivity,
        
        /// <summary>
        /// Multiple failed login attempts from the same IP.
        /// </summary>
        MultipleFailedLoginsFromIp,
        
        /// <summary>
        /// Login attempt for a non-existent user.
        /// </summary>
        NonExistentUserLogin,
        
        /// <summary>
        /// Rapid succession of login attempts detected.
        /// </summary>
        RapidLoginAttempts,
        
        /// <summary>
        /// Account lockout triggered.
        /// </summary>
        AccountLockout,
        
        /// <summary>
        /// Authorization failure - user denied access to a resource.
        /// </summary>
        /// <remarks>
        /// Requirement 8.7: Log all authorization failures with user ID and requested resource.
        /// </remarks>
        AuthorizationFailure,
        
        /// <summary>
        /// Sensitive data was accessed by a user.
        /// </summary>
        /// <remarks>
        /// Requirement 9.7: IF sensitive data is accessed, THEN THE Backend SHALL log the access for compliance purposes.
        /// </remarks>
        SensitiveDataAccess
    }

    /// <summary>
    /// Tracks security events for auditing and monitoring.
    /// </summary>
    /// <remarks>
    /// Requirement 7.2: Log all authentication attempts with IP address and user agent
    /// Used for detecting and logging suspicious activity patterns.
    /// </remarks>
    public class SecurityEventLog
    {
        public long Id { get; set; }
        
        [Required]
        public SecurityEventType EventType { get; set; }
        
        [MaxLength(500)]
        public string? Details { get; set; }
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        public long? UserId { get; set; }
        
        [MaxLength(100)]
        public string? Login { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(100)]
        public string? CorrelationId { get; set; }
        
        /// <summary>
        /// Additional metadata about the event (JSON format).
        /// </summary>
        [MaxLength(2000)]
        public string? Metadata { get; set; }
    }
}
