#region Imports

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Application.History;

#endregion

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        #region Declarations

        private readonly IHistoryService _historyService;

        #endregion

        #region Constructor

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Get history for a specific entity.
        /// </summary>
        /// <param name="entityName">The name of the table/entity (e.g. CropSprayer, Client)</param>
        /// <param name="entityId">The ID of the record</param>
        /// <returns>List of change log entries with user details</returns>
        [HttpGet("{entityName}/{entityId}")]
        public async Task<ActionResult<IEnumerable<HistoryEntryDto>>> GetHistory(string entityName, string entityId)
        {
            var logs = await _historyService.GetHistoryAsync(entityName, entityId);
            return Ok(logs);
        }

        #endregion
    }
}
