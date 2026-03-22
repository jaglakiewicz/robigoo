using Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IUserActivityService _userActivityService;
        private readonly Server.Services.IAuthorizationService _authorizationService;

        public ActivityLogsController(
            IUserActivityService userActivityService,
            Server.Services.IAuthorizationService authorizationService)
        {
            _userActivityService = userActivityService;
            _authorizationService = authorizationService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserActivities(long userId, [FromQuery] int limit = 100)
        {
            var currentUserId = GetCurrentUserId();
            
            // Users can view their own logs, admins can view any user's logs
            if (currentUserId != userId && !await _authorizationService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var activities = await _userActivityService.GetUserActivitiesAsync(userId, limit);
            return Ok(activities);
        }

        [HttpGet("all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllActivities(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int limit = 1000)
        {
            var activities = await _userActivityService.GetAllActivitiesAsync(from, to, limit);
            return Ok(activities);
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) 
                ? userId : 0;
        }
    }
}
