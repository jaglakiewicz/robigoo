/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using System.Collections.Concurrent;
using Server.Models;

#endregion

namespace Server.Services
{
    #region Interfaces

    /// <summary>
    /// Service for tracking rate limit violations and implementing temporary blocking.
    /// </summary>
    /// <remarks>
    /// Requirement 12.6: IF sustained rate limit violations occur, THEN THE Backend SHALL temporarily block the source.
    /// This service tracks violations per IP/user and implements temporary blocking when violations exceed a threshold.
    /// </remarks>
    public interface IRateLimitBlockingService
    {
        /// <summary>
        /// Records a rate limit violation for the specified identifier (IP or user ID).
        /// If violations exceed the configured threshold, the identifier will be temporarily blocked.
        /// </summary>
        /// <param name="identifier">The IP address or user identifier that violated the rate limit.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordViolationAsync(string identifier);

        /// <summary>
        /// Checks if the specified identifier is currently blocked due to sustained rate limit violations.
        /// </summary>
        /// <param name="identifier">The IP address or user identifier to check.</param>
        /// <returns>True if the identifier is blocked; otherwise, false.</returns>
        Task<bool> IsBlockedAsync(string identifier);

        /// <summary>
        /// Gets the expiration time of the block for the specified identifier.
        /// </summary>
        /// <param name="identifier">The IP address or user identifier to check.</param>
        /// <returns>The UTC time when the block expires, or null if not blocked.</returns>
        Task<DateTime?> GetBlockExpirationAsync(string identifier);

        /// <summary>
        /// Gets the current violation count for the specified identifier.
        /// </summary>
        /// <param name="identifier">The IP address or user identifier to check.</param>
        /// <returns>The number of violations within the tracking window.</returns>
        Task<int> GetViolationCountAsync(string identifier);

        /// <summary>
        /// Clears the block for the specified identifier (for administrative purposes).
        /// </summary>
        /// <param name="identifier">The IP address or user identifier to unblock.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearBlockAsync(string identifier);
    }

    #endregion

    /// <summary>
    /// Implementation of rate limit blocking service with in-memory tracking.
    /// </summary>
    /// <remarks>
    /// Requirement 12.6: IF sustained rate limit violations occur, THEN THE Backend SHALL temporarily block the source.
    /// 
    /// This implementation uses in-memory storage for tracking violations and blocks.
    /// For production deployments with multiple instances, consider using a distributed cache like Redis.
    /// 
    /// Configuration options (from appsettings.json):
    /// - ViolationThreshold: Number of violations before blocking (default: 10)
    /// - BlockDurationMinutes: How long to block after threshold is exceeded (default: 30)
    /// - ViolationWindowMinutes: Time window for counting violations (default: 5)
    /// </remarks>
    public class RateLimitBlockingService : IRateLimitBlockingService
    {
        #region Declarations

        private readonly ILogger<RateLimitBlockingService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        
        // Configuration
        private readonly int _violationThreshold;
        private readonly int _blockDurationMinutes;
        private readonly int _violationWindowMinutes;

        // In-memory storage for violations and blocks
        // Key: identifier (IP or user ID), Value: list of violation timestamps
        private readonly ConcurrentDictionary<string, List<DateTime>> _violations = new();
        
        // Key: identifier, Value: block expiration time
        private readonly ConcurrentDictionary<string, DateTime> _blocks = new();

        // Lock object for thread-safe operations on violation lists
        private readonly object _violationLock = new();

        #endregion

        #region Constructor

        public RateLimitBlockingService(
            IConfiguration configuration,
            ILogger<RateLimitBlockingService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            // Read configuration with defaults
            // Default: 10 violations in 5 minutes = 30 minute block
            _violationThreshold = configuration.GetValue<int>("RateLimitBlocking:ViolationThreshold", 10);
            _blockDurationMinutes = configuration.GetValue<int>("RateLimitBlocking:BlockDurationMinutes", 30);
            _violationWindowMinutes = configuration.GetValue<int>("RateLimitBlocking:ViolationWindowMinutes", 5);

            _logger.LogInformation(
                "RateLimitBlockingService initialized with threshold={Threshold}, blockDuration={BlockDuration}min, window={Window}min",
                _violationThreshold,
                _blockDurationMinutes,
                _violationWindowMinutes);
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Records a rate limit violation for the specified identifier.
        /// </summary>
        /// <remarks>
        /// Requirement 12.6: Track sustained violations and implement temporary blocking.
        /// </remarks>
        public async Task RecordViolationAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-_violationWindowMinutes);
            var wasBlocked = false;

            // Clean up old violations and add new one
            lock (_violationLock)
            {
                var violations = _violations.GetOrAdd(identifier, _ => new List<DateTime>());
                
                // Remove violations outside the window
                violations.RemoveAll(v => v < windowStart);
                
                // Add new violation
                violations.Add(now);

                // Check if threshold exceeded
                if (violations.Count >= _violationThreshold)
                {
                    // Block the identifier
                    var blockExpiration = now.AddMinutes(_blockDurationMinutes);
                    _blocks[identifier] = blockExpiration;

                    // Clear violations after blocking (they've served their purpose)
                    violations.Clear();
                    wasBlocked = true;

                    // Sanitize identifier for logging (simple truncation for singleton context)
                    var sanitizedIdentifier = identifier.Length > 100 ? identifier.Substring(0, 100) : identifier;

                    _logger.LogWarning(
                        "Rate limit blocking activated for {Identifier}. Blocked until {BlockExpiration}. Violations: {ViolationCount}",
                        sanitizedIdentifier,
                        blockExpiration,
                        _violationThreshold);
                }
            }

            // Log the security event asynchronously using a scope
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var securityAuditService = scope.ServiceProvider.GetRequiredService<ISecurityAuditService>();
                var inputValidationService = scope.ServiceProvider.GetRequiredService<IInputValidationService>();
                var violationCount = await GetViolationCountAsync(identifier);
                var isBlocked = wasBlocked || await IsBlockedAsync(identifier);
                var sanitizedId = inputValidationService.SanitizeForLogging(identifier, 100);
                
                await securityAuditService.LogSecurityEventAsync(
                    SecurityEventType.RateLimitExceeded,
                    $"Rate limit violation recorded for: {sanitizedId}",
                    identifier.StartsWith("ip:") ? identifier.Substring(3) : null,
                    login: null,
                    userAgent: null,
                    $"{{\"identifier\": \"{sanitizedId}\", \"violationCount\": {violationCount}, \"isBlocked\": {isBlocked.ToString().ToLower()}}}");
            }
        }

        /// <summary>
        /// Checks if the specified identifier is currently blocked.
        /// </summary>
        /// <remarks>
        /// Requirement 12.6: Temporary blocking for sustained rate limit violations.
        /// </remarks>
        public Task<bool> IsBlockedAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return Task.FromResult(false);
            }

            if (_blocks.TryGetValue(identifier, out var expiration))
            {
                if (DateTime.UtcNow < expiration)
                {
                    return Task.FromResult(true);
                }
                
                // Block has expired, remove it
                _blocks.TryRemove(identifier, out _);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets the expiration time of the block for the specified identifier.
        /// </summary>
        public Task<DateTime?> GetBlockExpirationAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return Task.FromResult<DateTime?>(null);
            }

            if (_blocks.TryGetValue(identifier, out var expiration))
            {
                if (DateTime.UtcNow < expiration)
                {
                    return Task.FromResult<DateTime?>(expiration);
                }
                
                // Block has expired, remove it
                _blocks.TryRemove(identifier, out _);
            }

            return Task.FromResult<DateTime?>(null);
        }

        /// <summary>
        /// Gets the current violation count for the specified identifier.
        /// </summary>
        public Task<int> GetViolationCountAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return Task.FromResult(0);
            }

            var windowStart = DateTime.UtcNow.AddMinutes(-_violationWindowMinutes);

            lock (_violationLock)
            {
                if (_violations.TryGetValue(identifier, out var violations))
                {
                    // Count only violations within the window
                    return Task.FromResult(violations.Count(v => v >= windowStart));
                }
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Clears the block for the specified identifier.
        /// </summary>
        public Task ClearBlockAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return Task.CompletedTask;
            }

            _blocks.TryRemove(identifier, out _);
            
            lock (_violationLock)
            {
                if (_violations.TryGetValue(identifier, out var violations))
                {
                    violations.Clear();
                }
            }

            // Simple sanitization for logging (truncate to 100 chars)
            var sanitizedIdentifier = identifier.Length > 100 ? identifier.Substring(0, 100) : identifier;
            _logger.LogInformation("Block cleared for {Identifier}", sanitizedIdentifier);

            return Task.CompletedTask;
        }

        #endregion
    }
}
