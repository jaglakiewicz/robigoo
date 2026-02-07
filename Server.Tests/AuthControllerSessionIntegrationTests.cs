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
    /// Integration tests for the session bug fix in the login flow.
    /// Tests the complete login flow through AuthController with various session states.
    /// 
    /// Validates: Requirements 3.2, 3.3
    /// - 3.2: THE Session_Manager SHALL only show the "interrupting other session" dialog when an active session actually exists
    /// - 3.3: WHEN no active session exists for the user, THE Authentication_Service SHALL proceed with login without conflict warnings
    /// </summary>
    public class AuthControllerSessionIntegrationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AuthController _controller;
        private readonly SessionManagementService _sessionManagementService;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ISecurityAuditService> _securityAuditServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<SessionManagementService>> _sessionLoggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        private const string TestPassword = "TestPassword123!";
        private const string TestPasswordHash = "hashed_password";
        private const string TestAccessToken = "test_access_token";
        private const string TestRefreshToken = "test_refresh_token";

        public AuthControllerSessionIntegrationTests()
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
            _sessionLoggerMock = new Mock<ILogger<SessionManagementService>>();

            // Create real SessionManagementService (the component under test)
            _sessionManagementService = new SessionManagementService(
                _context, 
                _configurationMock.Object, 
                _sessionLoggerMock.Object);

            // Set up password service mock
            _passwordServiceMock = new Mock<IPasswordService>();
            _passwordServiceMock
                .Setup(p => p.VerifyPassword(TestPassword, TestPasswordHash))
                .Returns(true);
            _passwordServiceMock
                .Setup(p => p.VerifyPassword(It.Is<string>(s => s != TestPassword), It.IsAny<string>()))
                .Returns(false);

            // Set up token service mock
            _tokenServiceMock = new Mock<ITokenService>();
            _tokenServiceMock
                .Setup(t => t.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(TestAccessToken);
            _tokenServiceMock
                .Setup(t => t.GenerateRefreshToken())
                .Returns(TestRefreshToken);
            _tokenServiceMock
                .Setup(t => t.GetAccessTokenExpirationMinutes())
                .Returns(60);

            // Set up security audit service mock
            _securityAuditServiceMock = new Mock<ISecurityAuditService>();
            _securityAuditServiceMock
                .Setup(s => s.IsLockedOutAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);
            _securityAuditServiceMock
                .Setup(s => s.LogLoginAttemptAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _securityAuditServiceMock
                .Setup(s => s.ResetFailedAttemptsAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Create controller with real SessionManagementService
            _controller = new AuthController(
                _context,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object,
                _sessionManagementService,
                _securityAuditServiceMock.Object);

            // Set up HttpContext for the controller
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            httpContext.Request.Headers["User-Agent"] = "Test User Agent";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Seed test user
            SeedTestUser();
        }

        private void SeedTestUser()
        {
            var user = new User
            {
                Id = 1,
                Login = "testuser",
                PasswordHash = TestPasswordHash,
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

        #region Integration Tests - Login Flow with Session States

        /// <summary>
        /// Integration Test: Complete login flow with no existing session succeeds without conflict.
        /// 
        /// Validates: Requirements 3.2, 3.3
        /// - 3.2: Only show conflict when active session actually exists
        /// - 3.3: Proceed with login without conflict warnings when no active session exists
        /// 
        /// Scenario:
        /// 1. User has no existing sessions in the database
        /// 2. User attempts to login with valid credentials
        /// 3. Login should succeed with 200 OK and return tokens
        /// 4. A new session should be created
        /// </summary>
        [Fact]
        public async Task Login_WithNoExistingSession_SucceedsWithoutConflict()
        {
            // Arrange - no sessions exist for user (clean state)
            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = false
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify response contains tokens
            var response = okResult.Value as AuthResponseDto;
            Assert.NotNull(response);
            Assert.Equal(TestAccessToken, response.Token.AccessToken);
            Assert.Equal(TestRefreshToken, response.Token.RefreshToken);
            Assert.Equal("testuser", response.User.Login);

            // Verify a new session was created
            var sessions = await _context.UserSessions.Where(s => s.UserId == 1).ToListAsync();
            Assert.Single(sessions);
            Assert.True(sessions[0].IsActive);
            Assert.Null(sessions[0].InvalidatedAt);
        }

        /// <summary>
        /// Integration Test: Complete login flow with expired session (refresh token expired) succeeds without conflict.
        /// 
        /// Validates: Requirements 3.3, 3.6
        /// - 3.3: Proceed with login without conflict warnings when no active session exists
        /// - 3.6: Exclude sessions where the refresh token has expired
        /// 
        /// Scenario:
        /// 1. User has an existing session with expired refresh token
        /// 2. User attempts to login with valid credentials
        /// 3. Login should succeed with 200 OK (no conflict)
        /// 4. A new session should be created
        /// </summary>
        [Fact]
        public async Task Login_WithExpiredRefreshTokenSession_SucceedsWithoutConflict()
        {
            // Arrange - create a session with expired refresh token
            var expiredSession = new UserSession
            {
                UserId = 1,
                SessionToken = "expired-access-token",
                RefreshToken = "expired-refresh-token",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5), // Recent activity but token expired
                IsActive = true,
                InvalidatedAt = null,
                IpAddress = "192.168.1.100",
                UserAgent = "Old Browser"
            };
            _context.UserSessions.Add(expiredSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = false
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK (not 409 Conflict)
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify response contains tokens
            var response = okResult.Value as AuthResponseDto;
            Assert.NotNull(response);
            Assert.Equal(TestAccessToken, response.Token.AccessToken);

            // Verify a new session was created (total 2 sessions now)
            var sessions = await _context.UserSessions.Where(s => s.UserId == 1).ToListAsync();
            Assert.Equal(2, sessions.Count);
            
            // The new session should be active
            var newSession = sessions.FirstOrDefault(s => s.SessionToken == TestAccessToken);
            Assert.NotNull(newSession);
            Assert.True(newSession.IsActive);
        }

        /// <summary>
        /// Integration Test: Complete login flow with timed out session (inactivity) succeeds without conflict.
        /// 
        /// Validates: Requirements 3.3, 3.7
        /// - 3.3: Proceed with login without conflict warnings when no active session exists
        /// - 3.7: Mark sessions as inactive if last activity exceeds configured timeout
        /// 
        /// Scenario:
        /// 1. User has an existing session that has timed out due to inactivity (> 30 min)
        /// 2. User attempts to login with valid credentials
        /// 3. Login should succeed with 200 OK (no conflict)
        /// 4. The old session should be marked as inactive
        /// 5. A new session should be created
        /// </summary>
        [Fact]
        public async Task Login_WithTimedOutSession_SucceedsWithoutConflict()
        {
            // Arrange - create a session that has timed out due to inactivity
            var timedOutSession = new UserSession
            {
                UserId = 1,
                SessionToken = "timed-out-access-token",
                RefreshToken = "valid-refresh-token",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token still valid
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-60), // Last activity 60 minutes ago (> 30 min timeout)
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30,
                IpAddress = "192.168.1.100",
                UserAgent = "Old Browser"
            };
            _context.UserSessions.Add(timedOutSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = false
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK (not 409 Conflict)
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify the old session was marked as inactive
            var oldSession = await _context.UserSessions.FindAsync(timedOutSession.Id);
            Assert.NotNull(oldSession);
            Assert.False(oldSession.IsActive);
            Assert.NotNull(oldSession.InvalidatedAt);
            Assert.Equal("Session timeout", oldSession.InvalidationReason);

            // Verify a new session was created
            var newSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == TestAccessToken);
            Assert.NotNull(newSession);
            Assert.True(newSession.IsActive);
        }

        /// <summary>
        /// Integration Test: Complete login flow with active session returns conflict.
        /// 
        /// Validates: Requirements 3.1, 3.2
        /// - 3.1: Check for existing active sessions for that user only
        /// - 3.2: Only show conflict when an active session actually exists
        /// 
        /// Scenario:
        /// 1. User has an existing truly active session (not expired, not timed out)
        /// 2. User attempts to login with valid credentials without Force flag
        /// 3. Login should return 409 Conflict with session info
        /// 4. No new session should be created
        /// </summary>
        [Fact]
        public async Task Login_WithActiveSession_ReturnsConflict()
        {
            // Arrange - create a truly active session
            var activeSession = new UserSession
            {
                UserId = 1,
                SessionToken = "active-access-token",
                RefreshToken = "active-refresh-token",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7), // Still valid
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5), // Recent activity (within 30 min timeout)
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30,
                IpAddress = "192.168.1.100",
                UserAgent = "Active Browser"
            };
            _context.UserSessions.Add(activeSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = false // Not forcing login
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should return 409 Conflict
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);

            // Verify conflict response contains session info
            var responseValue = conflictResult.Value;
            Assert.NotNull(responseValue);
            
            // Use reflection to check the anonymous type properties
            var messageProperty = responseValue.GetType().GetProperty("message");
            var hasActiveSessionProperty = responseValue.GetType().GetProperty("hasActiveSession");
            
            Assert.NotNull(messageProperty);
            Assert.NotNull(hasActiveSessionProperty);
            Assert.True((bool)hasActiveSessionProperty.GetValue(responseValue)!);

            // Verify no new session was created (still only 1 session)
            var sessions = await _context.UserSessions.Where(s => s.UserId == 1).ToListAsync();
            Assert.Single(sessions);
            Assert.Equal(activeSession.Id, sessions[0].Id);
        }

        /// <summary>
        /// Integration Test: Complete login flow with active session and Force flag succeeds.
        /// 
        /// Validates: Requirements 3.2, 3.4
        /// - 3.2: Only show conflict when active session actually exists (but Force bypasses)
        /// - 3.4: Properly invalidate sessions on logout/force login
        /// 
        /// Scenario:
        /// 1. User has an existing truly active session
        /// 2. User attempts to login with valid credentials WITH Force flag
        /// 3. Login should succeed with 200 OK
        /// 4. The old session should be invalidated
        /// 5. A new session should be created
        /// </summary>
        [Fact]
        public async Task Login_WithActiveSessionAndForceFlag_SucceedsAndInvalidatesOldSession()
        {
            // Arrange - create a truly active session
            var activeSession = new UserSession
            {
                UserId = 1,
                SessionToken = "active-access-token",
                RefreshToken = "active-refresh-token",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30,
                IpAddress = "192.168.1.100",
                UserAgent = "Active Browser"
            };
            _context.UserSessions.Add(activeSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = true // Force login
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify the old session was invalidated
            var oldSession = await _context.UserSessions.FindAsync(activeSession.Id);
            Assert.NotNull(oldSession);
            Assert.False(oldSession.IsActive);
            Assert.NotNull(oldSession.InvalidatedAt);
            Assert.Equal("Force logout - all sessions", oldSession.InvalidationReason);

            // Verify a new session was created
            var newSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == TestAccessToken);
            Assert.NotNull(newSession);
            Assert.True(newSession.IsActive);
        }

        /// <summary>
        /// Integration Test: Login with invalidated session (IsActive=false) succeeds without conflict.
        /// 
        /// Validates: Requirements 3.2, 3.3
        /// - 3.2: Only show conflict when active session actually exists
        /// - 3.3: Proceed with login without conflict warnings when no active session exists
        /// 
        /// Scenario:
        /// 1. User has an existing session that was previously logged out (IsActive=false)
        /// 2. User attempts to login with valid credentials
        /// 3. Login should succeed with 200 OK (no conflict)
        /// </summary>
        [Fact]
        public async Task Login_WithInactiveSession_SucceedsWithoutConflict()
        {
            // Arrange - create an inactive session (previously logged out)
            var inactiveSession = new UserSession
            {
                UserId = 1,
                SessionToken = "inactive-access-token",
                RefreshToken = "inactive-refresh-token",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7), // Token would still be valid
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-10),
                IsActive = false, // Already logged out
                InvalidatedAt = DateTime.UtcNow.AddMinutes(-10),
                InvalidationReason = "Logout",
                IpAddress = "192.168.1.100",
                UserAgent = "Old Browser"
            };
            _context.UserSessions.Add(inactiveSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = false
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK (not 409 Conflict)
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify a new session was created
            var newSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == TestAccessToken);
            Assert.NotNull(newSession);
            Assert.True(newSession.IsActive);
        }

        /// <summary>
        /// Integration Test: Login with explicitly invalidated session succeeds without conflict.
        /// 
        /// Validates: Requirements 3.2, 3.3
        /// - 3.2: Only show conflict when active session actually exists
        /// - 3.3: Proceed with login without conflict warnings when no active session exists
        /// 
        /// Scenario:
        /// 1. User has an existing session that was explicitly invalidated (InvalidatedAt set)
        /// 2. User attempts to login with valid credentials
        /// 3. Login should succeed with 200 OK (no conflict)
        /// </summary>
        [Fact]
        public async Task Login_WithExplicitlyInvalidatedSession_SucceedsWithoutConflict()
        {
            // Arrange - create a session that was explicitly invalidated by admin
            var invalidatedSession = new UserSession
            {
                UserId = 1,
                SessionToken = "invalidated-access-token",
                RefreshToken = "invalidated-refresh-token",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5), // Recent activity
                IsActive = true, // Still marked active but explicitly invalidated
                InvalidatedAt = DateTime.UtcNow.AddMinutes(-2), // Explicitly invalidated
                InvalidationReason = "Force logout by admin",
                IpAddress = "192.168.1.100",
                UserAgent = "Old Browser"
            };
            _context.UserSessions.Add(invalidatedSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser",
                Password = TestPassword,
                Force = false
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK (not 409 Conflict)
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        /// <summary>
        /// Integration Test: Session check only considers sessions for the specific user.
        /// 
        /// Validates: Requirement 3.1
        /// - 3.1: Check for existing active sessions for that user only
        /// 
        /// Scenario:
        /// 1. Another user has an active session
        /// 2. Test user has no sessions
        /// 3. Test user attempts to login
        /// 4. Login should succeed (other user's session should not cause conflict)
        /// </summary>
        [Fact]
        public async Task Login_WithOtherUserActiveSession_SucceedsWithoutConflict()
        {
            // Arrange - create another user with an active session
            var otherUser = new User
            {
                Id = 2,
                Login = "otheruser",
                PasswordHash = TestPasswordHash,
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
                RefreshToken = "other-user-refresh",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                InvalidatedAt = null,
                SessionTimeoutMinutes = 30
            };
            _context.UserSessions.Add(otherUserSession);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Login = "testuser", // Login as test user (user ID 1)
                Password = TestPassword,
                Force = false
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert - should succeed with 200 OK (other user's session should not affect this)
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Verify test user's new session was created
            var testUserSessions = await _context.UserSessions.Where(s => s.UserId == 1).ToListAsync();
            Assert.Single(testUserSessions);
            Assert.True(testUserSessions[0].IsActive);

            // Verify other user's session is still active
            var otherUserSessions = await _context.UserSessions.Where(s => s.UserId == 2).ToListAsync();
            Assert.Single(otherUserSessions);
            Assert.True(otherUserSessions[0].IsActive);
        }

        #endregion
    }
}
