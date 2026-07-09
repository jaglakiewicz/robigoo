using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Application.Dashboard;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("recent-inspections")]
        public async Task<IActionResult> GetRecentInspections([FromQuery] int count = 5, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetRecentInspectionsAsync(count, cancellationToken);
            return Ok(result);
        }

        [HttpGet("recent-crop-sprayers")]
        public async Task<IActionResult> GetRecentCropSprayers([FromQuery] int count = 5, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetRecentCropSprayersAsync(count, cancellationToken);
            return Ok(result);
        }

        [HttpGet("upcoming-inspections")]
        public async Task<IActionResult> GetUpcomingInspections([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetUpcomingInspectionsAsync(limit, cancellationToken);
            return Ok(result);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] int? year, CancellationToken cancellationToken = default)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var result = await _dashboardService.GetStatisticsAsync(targetYear, cancellationToken);
            return Ok(result);
        }
    }
}
