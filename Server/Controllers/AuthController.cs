#region Imports

using Server.Data;
using Server.Models;
using Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

#endregion //Imports

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ILogger<AuthController> _logger;

        #endregion //Declarations

        #region Constructor

        public AuthController(
            AppDbContext context, 
            IPasswordService passwordService, 
            ITokenService tokenService,
            ISessionManagementService sessionManagementService,
            ISecurityAuditService securityAuditService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _sessionManagementService = sessionManagementService;
            _securityAuditService = securityAuditService;
            _logger = logger;
        }

        #endregion //Constructor

        #region Properties

        #endregion //Properties

        #region Methods - Public

        /// <summary>
        /// Handles user login with proper session management and account lockout.
        /// </summary>
        /// <remarks>
        /// Requirements:
        /// - 3.2: Only show "interrupting other session" dialog when an active session actually exists
        /// - 3.3: Proceed with login without conflict warnings when no active session exists
        /// - 7.1: Account lockout after configurable failed login attempts (default: 5 attempts, 15-minute lockout)
        /// - 7.3: Return generic error without revealing lockout status
        /// - 7.4: Progressive delays after failed login attempts (e.g., 1s, 2s, 4s, 8s...)
        /// </remarks>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Get client information for security auditing
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            if (string.IsNullOrEmpty(dto.Login) || string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { message = "Login i hasło są wymagane" });

            // Requirement 7.1: Check if account is locked out before authentication
            // Requirement 7.3: Return generic error without revealing lockout status
            var isLockedOut = await _securityAuditService.IsLockedOutAsync(dto.Login, ipAddress);
            if (isLockedOut)
            {
                // Log the attempt even though account is locked
                await _securityAuditService.RecordFailedLoginAsync(
                    dto.Login, 
                    ipAddress, 
                    userAgent, 
                    "Account locked out");
                
                // Detect and log suspicious activity patterns (Requirement 7.2)
                await _securityAuditService.DetectAndLogSuspiciousActivityAsync(
                    dto.Login,
                    ipAddress,
                    userAgent,
                    userExists: true); // We don't know if user exists at this point, assume true for lockout
                
                // Requirement 7.4: Apply progressive delay AFTER recording the failed attempt
                await _securityAuditService.ApplyProgressiveDelayAsync(dto.Login, ipAddress);
                
                // Return generic error to not reveal lockout status (Requirement 7.3)
                return Unauthorized(new { message = "Błędny login lub hasło" });
            }

            var user = _context.Users.FirstOrDefault(u => u.Login == dto.Login);
            if (user == null || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                // Detect and log suspicious activity patterns (Requirement 7.2)
                await _securityAuditService.DetectAndLogSuspiciousActivityAsync(
                    dto.Login,
                    ipAddress,
                    userAgent,
                    userExists: user != null);
                
                // Record failed login attempt for lockout tracking (Requirement 7.1)
                await _securityAuditService.RecordFailedLoginAsync(
                    dto.Login, 
                    ipAddress, 
                    userAgent, 
                    user == null ? "User not found" : "Invalid password");
                
                // Requirement 7.4: Apply progressive delay AFTER recording the failed attempt
                await _securityAuditService.ApplyProgressiveDelayAsync(dto.Login, ipAddress);
                
                return Unauthorized(new { message = "Błędny login lub hasło" });
            }

            if (!user.IsActive)
            {
                await _securityAuditService.RecordFailedLoginAsync(
                    dto.Login, 
                    ipAddress, 
                    userAgent, 
                    "User inactive");
                
                // Requirement 7.4: Apply progressive delay AFTER recording the failed attempt
                await _securityAuditService.ApplyProgressiveDelayAsync(dto.Login, ipAddress);
                
                return Unauthorized(new { message = "Użytkownik jest nieaktywny" });
            }

            // One user - one session policy: Always invalidate existing sessions
            // This eliminates the need for user confirmation dialogs
            var sessionCheck = await _sessionManagementService.CheckExistingSessionsAsync(user.Id);
            
            if (sessionCheck.HasActiveSession)
            {
                await _sessionManagementService.InvalidateAllUserSessionsAsync(user.Id);
                _logger.LogInformation(
                    "Invalidated existing session for user {UserId} due to new login from {IpAddress}",
                    user.Id, ipAddress);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Login, user.Role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Create new session using session management service
            await _sessionManagementService.CreateSessionAsync(
                user.Id, 
                accessToken, 
                refreshToken, 
                ipAddress ?? "unknown", 
                userAgent);

            // Log successful login and reset failed attempts (Requirement 7.1)
            await _securityAuditService.LogLoginAttemptAsync(
                dto.Login, 
                ipAddress, 
                userAgent, 
                success: true);
            await _securityAuditService.ResetFailedAttemptsAsync(dto.Login);

            var response = new AuthResponseDto
            {
                Token = new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = _tokenService.GetAccessTokenExpirationMinutes() * 60
                },
                User = new LoginResponseDto
                {
                    UserId = user.Id,
                    Login = user.Login,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    PermissionNumber = user.PermissionNumber,
                    Role = user.Role,
                    AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Handles user logout by invalidating the current session.
        /// </summary>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            if (!string.IsNullOrEmpty(token))
            {
                await _sessionManagementService.InvalidateSessionAsync(token);
                _logger.LogInformation("User logged out, session invalidated");
            }
            
            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Refreshes an access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (string.IsNullOrEmpty(dto.AccessToken) || string.IsNullOrEmpty(dto.RefreshToken))
                return BadRequest(new { message = "Access token and refresh token are required" });

            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            if (principal == null)
                return Unauthorized(new { message = "Invalid access token" });

            var userIdClaim = principal.FindFirst("userId");
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
                return Unauthorized(new { message = "Invalid token claims" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "User not found or inactive" });

            // Verify refresh token is valid and matches the session
            var session = _context.UserSessions
                .FirstOrDefault(s => s.UserId == userId && 
                                    s.RefreshToken == dto.RefreshToken && 
                                    s.IsActive);

            if (session == null)
                return Unauthorized(new { message = "Invalid refresh token" });

            if (session.RefreshTokenExpiresAt.HasValue && session.RefreshTokenExpiresAt.Value <= DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token expired" });

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Login, user.Role);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Update session with new tokens
            session.SessionToken = newAccessToken;
            session.RefreshToken = newRefreshToken;
            session.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays());
            session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _tokenService.GetAccessTokenExpirationMinutes() * 60
            };

            return Ok(response);
        }

        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
                return Unauthorized();

            var user = _context.Users.Find(userId);
            if (user == null)
                return Unauthorized();

            var response = new LoginResponseDto
            {
                UserId = user.Id,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PermissionNumber = user.PermissionNumber,
                Role = user.Role,
                AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
            };

            return Ok(response);
        }

        #endregion

        #region Methods - Private

        #endregion
    }
}

