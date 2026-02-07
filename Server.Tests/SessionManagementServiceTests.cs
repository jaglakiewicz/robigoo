/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Controllers;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Tests
{
    /// <summary>
    /// Unit tests for SessionManagementService.
    /// Tests the session bug fix: only return conflict when truly active session exists.
    /// 
    /// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7
    /// </summary>
    public class SessionManagementServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SessionManagementService _service;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<SessionManagementService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        public SessionManagementServiceTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _context = new AppDbContext(options, _httpContextAccessorMock.Object);

            // Set up configuration mock
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c.GetSection("Session:TimeoutMinutes").Value).Returns("30");
            _configurationMock.Setup(c => c.GetSection("Jwt:RefreshTokenExpirationDays").Value).Returns("7");

            // Set up logger mock
            _loggerMock = new Mock<ILogger<SessionManagementService>>();

            _service = new SessionManagementService(_context, _configurationMock.Object, _loggerMock.Object);

            // Seed a test user
            SeedTestUser();
        }

        private void SeedTestUser()
        {
            var user = new User
            {
                Id = 1,
                Login = "testuser",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Role = "user",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region CheckExistingSessionsAsync Tests

        /// <summary>
        /// Test: Login with no existing session succeeds without conflict.
        /// Validates: Requirements 3.2, 3.3
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_NoExistingSession_ReturnsNoConflict()
        {
            // Arrange - no sessions exist for user

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert
            Assert.False(result.HasActiveSession);
            Assert.False(result.RequiresForceLogin);
            Assert.Null(result.ExistingSession);
        }

        /// <summary>
        /// Test: Login with expired session (refresh token expired) succeeds without conflict.
        /// Validates: Requirements 3.3, 3.6
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_ExpiredRefreshToken_ReturnsNoConflict()
        {
            // Arrange - create a session with expired refresh token
            var expiredSession = new UserSession
            {
                UserId = 1,
                SessionToken = "expired-token",
                RefreshToken = "expired-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null
            };
            _context.UserSessions.Add(expiredSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert
            Assert.False(result.HasActiveSession);
            Assert.False(result.RequiresForceLogin);
            Assert.Null(result.ExistingSession);
        }

        /// <summary>
        /// Test: Login with timed out session (inactivity timeout) succeeds without conflict.
        /// Validates: Requirements 3.3, 3.7
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_TimedOutSession_ReturnsNoConflict()
        {
            // Arrange - create a session that has timed out due to inactivity
            var timedOutSession = new UserSession
            {
                UserId = 1,
                SessionToken = "timed-out-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7), // Still valid
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-60), // Last activity 60 minutes ago (> 30 min timeout)
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(timedOutSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert
            Assert.False(result.HasActiveSession);
            Assert.False(result.RequiresForceLogin);
            Assert.Null(result.ExistingSession);

            // Verify the session was marked as inactive
            var updatedSession = await _context.UserSessions.FindAsync(timedOutSession.Id);
            Assert.False(updatedSession!.IsActive);
            Assert.NotNull(updatedSession.InvalidatedAt);
            Assert.Equal("Session timeout", updatedSession.InvalidationReason);
        }

        /// <summary>
        /// Test: Login with active session returns conflict.
        /// Validates: Requirements 3.1, 3.2
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_ActiveSession_ReturnsConflict()
        {
            // Arrange - create a truly active session
            var activeSession = new UserSession
            {
                UserId = 1,
                SessionToken = "active-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7), // Still valid
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5), // Recent activity
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(activeSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert
            Assert.True(result.HasActiveSession);
            Assert.True(result.RequiresForceLogin);
            Assert.NotNull(result.ExistingSession);
            Assert.Equal(activeSession.Id, result.ExistingSession.Id);
        }

        /// <summary>
        /// Test: Login with invalidated session (IsActive=false) succeeds without conflict.
        /// Validates: Requirements 3.2, 3.3
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_InactiveSession_ReturnsNoConflict()
        {
            // Arrange - create an inactive session
            var inactiveSession = new UserSession
            {
                UserId = 1,
                SessionToken = "inactive-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = false, // Already inactive
                InvalidatedAt = DateTime.UtcNow.AddMinutes(-5),
                InvalidationReason = "Logout"
            };
            _context.UserSessions.Add(inactiveSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert
            Assert.False(result.HasActiveSession);
            Assert.False(result.RequiresForceLogin);
            Assert.Null(result.ExistingSession);
        }

        /// <summary>
        /// Test: Login with explicitly invalidated session succeeds without conflict.
        /// Validates: Requirements 3.2, 3.3
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_ExplicitlyInvalidatedSession_ReturnsNoConflict()
        {
            // Arrange - create a session that was explicitly invalidated
            var invalidatedSession = new UserSession
            {
                UserId = 1,
                SessionToken = "invalidated-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true, // Still marked active but explicitly invalidated
                InvalidatedAt = DateTime.UtcNow.AddMinutes(-2), // Explicitly invalidated
                InvalidationReason = "Force logout by admin"
            };
            _context.UserSessions.Add(invalidatedSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert
            Assert.False(result.HasActiveSession);
            Assert.False(result.RequiresForceLogin);
            Assert.Null(result.ExistingSession);
        }

        /// <summary>
        /// Test: Session check only considers sessions for the specific user.
        /// Validates: Requirement 3.1
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_OnlyChecksSpecificUser()
        {
            // Arrange - create another user and an active session for them
            var otherUser = new User
            {
                Id = 2,
                Login = "otheruser",
                PasswordHash = "hash",
                FirstName = "Other",
                LastName = "User",
                Email = "other@example.com",
                Role = "user",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(otherUser);

            var otherUserSession = new UserSession
            {
                UserId = 2, // Different user
                SessionToken = "other-user-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null
            };
            _context.UserSessions.Add(otherUserSession);
            await _context.SaveChangesAsync();

            // Act - check for user 1 (who has no sessions)
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert - should not find the other user's session
            Assert.False(result.HasActiveSession);
            Assert.False(result.RequiresForceLogin);
            Assert.Null(result.ExistingSession);
        }

        /// <summary>
        /// Test: Session with null RefreshTokenExpiresAt is considered active.
        /// Validates: Requirement 3.6
        /// </summary>
        [Fact]
        public async Task CheckExistingSessionsAsync_NullRefreshTokenExpiry_ConsideredActive()
        {
            // Arrange - create a session with null refresh token expiry
            var sessionWithNullExpiry = new UserSession
            {
                UserId = 1,
                SessionToken = "null-expiry-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = null, // No expiry set
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(sessionWithNullExpiry);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckExistingSessionsAsync(userId: 1);

            // Assert - session should be considered active
            Assert.True(result.HasActiveSession);
            Assert.True(result.RequiresForceLogin);
            Assert.NotNull(result.ExistingSession);
        }

        #endregion

        #region InvalidateSessionAsync Tests

        /// <summary>
        /// Test: Invalidating a session sets IsActive to false.
        /// Validates: Requirement 3.4
        /// </summary>
        [Fact]
        public async Task InvalidateSessionAsync_SetsIsActiveToFalse()
        {
            // Arrange
            var session = new UserSession
            {
                UserId = 1,
                SessionToken = "to-invalidate-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null
            };
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            await _service.InvalidateSessionAsync("to-invalidate-token");

            // Assert
            var updatedSession = await _context.UserSessions.FindAsync(session.Id);
            Assert.False(updatedSession!.IsActive);
            Assert.NotNull(updatedSession.InvalidatedAt);
            Assert.Equal("Logout", updatedSession.InvalidationReason);
        }

        /// <summary>
        /// Test: Invalidating a non-existent session does not throw.
        /// </summary>
        [Fact]
        public async Task InvalidateSessionAsync_NonExistentSession_DoesNotThrow()
        {
            // Act & Assert - should not throw
            await _service.InvalidateSessionAsync("non-existent-token");
        }

        #endregion

        #region InvalidateAllUserSessionsAsync Tests

        /// <summary>
        /// Test: Invalidating all user sessions sets IsActive to false for all.
        /// Validates: Requirement 3.4
        /// </summary>
        [Fact]
        public async Task InvalidateAllUserSessionsAsync_InvalidatesAllSessions()
        {
            // Arrange - create multiple sessions for the user
            var sessions = new[]
            {
                new UserSession
                {
                    UserId = 1,
                    SessionToken = "session-1",
                    RefreshToken = "refresh-1",
                    RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    LastActivityAt = DateTime.UtcNow.AddMinutes(-30),
                    IsActive = true,
                    InvalidatedAt = null
                },
                new UserSession
                {
                    UserId = 1,
                    SessionToken = "session-2",
                    RefreshToken = "refresh-2",
                    RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    LastActivityAt = DateTime.UtcNow.AddMinutes(-10),
                    IsActive = true,
                    InvalidatedAt = null
                }
            };
            _context.UserSessions.AddRange(sessions);
            await _context.SaveChangesAsync();

            // Act
            await _service.InvalidateAllUserSessionsAsync(userId: 1);

            // Assert
            var updatedSessions = await _context.UserSessions.Where(s => s.UserId == 1).ToListAsync();
            Assert.All(updatedSessions, s =>
            {
                Assert.False(s.IsActive);
                Assert.NotNull(s.InvalidatedAt);
                Assert.Equal("Force logout - all sessions", s.InvalidationReason);
            });
        }

        #endregion

        #region CleanupExpiredSessionsAsync Tests

        /// <summary>
        /// Test: Cleanup marks sessions with expired refresh tokens as inactive.
        /// Validates: Requirements 3.5, 3.6
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_MarksExpiredRefreshTokenSessionsInactive()
        {
            // Arrange
            var expiredSession = new UserSession
            {
                UserId = 1,
                SessionToken = "expired-refresh-token",
                RefreshToken = "expired-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null
            };
            _context.UserSessions.Add(expiredSession);
            await _context.SaveChangesAsync();

            // Act
            await _service.CleanupExpiredSessionsAsync();

            // Assert
            var updatedSession = await _context.UserSessions.FindAsync(expiredSession.Id);
            Assert.False(updatedSession!.IsActive);
            Assert.NotNull(updatedSession.InvalidatedAt);
            Assert.Equal("Refresh token expired", updatedSession.InvalidationReason);
        }

        /// <summary>
        /// Test: Cleanup marks sessions that have timed out as inactive.
        /// Validates: Requirement 3.7
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_MarksTimedOutSessionsInactive()
        {
            // Arrange
            var timedOutSession = new UserSession
            {
                UserId = 1,
                SessionToken = "timed-out-session",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7), // Still valid
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-60), // Timed out (> 30 min)
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(timedOutSession);
            await _context.SaveChangesAsync();

            // Act
            await _service.CleanupExpiredSessionsAsync();

            // Assert
            var updatedSession = await _context.UserSessions.FindAsync(timedOutSession.Id);
            Assert.False(updatedSession!.IsActive);
            Assert.NotNull(updatedSession.InvalidatedAt);
            Assert.Equal("Session timeout", updatedSession.InvalidationReason);
        }

        /// <summary>
        /// Test: Cleanup does not affect active sessions.
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_DoesNotAffectActiveSessions()
        {
            // Arrange
            var activeSession = new UserSession
            {
                UserId = 1,
                SessionToken = "active-session",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5), // Recent activity
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(activeSession);
            await _context.SaveChangesAsync();

            // Act
            await _service.CleanupExpiredSessionsAsync();

            // Assert
            var updatedSession = await _context.UserSessions.FindAsync(activeSession.Id);
            Assert.True(updatedSession!.IsActive);
            Assert.Null(updatedSession.InvalidatedAt);
        }

        #endregion

        #region IsSessionValidAsync Tests

        /// <summary>
        /// Test: Valid session returns true.
        /// </summary>
        [Fact]
        public async Task IsSessionValidAsync_ValidSession_ReturnsTrue()
        {
            // Arrange
            var validSession = new UserSession
            {
                UserId = 1,
                SessionToken = "valid-session-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(validSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.IsSessionValidAsync("valid-session-token");

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Test: Non-existent session returns false.
        /// </summary>
        [Fact]
        public async Task IsSessionValidAsync_NonExistentSession_ReturnsFalse()
        {
            // Act
            var result = await _service.IsSessionValidAsync("non-existent-token");

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Test: Inactive session returns false.
        /// </summary>
        [Fact]
        public async Task IsSessionValidAsync_InactiveSession_ReturnsFalse()
        {
            // Arrange
            var inactiveSession = new UserSession
            {
                UserId = 1,
                SessionToken = "inactive-session-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = false,
                InvalidatedAt = DateTime.UtcNow.AddMinutes(-2)
            };
            _context.UserSessions.Add(inactiveSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.IsSessionValidAsync("inactive-session-token");

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Test: Session with expired refresh token returns false.
        /// </summary>
        [Fact]
        public async Task IsSessionValidAsync_ExpiredRefreshToken_ReturnsFalse()
        {
            // Arrange
            var expiredSession = new UserSession
            {
                UserId = 1,
                SessionToken = "expired-refresh-session",
                RefreshToken = "expired-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null
            };
            _context.UserSessions.Add(expiredSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.IsSessionValidAsync("expired-refresh-session");

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Test: Timed out session returns false.
        /// </summary>
        [Fact]
        public async Task IsSessionValidAsync_TimedOutSession_ReturnsFalse()
        {
            // Arrange
            var timedOutSession = new UserSession
            {
                UserId = 1,
                SessionToken = "timed-out-session-token",
                RefreshToken = "valid-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-60), // Timed out
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(timedOutSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.IsSessionValidAsync("timed-out-session-token");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CreateSessionAsync Tests

        /// <summary>
        /// Test: Creating a session stores all required fields.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_StoresAllFields()
        {
            // Act
            var session = await _service.CreateSessionAsync(
                userId: 1,
                accessToken: "new-access-token",
                refreshToken: "new-refresh-token",
                ipAddress: "192.168.1.1",
                userAgent: "Mozilla/5.0"
            );

            // Assert
            Assert.NotNull(session);
            Assert.Equal(1, session.UserId);
            Assert.Equal("new-access-token", session.SessionToken);
            Assert.Equal("new-refresh-token", session.RefreshToken);
            Assert.Equal("192.168.1.1", session.IpAddress);
            Assert.Equal("Mozilla/5.0", session.UserAgent);
            Assert.True(session.IsActive);
            Assert.Null(session.InvalidatedAt);
            Assert.NotNull(session.RefreshTokenExpiresAt);
            Assert.True(session.RefreshTokenExpiresAt > DateTime.UtcNow);
        }

        /// <summary>
        /// Test: Creating a session truncates long IP address.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_TruncatesLongIpAddress()
        {
            // Arrange
            var longIpAddress = new string('1', 100);

            // Act
            var session = await _service.CreateSessionAsync(
                userId: 1,
                accessToken: "token",
                refreshToken: "refresh",
                ipAddress: longIpAddress,
                userAgent: "Mozilla/5.0"
            );

            // Assert
            Assert.Equal(50, session.IpAddress!.Length);
        }

        /// <summary>
        /// Test: Creating a session truncates long user agent.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_TruncatesLongUserAgent()
        {
            // Arrange
            var longUserAgent = new string('A', 1000);

            // Act
            var session = await _service.CreateSessionAsync(
                userId: 1,
                accessToken: "token",
                refreshToken: "refresh",
                ipAddress: "192.168.1.1",
                userAgent: longUserAgent
            );

            // Assert
            Assert.Equal(500, session.UserAgent!.Length);
        }

        #endregion
    }
}
