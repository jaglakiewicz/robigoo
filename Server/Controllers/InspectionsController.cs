#region Imports

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Application.Inspections;
using Server.Models;

#endregion

namespace Server.Controllers
{
    [ApiController]
    [Route("api/inspections")]
    [Authorize]
    public class InspectionsController : ControllerBase
    {
        #region Declarations

        private readonly IInspectionService _inspectionService;

        #endregion

        #region Constructor

        public InspectionsController(IInspectionService inspectionService)
        {
            _inspectionService = inspectionService;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Gets all inspections.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InspectionResponseDto>>> GetInspections()
        {
            var result = await _inspectionService.GetListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a single inspection by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<InspectionResponseDto>> GetInspection(long id)
        {
            var result = await _inspectionService.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new inspection.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InspectionResponseDto>> CreateInspection([FromBody] InspectionCreateDto dto)
        {
            var result = await _inspectionService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetInspection), new { id = result.Id }, result);
        }

        #endregion
    }
}
