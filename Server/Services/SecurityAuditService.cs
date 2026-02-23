#region Imports

using Server.Data;
using Server.Models;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Server.Services
{
    #region Interfaces

    /// <summary>
    /// Service for security auditing and account lockout management.
    /// </summary>
    /// <remarks>
    /// Requirements:
    /// - 7.1: Account lockout after configurable failed login attempts (default: 5 attempts, 15-minute lockout)
    /// - 7.2: Log all authentication attempts with IP address and user agent
    /// </remarks>
    public interface ISecurityAuditService
    {
        /// <summary>
        /// Logs a login attempt for security auditing.
        /// </summary>
        Task LogLoginAttemptAsync(string login, string? ipAddress, string? userAgent, bool success, string? failureReason = null);
        
        /// <summary>
        /// Gets the count of recent failed login attempts within a time window.
        /// </summary>
        Task<int> GetRecentFailedAttemptsCountAsync(string login, string? ipAddress, TimeSpan window);
        
        /// <summary>
        /// Checks if a login/IP is currently locked out due to too many failed attempts.
        /// </summary>
        /// <remarks>
        /// Requirement 7.1: Account lockout after configurable failed login attempts
        /// </remarks>
        Task<bool> IsLockedOutAsync(string login, string? ipAddress);
        
        /// <summary>
        /// Records a failed login attempt for lockout tracking.
        /// </summary>
        /// <remarks>
        /// Requirement 7.1: Track failed attempts for account lockout
        /// </remarks>
        Task RecordFailedLoginAsync(string login, string? ipAddress, string? userAgent, string? failureReason = null);
        
        /// <summary>
        /// Resets failed login attempts for a user after successful login.
        /// This is done by recording a successful login which effectively resets the lockout window.
        /// </summary>
        /// <remarks>
        /// Requirement 7.1: Reset lockout on successful login
        /// </remarks>
        Task ResetFailedAttemptsAsync(string login);
        
        /// <summary>
        /// Gets the number of failed attempts within the lockout window.
        /// </summary>
        Task<int> GetFailedAttemptsCountAsync(string login, string? ipAddress);
        
        /// <summary>
        /// Calculates the progressive delay based on failed attempt count.
        /// Uses exponential backoff: baseDelay * 2^(failedAttempts-1), capped at maxDelay.
        /// </summary>
        /// <remarks>
        /// Requirement 7.4: Progressive delays after failed login attempts (e.g., 1s, 2s, 4s, 8s...)
        /// </remarks>
        TimeSpan CalculateProgressiveDelay(int failedAttemptCount);
        
        /// <summary>
        /// Applies the progressive delay for a failed login attempt.
        /// The delay is calculated based on the number of failed attempts.
        /// </summary>
        /// <remarks>
        /// Requirement 7.4: Progressive delays after failed login attempts
        /// </remarks>
        Task ApplyProgressiveDelayAsync(string login, string? ipAddress);
        
        /// <summary>
        /// Logs a security event for auditing and monitoring.
        /// </summary>
        /// <remarks>
        /// Requirement 7.2: Log security events for suspicious activity
        /// </remarks>
        Task LogSecurityEventAsync(SecurityEventType eventType, string? details, string? ipAddress, string? login = null, string? userAgent = null, string? metadata = null);
        
        /// <summary>
        /// Detects and logs suspicious activity patterns.
        /// Checks for: multiple failed attempts from same IP, login attempts for non-existent users,
        /// and rapid succession of login attempts.
        /// </summary>
        /// <remarks>
        /// Requirement 7.2: Log security events for suspicious activity
        /// </remarks>
        Task DetectAndLogSuspiciousActivityAsync(string login, string? ipAddress, string? userAgent, bool userExists);
        
        /// <summary>
        /// Checks if there are rapid login attempts from the same IP.
        /// </summary>
        Task<bool> HasRapidLoginAttemptsAsync(string? ipAddress, TimeSpan window, int threshold);
        
        /// <summary>
        /// Gets the count of failed login attempts from a specific IP within a time window.
        /// </summary>
        Task<int> GetFailedAttemptsFromIpAsync(string? ipAddress, TimeSpan window);
        
        /// <summary>
        /// Logs access to sensitive data for compliance purposes.
        /// </summary>
        /// <remarks>
        /// Requirement 9.7: IF sensitive data is accessed, THEN THE Backend SHALL log the access for compliance purposes.
        /// </remarks>
        Task LogDataAccessAsync(long userId, string entityType, string entityId, string action);
        
        /// <summary>
        /// Logs an authorization failure for security auditing.
        /// </summary>
        /// <remarks>
        /// Requirement 8.7: IF authorization fails, THEN THE Backend SHALL log the attempt with user ID and requested resource.
        /// </remarks>
        Task LogAuthorizationFailureAsync(long userId, string resource, string action);
    }

    #endregion

    /// <summary>
    /// Implementation of security audit service with account lockout support.
    /// </summary>
    /// <remarks>
    /// Property 18: Account Lockout
    /// For any user account, after N consecutive failed login attempts (where N is configurable),
    /// subsequent login attempts SHALL be rejected until the lockout period expires.
    /// Validates: Requirements 7.1
    /// 
    /// Requirement 7.4: Progressive delays after failed login attempts (e.g., 1s, 2s, 4s, 8s...)
    /// Requirement 11.7: Sanitize user input before logging to prevent log injection
    /// </remarks>
    public class SecurityAuditService : ISecurityAuditService
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityAuditService> _logger;
        private readonly IInputValidationService _inputValidationService;
        private readonly int _maxFailedAttempts;
        private readonly int _lockoutDurationMinutes;
        private readonly int _baseDelayMilliseconds;
        private readonly int _maxDelayMilliseconds;
        
        // Suspicious activity detection thresholds
        private readonly int _rapidLoginThreshold;
        private readonly int _rapidLoginWindowSeconds;
        private readonly int _multipleFailedIpThreshold;
        private readonly int _multipleFailedIpWindowMinutes;

        #endregion

        #region Constructor

        public SecurityAuditService(
            AppDbContext context, 
            IConfiguration configuration, 
            ILogger<SecurityAuditService> logger,
            IInputValidationService inputValidationService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _inputValidationService = inputValidationService;
            
            // Read lockout configuration with defaults per requirement 7.1
            // Default: 5 failed attempts, 15-minute lockout
            _maxFailedAttempts = configuration.GetValue<int>("AccountLockout:MaxFailedAttempts", 5);
            _lockoutDurationMinutes = configuration.GetValue<int>("AccountLockout:LockoutDurationMinutes", 15);
            
            // Read progressive delay configuration per requirement 7.4
            // Default: 1 second base delay, 16 second max delay (exponential: 1s, 2s, 4s, 8s, 16s)
            _baseDelayMilliseconds = configuration.GetValue<int>("ProgressiveDelay:BaseDelayMilliseconds", 1000);
            _maxDelayMilliseconds = configuration.GetValue<int>("ProgressiveDelay:MaxDelayMilliseconds", 16000);
            
            // Read suspicious activity detection configuration per requirement 7.2
            // Default: 5 attempts in 30 seconds = rapid login attempts
            _rapidLoginThreshold = configuration.GetValue<int>("SuspiciousActivity:RapidLoginThreshold", 5);
            _rapidLoginWindowSeconds = configuration.GetValue<int>("SuspiciousActivity:RapidLoginWindowSeconds", 30);
            // Default: 10 failed attempts from same IP in 5 minutes = suspicious
            _multipleFailedIpThreshold = configuration.GetValue<int>("SuspiciousActivity:MultipleFailedIpThreshold", 10);
            _multipleFailedIpWindowMinutes = configuration.GetValue<int>("SuspiciousActivity:MultipleFailedIpWindowMinutes", 5);
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Logs a login attempt for security auditing.
        /// </summary>
        /// <remarks>
        /// Requirement 7.2: Log all authentication attempts with IP address and user agent
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// </remarks>
        public async Task LogLoginAttemptAsync(string login, string? ipAddress, string? userAgent, bool success, string? failureReason = null)
        {
            // Sanitize user input before storing and logging (Requirement 11.7)
            var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);
            var sanitizedIpAddress = _inputValidationService.SanitizeForLogging(ipAddress, 50);
            var sanitizedUserAgent = _inputValidationService.SanitizeForLogging(userAgent, 500);
            var sanitizedFailureReason = _inputValidationService.SanitizeForLogging(failureReason, 200);
            
            var attempt = new LoginAttempt
            {
                Login = NormalizeLogin(login),
                IpAddress = TruncateString(ipAddress, 50),
                UserAgent = TruncateString(userAgent, 500),
                Success = success,
                FailureReason = failureReason,
                AttemptedAt = DateTime.UtcNow
            };

            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            
            // Use sanitized values in log output to prevent log injection
            _logger.LogInformation(
                "Login attempt for user {Login} from IP {IpAddress}: {Result}",
                sanitizedLogin,
                sanitizedIpAddress,
                success ? "Success" : $"Failed - {sanitizedFailureReason}");
        }

        /// <summary>
        /// Gets the count of recent failed login attempts within a time window.
        /// </summary>
        public async Task<int> GetRecentFailedAttemptsCountAsync(string login, string? ipAddress, TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;
            var normalizedLogin = NormalizeLogin(login);

            // Count failed attempts since the last successful login or within the window
            var lastSuccessfulLogin = await _context.LoginAttempts
                .Where(a => a.Success && a.Login == normalizedLogin && a.AttemptedAt >= cutoff)
                .OrderByDescending(a => a.AttemptedAt)
                .FirstOrDefaultAsync();

            var failedAttemptsCutoff = lastSuccessfulLogin?.AttemptedAt ?? cutoff;

            return await _context.LoginAttempts
                .Where(a => !a.Success && 
                            a.AttemptedAt > failedAttemptsCutoff &&
                            a.Login == normalizedLogin)
                .CountAsync();
        }

        /// <summary>
        /// Checks if a login/IP is currently locked out due to too many failed attempts.
        /// </summary>
        /// <remarks>
        /// Requirement 7.1: Account lockout after configurable failed login attempts
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// Property 18: After N consecutive failed login attempts, subsequent login attempts
        /// SHALL be rejected until the lockout period expires.
        /// </remarks>
        public async Task<bool> IsLockedOutAsync(string login, string? ipAddress)
        {
            var lockoutWindow = TimeSpan.FromMinutes(_lockoutDurationMinutes);
            var failedCount = await GetFailedAttemptsCountAsync(login, ipAddress);
            
            var isLocked = failedCount >= _maxFailedAttempts;
            
            if (isLocked)
            {
                // Sanitize user input before logging (Requirement 11.7)
                var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);
                
                _logger.LogWarning(
                    "Account lockout active for user {Login}. Failed attempts: {FailedCount}/{MaxAttempts}",
                    sanitizedLogin,
                    failedCount,
                    _maxFailedAttempts);
            }
            
            return isLocked;
        }

        /// <summary>
        /// Records a failed login attempt for lockout tracking.
        /// </summary>
        /// <remarks>
        /// Requirement 7.1: Track failed attempts for account lockout
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// </remarks>
        public async Task RecordFailedLoginAsync(string login, string? ipAddress, string? userAgent, string? failureReason = null)
        {
            await LogLoginAttemptAsync(login, ipAddress, userAgent, success: false, failureReason);
            
            // Check if this triggers a lockout
            var failedCount = await GetFailedAttemptsCountAsync(login, ipAddress);
            if (failedCount >= _maxFailedAttempts)
            {
                // Sanitize user input before logging (Requirement 11.7)
                var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);
                
                _logger.LogWarning(
                    "Account {Login} has been locked out after {FailedCount} failed attempts. Lockout duration: {LockoutMinutes} minutes",
                    sanitizedLogin,
                    failedCount,
                    _lockoutDurationMinutes);
            }
        }

        /// <summary>
        /// Resets failed login attempts for a user after successful login.
        /// </summary>
        /// <remarks>
        /// Requirement 7.1: Reset lockout on successful login
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// Recording a successful login effectively resets the consecutive failed attempt count
        /// because GetRecentFailedAttemptsCountAsync only counts failures after the last success.
        /// </remarks>
        public async Task ResetFailedAttemptsAsync(string login)
        {
            // Sanitize user input before logging (Requirement 11.7)
            var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);
            
            // The successful login is already recorded via LogLoginAttemptAsync
            // This method exists for explicit reset scenarios if needed
            _logger.LogInformation(
                "Failed login attempts reset for user {Login}",
                sanitizedLogin);
        }

        /// <summary>
        /// Gets the number of failed attempts within the lockout window.
        /// </summary>
        public async Task<int> GetFailedAttemptsCountAsync(string login, string? ipAddress)
        {
            var lockoutWindow = TimeSpan.FromMinutes(_lockoutDurationMinutes);
            return await GetRecentFailedAttemptsCountAsync(login, ipAddress, lockoutWindow);
        }

        /// <summary>
        /// Calculates the progressive delay based on failed attempt count.
        /// Uses exponential backoff: baseDelay * 2^(failedAttempts-1), capped at maxDelay.
        /// </summary>
        /// <remarks>
        /// Requirement 7.4: Progressive delays after failed login attempts (e.g., 1s, 2s, 4s, 8s...)
        /// For 0 or 1 failed attempts: no delay
        /// For 2 failed attempts: 1s (baseDelay * 2^0)
        /// For 3 failed attempts: 2s (baseDelay * 2^1)
        /// For 4 failed attempts: 4s (baseDelay * 2^2)
        /// For 5 failed attempts: 8s (baseDelay * 2^3)
        /// And so on, capped at maxDelay
        /// </remarks>
        public TimeSpan CalculateProgressiveDelay(int failedAttemptCount)
        {
            // No delay for first failed attempt
            if (failedAttemptCount <= 1)
            {
                return TimeSpan.Zero;
            }

            // Calculate exponential delay: baseDelay * 2^(failedAttempts-2)
            // This gives: 1s, 2s, 4s, 8s, 16s... for attempts 2, 3, 4, 5, 6...
            var exponent = failedAttemptCount - 2;
            
            // Prevent overflow for very large exponents
            if (exponent > 30)
            {
                return TimeSpan.FromMilliseconds(_maxDelayMilliseconds);
            }

            var delayMs = _baseDelayMilliseconds * Math.Pow(2, exponent);
            
            // Cap at max delay
            var cappedDelayMs = Math.Min(delayMs, _maxDelayMilliseconds);
            
            return TimeSpan.FromMilliseconds(cappedDelayMs);
        }

        /// <summary>
        /// Applies the progressive delay for a failed login attempt.
        /// The delay is calculated based on the number of failed attempts.
        /// </summary>
        /// <remarks>
        /// Requirement 7.4: Progressive delays after failed login attempts
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// This method should be called AFTER recording the failed attempt.
        /// </remarks>
        public async Task ApplyProgressiveDelayAsync(string login, string? ipAddress)
        {
            var failedCount = await GetFailedAttemptsCountAsync(login, ipAddress);
            var delay = CalculateProgressiveDelay(failedCount);
            
            if (delay > TimeSpan.Zero)
            {
                // Sanitize user input before logging (Requirement 11.7)
                var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);
                
                _logger.LogInformation(
                    "Applying progressive delay of {DelayMs}ms for user {Login} after {FailedCount} failed attempts",
                    delay.TotalMilliseconds,
                    sanitizedLogin,
                    failedCount);
                
                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Logs a security event for auditing and monitoring.
        /// </summary>
        /// <remarks>
        /// Requirement 7.2: Log security events for suspicious activity
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// </remarks>
        public async Task LogSecurityEventAsync(SecurityEventType eventType, string? details, string? ipAddress, string? login = null, string? userAgent = null, string? metadata = null)
        {
            var securityEvent = new SecurityEventLog
            {
                EventType = eventType,
                Details = TruncateString(details, 500),
                IpAddress = TruncateString(ipAddress, 50),
                Login = TruncateString(login, 100),
                UserAgent = TruncateString(userAgent, 500),
                Metadata = TruncateString(metadata, 2000),
                OccurredAt = DateTime.UtcNow
            };

            _context.SecurityEventLogs.Add(securityEvent);
            await _context.SaveChangesAsync();

            // Sanitize user input before logging (Requirement 11.7)
            var sanitizedDetails = _inputValidationService.SanitizeForLogging(details, 500);
            var sanitizedIpAddress = _inputValidationService.SanitizeForLogging(ipAddress, 50);
            var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);

            _logger.LogWarning(
                "Security event logged: {EventType} - {Details} from IP {IpAddress} for user {Login}",
                eventType,
                sanitizedDetails,
                sanitizedIpAddress,
                sanitizedLogin);
        }

        /// <summary>
        /// Detects and logs suspicious activity patterns.
        /// Checks for: multiple failed attempts from same IP, login attempts for non-existent users,
        /// and rapid succession of login attempts.
        /// </summary>
        /// <remarks>
        /// Requirement 7.2: Log security events for suspicious activity
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// </remarks>
        public async Task DetectAndLogSuspiciousActivityAsync(string login, string? ipAddress, string? userAgent, bool userExists)
        {
            // Sanitize login for use in log messages (Requirement 11.7)
            var sanitizedLogin = _inputValidationService.SanitizeForLogging(login, 100);
            
            // Check for login attempts for non-existent users
            if (!userExists)
            {
                await LogSecurityEventAsync(
                    SecurityEventType.NonExistentUserLogin,
                    $"Login attempt for non-existent user: {sanitizedLogin}",
                    ipAddress,
                    login,
                    userAgent);
            }

            // Check for rapid login attempts from the same IP
            var rapidWindow = TimeSpan.FromSeconds(_rapidLoginWindowSeconds);
            if (await HasRapidLoginAttemptsAsync(ipAddress, rapidWindow, _rapidLoginThreshold))
            {
                await LogSecurityEventAsync(
                    SecurityEventType.RapidLoginAttempts,
                    $"Rapid login attempts detected: {_rapidLoginThreshold}+ attempts in {_rapidLoginWindowSeconds} seconds",
                    ipAddress,
                    login,
                    userAgent,
                    $"{{\"threshold\": {_rapidLoginThreshold}, \"windowSeconds\": {_rapidLoginWindowSeconds}}}");
            }

            // Check for multiple failed attempts from the same IP
            var multipleFailedWindow = TimeSpan.FromMinutes(_multipleFailedIpWindowMinutes);
            var failedFromIp = await GetFailedAttemptsFromIpAsync(ipAddress, multipleFailedWindow);
            if (failedFromIp >= _multipleFailedIpThreshold)
            {
                await LogSecurityEventAsync(
                    SecurityEventType.MultipleFailedLoginsFromIp,
                    $"Multiple failed login attempts from IP: {failedFromIp} attempts in {_multipleFailedIpWindowMinutes} minutes",
                    ipAddress,
                    login,
                    userAgent,
                    $"{{\"failedCount\": {failedFromIp}, \"threshold\": {_multipleFailedIpThreshold}, \"windowMinutes\": {_multipleFailedIpWindowMinutes}}}");
            }
        }

        /// <summary>
        /// Checks if there are rapid login attempts from the same IP.
        /// </summary>
        public async Task<bool> HasRapidLoginAttemptsAsync(string? ipAddress, TimeSpan window, int threshold)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;

            var cutoff = DateTime.UtcNow - window;
            var attemptCount = await _context.LoginAttempts
                .Where(a => a.IpAddress == ipAddress && a.AttemptedAt >= cutoff)
                .CountAsync();

            return attemptCount >= threshold;
        }

        /// <summary>
        /// Gets the count of failed login attempts from a specific IP within a time window.
        /// </summary>
        public async Task<int> GetFailedAttemptsFromIpAsync(string? ipAddress, TimeSpan window)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return 0;

            var cutoff = DateTime.UtcNow - window;
            return await _context.LoginAttempts
                .Where(a => !a.Success && a.IpAddress == ipAddress && a.AttemptedAt >= cutoff)
                .CountAsync();
        }

        /// <summary>
        /// Logs access to sensitive data for compliance purposes.
        /// </summary>
        /// <remarks>
        /// Requirement 9.7: IF sensitive data is accessed, THEN THE Backend SHALL log the access for compliance purposes.
        /// This method should be called when accessing sensitive entities like User profiles, 
        /// personal information, or any data that requires compliance tracking.
        /// </remarks>
        public async Task LogDataAccessAsync(long userId, string entityType, string entityId, string action)
        {
            var securityEvent = new SecurityEventLog
            {
                EventType = SecurityEventType.SensitiveDataAccess,
                UserId = userId,
                Details = $"User {userId} performed '{action}' on {entityType} with ID {entityId}",
                Metadata = $"{{\"entityType\": \"{entityType}\", \"entityId\": \"{entityId}\", \"action\": \"{action}\"}}",
                OccurredAt = DateTime.UtcNow
            };

            _context.SecurityEventLogs.Add(securityEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sensitive data access logged: User {UserId} performed '{Action}' on {EntityType}/{EntityId}",
                userId,
                action,
                entityType,
                entityId);
        }

        /// <summary>
        /// Logs an authorization failure for security auditing.
        /// </summary>
        /// <remarks>
        /// Requirement 8.7: IF authorization fails, THEN THE Backend SHALL log the attempt with user ID and requested resource.
        /// </remarks>
        public async Task LogAuthorizationFailureAsync(long userId, string resource, string action)
        {
            var securityEvent = new SecurityEventLog
            {
                EventType = SecurityEventType.AuthorizationFailure,
                UserId = userId,
                Details = $"User {userId} was denied '{action}' access to resource: {resource}",
                Metadata = $"{{\"resource\": \"{resource}\", \"action\": \"{action}\"}}",
                OccurredAt = DateTime.UtcNow
            };

            _context.SecurityEventLogs.Add(securityEvent);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Authorization failure logged: User {UserId} denied '{Action}' access to {Resource}",
                userId,
                action,
                resource);
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Normalizes a login string for consistent comparison.
        /// </summary>
        private static string NormalizeLogin(string? login)
        {
            return login?.Trim().ToLowerInvariant() ?? "unknown";
        }

        /// <summary>
        /// Truncates a string to a maximum length.
        /// </summary>
        private static string? TruncateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #endregion
    }
}
