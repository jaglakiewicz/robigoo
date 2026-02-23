using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// Result of checking for existing active sessions.
    /// </summary>
    public class SessionCheckResult
    {
        /// <summary>
        /// Indicates whether an active session exists for the user.
        /// </summary>
        public bool HasActiveSession { get; set; }
        
        /// <summary>
        /// The existing active session, if any.
        /// </summary>
        public UserSession? ExistingSession { get; set; }
        
        /// <summary>
        /// Indicates whether the user needs to force login to take over the session.
        /// </summary>
        public bool RequiresForceLogin { get; set; }
    }

    /// <summary>
    /// Service for managing user sessions.
    /// Implements proper session detection, invalidation, and cleanup.
    /// </summary>
    /// <remarks>
    /// Requirements:
    /// - 3.1: Check for existing active sessions for a specific user only
    /// - 3.2: Only show conflict when an active session actually exists
    /// - 3.3: Proceed with login without conflict warnings when no active session exists
    /// - 3.4: Properly invalidate sessions on logout by setting IsActive to false
    /// - 3.5: Clean up expired sessions based on token expiration times
    /// - 3.6: Exclude sessions where the refresh token has expired
    /// - 3.7: Mark sessions as inactive if last activity exceeds configured timeout
    /// </remarks>
    public interface ISessionManagementService
    {
        /// <summary>
        /// Checks for existing active sessions for a user.
        /// Only returns conflict when a truly active session exists.
        /// </summary>
        /// <param name="userId">The user ID to check sessions for.</param>
        /// <returns>Session check result indicating if an active session exists.</returns>
        /// <remarks>
        /// Validates: Requirements 3.1, 3.2, 3.3, 3.6, 3.7
        /// </remarks>
        Task<SessionCheckResult> CheckExistingSessionsAsync(long userId);

        /// <summary>
        /// Creates a new session for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="accessToken">The JWT access token.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="ipAddress">The client IP address.</param>
        /// <param name="userAgent">The client user agent.</param>
        /// <returns>The created session.</returns>
        Task<UserSession> CreateSessionAsync(long userId, string accessToken, string refreshToken, string ipAddress, string userAgent);

        /// <summary>
        /// Invalidates a specific session by its token.
        /// </summary>
        /// <param name="sessionToken">The session token to invalidate.</param>
        /// <remarks>
        /// Validates: Requirement 3.4
        /// </remarks>
        Task InvalidateSessionAsync(string sessionToken);

        /// <summary>
        /// Invalidates all sessions for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <remarks>
        /// Validates: Requirement 3.4
        /// </remarks>
        Task InvalidateAllUserSessionsAsync(long userId);

        /// <summary>
        /// Cleans up expired sessions based on token expiration times.
        /// </summary>
        /// <remarks>
        /// Validates: Requirements 3.5, 3.6
        /// </remarks>
        Task CleanupExpiredSessionsAsync();

        /// <summary>
        /// Checks if a session is valid (active and not expired).
        /// </summary>
        /// <param name="sessionToken">The session token to validate.</param>
        /// <returns>True if the session is valid, false otherwise.</returns>
        Task<bool> IsSessionValidAsync(string sessionToken);
    }
}
