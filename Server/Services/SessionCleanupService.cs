/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Server.Services
{
    /// <summary>
    /// Background service that periodically cleans up expired sessions.
    /// </summary>
    /// <remarks>
    /// Validates: Requirement 3.5 - THE Session_Manager SHALL clean up expired sessions based on token expiration times
    /// 
    /// This service runs as a hosted background service and periodically calls
    /// ISessionManagementService.CleanupExpiredSessionsAsync() to remove expired sessions.
    /// 
    /// Configuration:
    /// - Session:CleanupIntervalMinutes - The interval between cleanup runs (default: 15 minutes)
    /// </remarks>
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly IConfiguration _configuration;
        
        /// <summary>
        /// Default cleanup interval in minutes if not configured.
        /// </summary>
        private const int DefaultCleanupIntervalMinutes = 15;

        public SessionCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<SessionCleanupService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Executes the background cleanup task.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the service.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session cleanup service is starting");

            // Get cleanup interval from configuration
            var cleanupIntervalMinutes = _configuration.GetValue<int>(
                "Session:CleanupIntervalMinutes", 
                DefaultCleanupIntervalMinutes);
            
            var cleanupInterval = TimeSpan.FromMinutes(cleanupIntervalMinutes);
            
            _logger.LogInformation(
                "Session cleanup service configured with interval of {IntervalMinutes} minutes",
                cleanupIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessionsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Service is stopping, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during session cleanup");
                }

                try
                {
                    await Task.Delay(cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Service is stopping, exit gracefully
                    break;
                }
            }

            _logger.LogInformation("Session cleanup service is stopping");
        }

        /// <summary>
        /// Performs the actual cleanup of expired sessions.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting session cleanup cycle");

            // Create a new scope to get scoped services
            using var scope = _scopeFactory.CreateScope();
            var sessionManagementService = scope.ServiceProvider.GetRequiredService<ISessionManagementService>();

            await sessionManagementService.CleanupExpiredSessionsAsync();

            _logger.LogDebug("Completed session cleanup cycle");
        }
    }
}
