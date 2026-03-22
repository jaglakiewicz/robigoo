using Server.Data;
using Server.Models;
using Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class SessionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly IUserActivityService _userActivityService;
        private readonly ILogger<SessionsController> _logger;

        public SessionsController(
            AppDbContext context,
            ISessionManagementService sessionManagementService,
            IUserActivityService userActivityService,
            ILogger<SessionsController> logger)
        {
            _context = context;
            _sessionManagementService = sessionManagementService;
            _userActivityService = userActivityService;
            _logger = logger;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var now = DateTime.UtcNow;
            
            var sessions = await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.IsActive && s.InvalidatedAt == null)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();

            var result = sessions.Select(s => new
            {
                s.Id,
                s.UserId,
                UserLogin = s.User.Login,
                UserFullName = s.User.FirstName + " " + s.User.LastName,
                s.CreatedAt,
                s.LastActivityAt,
                SessionDuration = (int)(now - s.CreatedAt).TotalMinutes,
                IdleTime = (int)(now - s.LastActivityAt).TotalMinutes,
                s.IpAddress,
                s.UserAgent
            }).ToList();

            return Ok(result);
        }

        [HttpPost("{sessionId}/terminate")]
        public async Task<IActionResult> TerminateSession(long sessionId)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound(new { message = "Session not found" });

            await _sessionManagementService.InvalidateSessionAsync(session.SessionToken);

            var currentUserId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            await _userActivityService.LogActivityAsync(
                currentUserId,
                ActivityType.SessionTerminated,
                "Session",
                sessionId.ToString(),
                $"Admin terminated session for user {session.User.Login}",
                ipAddress,
                userAgent,
                $"{{\"targetUserId\":{session.UserId},\"targetUserLogin\":\"{session.User.Login}\"}}"
            );

            return Ok(new { message = "Session terminated successfully" });
        }

        [HttpPost("user/{userId}/terminate-all")]
        public async Task<IActionResult> TerminateAllUserSessions(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            await _sessionManagementService.InvalidateAllUserSessionsAsync(userId);

            var currentUserId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            await _userActivityService.LogActivityAsync(
                currentUserId,
                ActivityType.SessionTerminated,
                "Session",
                userId.ToString(),
                $"Admin terminated all sessions for user {user.Login}",
                ipAddress,
                userAgent,
                $"{{\"targetUserId\":{userId},\"targetUserLogin\":\"{user.Login}\"}}"
            );

            return Ok(new { message = "All user sessions terminated successfully" });
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) 
                ? userId : 0;
        }
    }
}
