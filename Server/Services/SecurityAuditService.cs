/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Server.Data;
using Server.Models;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Server.Services
{
    #region Interfaces

    public interface ISecurityAuditService
    {
        Task LogLoginAttemptAsync(string login, string? ipAddress, string? userAgent, bool success, string? failureReason = null);
        Task<int> GetRecentFailedAttemptsCountAsync(string login, string? ipAddress, TimeSpan window);
        Task<bool> IsLockedOutAsync(string login, string? ipAddress);
    }

    #endregion

    public class SecurityAuditService : ISecurityAuditService
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly int _maxFailedAttempts;
        private readonly int _lockoutWindowMinutes;

        #endregion

        #region Constructor

        public SecurityAuditService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _maxFailedAttempts = configuration.GetValue<int>("RateLimiting:LoginPermitLimit", 5);
            _lockoutWindowMinutes = configuration.GetValue<int>("RateLimiting:LoginWindowMinutes", 1);
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Logs a login attempt for security auditing.
        /// </summary>
        public async Task LogLoginAttemptAsync(string login, string? ipAddress, string? userAgent, bool success, string? failureReason = null)
        {
            var attempt = new LoginAttempt
            {
                Login = login?.Trim().ToLowerInvariant() ?? "unknown",
                IpAddress = ipAddress?.Substring(0, Math.Min(ipAddress.Length, 50)),
                UserAgent = userAgent?.Substring(0, Math.Min(userAgent.Length, 500)),
                Success = success,
                FailureReason = failureReason,
                AttemptedAt = DateTime.UtcNow
            };

            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the count of recent failed login attempts.
        /// </summary>
        public async Task<int> GetRecentFailedAttemptsCountAsync(string login, string? ipAddress, TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;
            var normalizedLogin = login?.Trim().ToLowerInvariant() ?? "";

            return await _context.LoginAttempts
                .Where(a => !a.Success && a.AttemptedAt >= cutoff &&
                    (a.Login == normalizedLogin || a.IpAddress == ipAddress))
                .CountAsync();
        }

        /// <summary>
        /// Checks if a login/IP is currently locked out due to too many failed attempts.
        /// </summary>
        public async Task<bool> IsLockedOutAsync(string login, string? ipAddress)
        {
            var window = TimeSpan.FromMinutes(_lockoutWindowMinutes);
            var failedCount = await GetRecentFailedAttemptsCountAsync(login, ipAddress, window);
            return failedCount >= _maxFailedAttempts;
        }

        #endregion
    }
}
