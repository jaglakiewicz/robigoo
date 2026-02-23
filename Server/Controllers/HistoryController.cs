using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistoryController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get history for a specific entity.
        /// </summary>
        /// <param name="entityName">The name of the table/entity (e.g. Machine, Client)</param>
        /// <param name="entityId">The ID of the record</param>
        /// <returns>List of change logs</returns>
        [HttpGet("{entityName}/{entityId}")]
        public async Task<ActionResult<IEnumerable<ChangeLog>>> GetHistory(string entityName, string entityId)
        {
            // Simple validation or sanitation could be added here
            
            var logs = await _context.ChangeLogs
                .Where(l => l.EntityName == entityName && l.EntityId == entityId)
                .OrderByDescending(l => l.When)
                .ToListAsync();

            return Ok(logs);
        }
    }
}
