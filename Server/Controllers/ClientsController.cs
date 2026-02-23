#region Imports

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Application.Clients;
using Server.Models;

#endregion

namespace Server.Controllers
{
    [ApiController]
    [Route("api/clients")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        #region Declarations

        private readonly IClientService _clientService;

        #endregion

        #region Constructor

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Gets clients list with filters.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientListItemDto>>> GetClients(
            [FromQuery] string? q,
            [FromQuery] string? clientType,
            [FromQuery] string? city)
        {
            var filter = new ClientFilterDto { Q = q, ClientType = clientType, City = city };
            var result = await _clientService.GetListAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Gets a single client by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDetailDto>> GetClient(string id)
        {
            var detail = await _clientService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();

            return Ok(detail);
        }

        /// <summary>
        /// Creates a new client.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ClientDetailDto>> CreateClient([FromBody] ClientCreateUpdateDto dto)
        {
            var (detail, validationError) = await _clientService.CreateAsync(dto);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            if (detail == null)
                return BadRequest();

            return CreatedAtAction(nameof(GetClient), new { id = detail.Id }, detail);
        }

        /// <summary>
        /// Updates an existing client.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ClientDetailDto>> UpdateClient(string id, [FromBody] ClientCreateUpdateDto dto)
        {
            var (detail, validationError) = await _clientService.UpdateAsync(id, dto);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            if (detail == null)
                return NotFound();

            return Ok(detail);
        }

        /// <summary>
        /// Deletes a client.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var deleted = await _clientService.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        #endregion
    }
}
