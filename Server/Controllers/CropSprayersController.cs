#region Imports

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Application.CropSprayers;
using Server.Models;

#endregion

namespace Server.Controllers
{
    [ApiController]
    [Route("api/machines")]
    [Authorize]
    public class CropSprayersController : ControllerBase
    {
        #region Declarations

        private readonly ICropSprayerService _cropSprayerService;

        #endregion

        #region Constructor

        public CropSprayersController(ICropSprayerService cropSprayerService)
        {
            _cropSprayerService = cropSprayerService;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Lista opryskiwaczy z prostym filtrem tekstowym i po typie/rodzaju.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CropSprayerListItemDto>>> GetCropSprayers(
            [FromQuery] string? q,
            [FromQuery] string? type,
            [FromQuery] string? kind,
            [FromQuery] string? manufacturer,
            [FromQuery] string? yearFrom,
            [FromQuery] string? yearTo,
            [FromQuery] string? ownerId)
        {
            var filter = new CropSprayerFilterDto
            {
                Q = q,
                Type = type,
                Kind = kind,
                Manufacturer = manufacturer,
                YearFrom = yearFrom,
                YearTo = yearTo,
                OwnerId = ownerId
            };

            var result = await _cropSprayerService.GetListAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Unique field values for autosuggestions.
        /// GET /api/machines/suggestions?field=manufacturer|sprayerName
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<ActionResult<IEnumerable<string>>> GetSuggestions([FromQuery] string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return BadRequest(new { message = "Field parameter is required." });

            var values = await _cropSprayerService.GetDistinctValuesAsync(field);
            if (values == null)
                return BadRequest(new { message = $"Unsupported field: {field}" });

            return Ok(values);
        }

        /// <summary>
        /// Szczegóły opryskiwacza.
        /// </summary>
        [HttpGet("{serialNumber}")]
        public async Task<ActionResult<CropSprayerDetailDto>> GetCropSprayer(string serialNumber)
        {
            var detail = await _cropSprayerService.GetBySerialNumberAsync(serialNumber);
            if (detail == null)
                return NotFound();

            return Ok(detail);
        }

        /// <summary>
        /// Dodanie nowego opryskiwacza.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CropSprayerDetailDto>> CreateCropSprayer([FromBody] CropSprayerCreateUpdateDto dto)
        {
            var (detail, validationError) = await _cropSprayerService.CreateAsync(dto);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            if (detail == null)
                return BadRequest();

            return CreatedAtAction(nameof(GetCropSprayer), new { serialNumber = detail.SerialNumber }, detail);
        }

        /// <summary>
        /// Aktualizacja istniejącego opryskiwacza.
        /// Implements optimistic concurrency control with 409 Conflict on concurrent modification.
        /// </summary>
        [HttpPut("{serialNumber}")]
        public async Task<ActionResult<CropSprayerDetailDto>> UpdateCropSprayer(string serialNumber, [FromBody] CropSprayerCreateUpdateDto dto)
        {
            var (detail, validationError, concurrencyConflict) = await _cropSprayerService.UpdateAsync(serialNumber, dto);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            if (concurrencyConflict != null)
                return Conflict(new
                {
                    error = "CONCURRENCY_CONFLICT",
                    message = concurrencyConflict,
                    entityType = "CropSprayer",
                    entityId = serialNumber,
                    conflictTime = DateTime.UtcNow
                });

            if (detail == null)
                return NotFound();

            return Ok(detail);
        }

        /// <summary>
        /// Usunięcie opryskiwacza.
        /// </summary>
        [HttpDelete("{serialNumber}")]
        public async Task<IActionResult> DeleteCropSprayer(string serialNumber)
        {
            var deleted = await _cropSprayerService.DeleteAsync(serialNumber);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        #endregion
    }
}
