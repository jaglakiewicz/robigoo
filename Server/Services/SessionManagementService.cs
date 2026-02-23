using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// Implementation of session management service.
    /// Fixes the bug where false "interrupting other session" warnings were shown.
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
    public class SessionManagementService : ISessionManagementService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SessionManagementService> _logger;
        
        /// <summary>
        /// Default session timeout in minutes if not configured.
        /// </summary>
        private const int DefaultSessionTimeoutMinutes = 30;

        public SessionManagementService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<SessionManagementService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method fixes the bug where users were incorrectly shown the
        /// "interrupting other session" dialog when no active session existed.
        /// 
        /// A session is considered truly active only when:
        /// 1. IsActive is true
        /// 2. InvalidatedAt is null (not explicitly invalidated)
        /// 3. RefreshTokenExpiresAt is null OR in the future (not expired)
        /// 4. LastActivityAt + SessionTimeoutMinutes > now (not timed out)
        /// 
        /// Validates: Requirements 3.1, 3.2, 3.3, 3.6, 3.7
        /// </remarks>
        public async Task<SessionCheckResult> CheckExistingSessionsAsync(long userId)
        {
            var now = DateTime.UtcNow;
            
            // Find truly active sessions (not expired, not invalidated)
            // Requirement 3.1: Check for existing active sessions for that user only
            // Requirement 3.6: Exclude sessions where the refresh token has expired
            var activeSession = await _context.UserSessions
                .Where(s => s.UserId == userId && 
                            s.IsActive && 
                            s.InvalidatedAt == null &&
                            (s.RefreshTokenExpiresAt == null || s.RefreshTokenExpiresAt > now))
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();
            
            if (activeSession == null)
            {
                // Requirement 3.3: Proceed with login without conflict warnings
                _logger.LogDebug("No active session found for user {UserId}", userId);
                return new SessionCheckResult
                {
                    HasActiveSession = false,
                    RequiresForceLogin = false
                };
            }
            
            // Requirement 3.7: Check if session has timed out due to inactivity
            var timeoutMinutes = activeSession.SessionTimeoutMinutes 
                ?? _configuration.GetValue<int>("Session:TimeoutMinutes", DefaultSessionTimeoutMinutes);
            var sessionTimeout = activeSession.LastActivityAt.AddMinutes(timeoutMinutes);
            
            if (now > sessionTimeout)
            {
                // Session has timed out - mark as inactive
                _logger.LogInformation(
                    "Session {SessionId} for user {UserId} has timed out (last activity: {LastActivity}, timeout: {Timeout} minutes)",
                    activeSession.Id, userId, activeSession.LastActivityAt, timeoutMinutes);
                
                activeSession.IsActive = false;
                activeSession.InvalidatedAt = now;
                activeSession.InvalidationReason = "Session timeout";
                await _context.SaveChangesAsync();
                
                // Requirement 3.3: Proceed with login without conflict warnings
                return new SessionCheckResult
                {
                    HasActiveSession = false,
                    RequiresForceLogin = false
                };
            }
            
            // Requirement 3.2: Only show conflict when an active session actually exists
            _logger.LogDebug(
                "Active session {SessionId} found for user {UserId} (last activity: {LastActivity})",
                activeSession.Id, userId, activeSession.LastActivityAt);
            
            return new SessionCheckResult
            {
                HasActiveSession = true,
                ExistingSession = activeSession,
                RequiresForceLogin = true
            };
        }

        /// <inheritdoc />
        public async Task<UserSession> CreateSessionAsync(
            long userId, 
            string accessToken, 
            string refreshToken, 
            string ipAddress, 
            string userAgent)
        {
            var now = DateTime.UtcNow;
            var refreshTokenExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
            var sessionTimeoutMinutes = _configuration.GetValue<int>("Session:TimeoutMinutes", DefaultSessionTimeoutMinutes);
            
            var session = new UserSession
            {
                UserId = userId,
                SessionToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = now.AddDays(refreshTokenExpirationDays),
                CreatedAt = now,
                LastActivityAt = now,
                IsActive = true,
                IpAddress = ipAddress?.Length > 50 ? ipAddress.Substring(0, 50) : ipAddress,
                UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                SessionTimeoutMinutes = sessionTimeoutMinutes
            };
            
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Created new session {SessionId} for user {UserId} from IP {IpAddress}",
                session.Id, userId, ipAddress);
            
            return session;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Validates: Requirement 3.4 - Properly invalidate sessions on logout
        /// </remarks>
        public async Task InvalidateSessionAsync(string sessionToken)
        {
            var now = DateTime.UtcNow;
            
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);
            
            if (session != null)
            {
                // Requirement 3.4: Set IsActive to false
                session.IsActive = false;
                session.InvalidatedAt = now;
                session.InvalidationReason = "Logout";
                session.LastActivityAt = now;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Invalidated session {SessionId} for user {UserId} (reason: Logout)",
                    session.Id, session.UserId);
            }
            else
            {
                _logger.LogDebug("Session not found or already inactive for token invalidation");
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Validates: Requirement 3.4 - Properly invalidate sessions on logout
        /// </remarks>
        public async Task InvalidateAllUserSessionsAsync(long userId)
        {
            var now = DateTime.UtcNow;
            
            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();
            
            foreach (var session in activeSessions)
            {
                // Requirement 3.4: Set IsActive to false
                session.IsActive = false;
                session.InvalidatedAt = now;
                session.InvalidationReason = "Force logout - all sessions";
                session.LastActivityAt = now;
            }
            
            if (activeSessions.Count > 0)
            {
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Invalidated {Count} sessions for user {UserId} (reason: Force logout - all sessions)",
                    activeSessions.Count, userId);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Validates: Requirements 3.5, 3.6
        /// - 3.5: Clean up expired sessions based on token expiration times
        /// - 3.6: Exclude sessions where the refresh token has expired
        /// </remarks>
        public async Task CleanupExpiredSessionsAsync()
        {
            var now = DateTime.UtcNow;
            var defaultTimeoutMinutes = _configuration.GetValue<int>("Session:TimeoutMinutes", DefaultSessionTimeoutMinutes);
            
            // Find sessions that should be cleaned up:
            // 1. Sessions where refresh token has expired (Requirement 3.5, 3.6)
            // 2. Sessions that have timed out due to inactivity (Requirement 3.7)
            var expiredSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.InvalidatedAt == null)
                .ToListAsync();
            
            var cleanedCount = 0;
            
            foreach (var session in expiredSessions)
            {
                var shouldCleanup = false;
                string reason = string.Empty;
                
                // Check if refresh token has expired (Requirement 3.5, 3.6)
                if (session.RefreshTokenExpiresAt != null && session.RefreshTokenExpiresAt <= now)
                {
                    shouldCleanup = true;
                    reason = "Refresh token expired";
                }
                // Check if session has timed out (Requirement 3.7)
                else
                {
                    var timeoutMinutes = session.SessionTimeoutMinutes ?? defaultTimeoutMinutes;
                    var sessionTimeout = session.LastActivityAt.AddMinutes(timeoutMinutes);
                    
                    if (now > sessionTimeout)
                    {
                        shouldCleanup = true;
                        reason = "Session timeout";
                    }
                }
                
                if (shouldCleanup)
                {
                    session.IsActive = false;
                    session.InvalidatedAt = now;
                    session.InvalidationReason = reason;
                    cleanedCount++;
                }
            }
            
            if (cleanedCount > 0)
            {
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Cleaned up {Count} expired sessions",
                    cleanedCount);
            }
            else
            {
                _logger.LogDebug("No expired sessions to clean up");
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsSessionValidAsync(string sessionToken)
        {
            var now = DateTime.UtcNow;
            
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
            
            if (session == null)
            {
                return false;
            }
            
            // Check if session is active and not invalidated
            if (!session.IsActive || session.InvalidatedAt != null)
            {
                return false;
            }
            
            // Check if refresh token has expired (Requirement 3.6)
            if (session.RefreshTokenExpiresAt != null && session.RefreshTokenExpiresAt <= now)
            {
                return false;
            }
            
            // Check if session has timed out (Requirement 3.7)
            var timeoutMinutes = session.SessionTimeoutMinutes 
                ?? _configuration.GetValue<int>("Session:TimeoutMinutes", DefaultSessionTimeoutMinutes);
            var sessionTimeout = session.LastActivityAt.AddMinutes(timeoutMinutes);
            
            if (now > sessionTimeout)
            {
                return false;
            }
            
            return true;
        }
    }
}
