#region Imports

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;
using System.Text.RegularExpressions;

#endregion

namespace Server.Controllers
{
    [ApiController]
    [Route("api/machines")]
    [Authorize]
    public class CropSprayersController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly ILogger<CropSprayersController> _logger;
        private readonly IConcurrencyController _concurrencyController;

        private static readonly Regex YearRegex = new("^\\d{4}$", RegexOptions.Compiled);

        #endregion

        #region Constructor

        public CropSprayersController(
            AppDbContext context, 
            ILogger<CropSprayersController> logger,
            IConcurrencyController concurrencyController)
        {
            _context = context;
            _logger = logger;
            _concurrencyController = concurrencyController;
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
            [FromQuery] string? yearTo)
        {
            // Join crop sprayers with clients to get real owner names
            var joinedQuery = from cs in _context.CropSprayers
                              join c in _context.Clients on cs.OwnerId equals c.Id into clients
                              from client in clients.DefaultIfEmpty()
                              select new { CropSprayer = cs, Client = client };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                joinedQuery = joinedQuery.Where(mc =>
                    mc.CropSprayer.SerialNumber.ToLower().Contains(term) ||
                    mc.CropSprayer.SprayerName.ToLower().Contains(term) ||
                    mc.CropSprayer.Manufacturer.ToLower().Contains(term) ||
                    mc.CropSprayer.ProductionYear.ToLower().Contains(term) ||
                    (mc.Client != null && mc.Client.DisplayName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                joinedQuery = joinedQuery.Where(mc => mc.CropSprayer.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(kind))
            {
                joinedQuery = joinedQuery.Where(mc => mc.CropSprayer.Kind == kind);
            }

            if (!string.IsNullOrWhiteSpace(manufacturer))
            {
                var manufacturerTerm = manufacturer.Trim().ToLowerInvariant();
                joinedQuery = joinedQuery.Where(mc => mc.CropSprayer.Manufacturer.ToLower().Contains(manufacturerTerm));
            }

            if (!string.IsNullOrWhiteSpace(yearFrom) && YearRegex.IsMatch(yearFrom))
            {
                joinedQuery = joinedQuery.Where(mc => string.Compare(mc.CropSprayer.ProductionYear, yearFrom) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(yearTo) && YearRegex.IsMatch(yearTo))
            {
                joinedQuery = joinedQuery.Where(mc => string.Compare(mc.CropSprayer.ProductionYear, yearTo) <= 0);
            }

            var result = await joinedQuery
                .OrderByDescending(mc => mc.CropSprayer.CreatedAt)
                .Select(mc => new CropSprayerListItemDto
                {
                    SerialNumber = mc.CropSprayer.SerialNumber,
                    SprayerName = mc.CropSprayer.SprayerName,
                    Manufacturer = mc.CropSprayer.Manufacturer,
                    ProductionYear = mc.CropSprayer.ProductionYear,
                    Type = mc.CropSprayer.Type,
                    Kind = mc.CropSprayer.Kind,
                    OwnerId = mc.CropSprayer.OwnerId,
                    OwnerName = mc.Client != null ? mc.Client.DisplayName : null,
                    CreatedAt = mc.CropSprayer.CreatedAt
                })
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Szczegóły opryskiwacza.
        /// </summary>
        [HttpGet("{serialNumber}")]
        public async Task<ActionResult<CropSprayerDetailDto>> GetCropSprayer(string serialNumber)
        {
            var cropSprayer = await _context.CropSprayers.FindAsync(serialNumber);

            if (cropSprayer == null)
                return NotFound();

            // Look up real owner name from Clients table
            string? realOwnerName = null;
            if (!string.IsNullOrEmpty(cropSprayer.OwnerId))
            {
                var owner = await _context.Clients.FindAsync(cropSprayer.OwnerId);
                realOwnerName = owner?.DisplayName;
            }

            return Ok(ToDetailDto(cropSprayer, realOwnerName));
        }

        /// <summary>
        /// Dodanie nowego opryskiwacza.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CropSprayerDetailDto>> CreateCropSprayer([FromBody] CropSprayerCreateUpdateDto dto)
        {
            var validationError = ValidateDto(dto, isCreate: true);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            if (await _context.CropSprayers.AnyAsync(cs => cs.SerialNumber == dto.SerialNumber))
                return BadRequest(new { message = "Opryskiwacz o podanym numerze już istnieje" });

            var cropSprayer = new CropSprayer
            {
                SerialNumber = dto.SerialNumber.Trim(),
                SprayerName = dto.SprayerName.Trim(),
                Type = dto.Type,
                Kind = dto.Kind,
                Manufacturer = dto.Manufacturer.Trim(),
                ProductionYear = dto.ProductionYear,
                PurchaseDate = dto.PurchaseDate,
                PumpPiston = dto.PumpPiston,
                PumpDiaphragm = dto.PumpDiaphragm,
                PumpOther = dto.PumpOther,
                PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim(),
                PumpFlowRate = dto.PumpFlowRate,
                TankCapacity = dto.TankCapacity,
                HasFlushing = dto.HasFlushing,
                HasDiluter = dto.HasDiluter,
                HasWashingDevice = dto.HasWashingDevice,
                HasManometer = dto.HasManometer,
                HasComputer = dto.HasComputer,
                BoomWidth = dto.BoomWidth,
                BoomWet = dto.BoomWet,
                BoomDry = dto.BoomDry,
                BoomDampeningMechanism = dto.BoomDampeningMechanism,
                SectionCount = dto.SectionCount,
                NozzlesFieldFeatures = dto.NozzlesFieldFeatures,
                NozzlesGardenFeatures = dto.NozzlesGardenFeatures,
                FanType = dto.FanType,
                OwnerId = dto.OwnerId,
                OwnerName = dto.OwnerName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.CropSprayers.Add(cropSprayer);
            await _context.SaveChangesAsync();

            // Look up real owner name from Clients table
            string? realOwnerName = null;
            if (!string.IsNullOrEmpty(cropSprayer.OwnerId))
            {
                var owner = await _context.Clients.FindAsync(cropSprayer.OwnerId);
                realOwnerName = owner?.DisplayName;
            }

            var detail = ToDetailDto(cropSprayer, realOwnerName);
            return CreatedAtAction(nameof(GetCropSprayer), new { serialNumber = cropSprayer.SerialNumber }, detail);
        }

        /// <summary>
        /// Aktualizacja istniejącego opryskiwacza.
        /// Implements optimistic concurrency control with 409 Conflict on concurrent modification.
        /// Requirements: 4.3, 4.6
        /// </summary>
        [HttpPut("{serialNumber}")]
        public async Task<ActionResult<CropSprayerDetailDto>> UpdateCropSprayer(string serialNumber, [FromBody] CropSprayerCreateUpdateDto dto)
        {
            _logger.LogInformation("=== UpdateCropSprayer called ===");
            _logger.LogInformation("Received DTO - OwnerId: {OwnerId}, OwnerName: {OwnerName}", dto.OwnerId, dto.OwnerName);
            
            if (!string.Equals(serialNumber, dto.SerialNumber, StringComparison.Ordinal))
                return BadRequest(new { message = "Nie można zmienić numeru seryjnego opryskiwacza" });

            var cropSprayer = await _context.CropSprayers.FindAsync(serialNumber);
            if (cropSprayer == null)
                return NotFound();

            var validationError = ValidateDto(dto, isCreate: false);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            // Execute update with optimistic concurrency control
            // Requirement 4.3: IF a concurrency conflict is detected, THEN THE Backend SHALL return a 409 Conflict response with details
            // Requirement 4.6: WHEN saving inspection protocols, THE Database_Access_Layer SHALL acquire appropriate locks to prevent race conditions
            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                // Re-fetch the entity to ensure we have the latest version
                var entity = await _context.CropSprayers.FindAsync(serialNumber);
                if (entity == null)
                    throw new InvalidOperationException("Entity not found during update");

                // Capture old owner values for history tracking
                var oldOwnerId = entity.OwnerId;
                var oldOwnerName = entity.OwnerName;
                
                _logger.LogInformation("Old values - OwnerId: {OwnerId}, OwnerName: {OwnerName}", oldOwnerId, oldOwnerName);

                entity.SprayerName = dto.SprayerName.Trim();
                entity.Type = dto.Type;
                entity.Kind = dto.Kind;
                entity.Manufacturer = dto.Manufacturer.Trim();
                entity.ProductionYear = dto.ProductionYear;
                entity.PurchaseDate = dto.PurchaseDate;
                entity.PumpPiston = dto.PumpPiston;
                entity.PumpDiaphragm = dto.PumpDiaphragm;
                entity.PumpOther = dto.PumpOther;
                entity.PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim();
                entity.PumpFlowRate = dto.PumpFlowRate;
                entity.TankCapacity = dto.TankCapacity;
                entity.HasFlushing = dto.HasFlushing;
                entity.HasDiluter = dto.HasDiluter;
                entity.HasWashingDevice = dto.HasWashingDevice;
                entity.HasManometer = dto.HasManometer;
                entity.HasComputer = dto.HasComputer;
                entity.BoomWidth = dto.BoomWidth;
                entity.BoomWet = dto.BoomWet;
                entity.BoomDry = dto.BoomDry;
                entity.BoomDampeningMechanism = dto.BoomDampeningMechanism;
                entity.SectionCount = dto.SectionCount;
                entity.NozzlesFieldFeatures = dto.NozzlesFieldFeatures;
                entity.NozzlesGardenFeatures = dto.NozzlesGardenFeatures;
                entity.FanType = dto.FanType;

                // Track owner change in history
                if (oldOwnerId != dto.OwnerId || oldOwnerName != dto.OwnerName)
                {
                    var changeDescription = BuildOwnerChangeDescription(oldOwnerId, oldOwnerName, dto.OwnerId, dto.OwnerName);
                    var userName = User.Identity?.Name ?? "System";
                    
                    var changeLog = new ChangeLog
                    {
                        EntityName = "CropSprayer",
                        EntityId = serialNumber,
                        Changes = changeDescription,
                        Who = userName,
                        When = DateTime.UtcNow
                    };
                    _context.ChangeLogs.Add(changeLog);
                }

                entity.OwnerId = dto.OwnerId;
                entity.OwnerName = dto.OwnerName;
                entity.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation("After assignment - CropSprayer.OwnerId: {OwnerId}, CropSprayer.OwnerName: {OwnerName}", entity.OwnerId, entity.OwnerName);

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("After SaveChangesAsync - CropSprayer.OwnerId: {OwnerId}, CropSprayer.OwnerName: {OwnerName}", entity.OwnerId, entity.OwnerName);

                return entity;
            });

            // Check if concurrency conflict occurred
            if (!result.Success)
            {
                _logger.LogWarning(
                    "Concurrency conflict detected for CropSprayer {SerialNumber}: {Message}",
                    serialNumber,
                    result.Conflict?.Message);

                return Conflict(new
                {
                    error = "CONCURRENCY_CONFLICT",
                    message = result.Conflict?.Message ?? "The record was modified by another user. Please refresh and try again.",
                    entityType = result.Conflict?.EntityType ?? "CropSprayer",
                    entityId = result.Conflict?.EntityId ?? serialNumber,
                    conflictTime = result.Conflict?.ConflictTime ?? DateTime.UtcNow
                });
            }

            // Look up real owner name from Clients table
            string? realOwnerName = null;
            if (!string.IsNullOrEmpty(result.Result!.OwnerId))
            {
                var owner = await _context.Clients.FindAsync(result.Result.OwnerId);
                realOwnerName = owner?.DisplayName;
            }

            var detailDto = ToDetailDto(result.Result, realOwnerName);
            _logger.LogInformation("ToDetailDto result - OwnerId: {OwnerId}, OwnerName: {OwnerName}", detailDto.OwnerId, detailDto.OwnerName);

            return Ok(detailDto);
        }

        /// <summary>
        /// Usunięcie opryskiwacza.
        /// </summary>
        [HttpDelete("{serialNumber}")]
        public async Task<IActionResult> DeleteCropSprayer(string serialNumber)
        {
            var cropSprayer = await _context.CropSprayers.FindAsync(serialNumber);
            if (cropSprayer == null)
                return NotFound();

            _context.CropSprayers.Remove(cropSprayer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Methods - Private

        private static CropSprayerDetailDto ToDetailDto(CropSprayer cs, string? ownerNameOverride = null)
        {
            return new CropSprayerDetailDto
            {
                SerialNumber = cs.SerialNumber,
                SprayerName = cs.SprayerName,
                Type = cs.Type,
                Kind = cs.Kind,
                Manufacturer = cs.Manufacturer,
                ProductionYear = cs.ProductionYear,
                PurchaseDate = cs.PurchaseDate,
                PumpPiston = cs.PumpPiston,
                PumpDiaphragm = cs.PumpDiaphragm,
                PumpOther = cs.PumpOther,
                PumpOtherType = cs.PumpOtherType,
                PumpFlowRate = cs.PumpFlowRate,
                TankCapacity = cs.TankCapacity,
                HasFlushing = cs.HasFlushing,
                HasDiluter = cs.HasDiluter,
                HasWashingDevice = cs.HasWashingDevice,
                HasManometer = cs.HasManometer,
                HasComputer = cs.HasComputer,
                BoomWidth = cs.BoomWidth,
                BoomWet = cs.BoomWet,
                BoomDry = cs.BoomDry,
                BoomDampeningMechanism = cs.BoomDampeningMechanism,
                SectionCount = cs.SectionCount,
                NozzlesFieldFeatures = cs.NozzlesFieldFeatures,
                NozzlesGardenFeatures = cs.NozzlesGardenFeatures,
                FanType = cs.FanType,
                OwnerId = cs.OwnerId,
                OwnerName = ownerNameOverride ?? cs.OwnerName,
                CreatedAt = cs.CreatedAt,
                UpdatedAt = cs.UpdatedAt
            };
        }

        private static string BuildOwnerChangeDescription(string? oldOwnerId, string? oldOwnerName, string? newOwnerId, string? newOwnerName)
        {
            if (string.IsNullOrEmpty(oldOwnerId) && !string.IsNullOrEmpty(newOwnerId))
            {
                return $"Przypisano właściciela: {newOwnerName ?? newOwnerId}";
            }
            else if (!string.IsNullOrEmpty(oldOwnerId) && string.IsNullOrEmpty(newOwnerId))
            {
                return $"Usunięto właściciela: {oldOwnerName ?? oldOwnerId}";
            }
            else
            {
                return $"Zmieniono właściciela z \"{oldOwnerName ?? oldOwnerId}\" na \"{newOwnerName ?? newOwnerId}\"";
            }
        }

        private static string? ValidateDto(CropSprayerCreateUpdateDto dto, bool isCreate)
        {
            if (string.IsNullOrWhiteSpace(dto.SerialNumber) && isCreate)
                return "Numer seryjny/ewidencyjny jest wymagany";

            if (string.IsNullOrWhiteSpace(dto.SprayerName))
                return "Nazwa opryskiwacza jest wymagana";

            if (string.IsNullOrWhiteSpace(dto.Type) || (dto.Type != "00" && dto.Type != "01"))
                return "Nieprawidłowy typ (dozwolone: 00 - polowy, 01 - sadowniczy)";

            if (string.IsNullOrWhiteSpace(dto.Kind) || (dto.Kind != "00" && dto.Kind != "01" && dto.Kind != "02" && dto.Kind != "03"))
                return "Nieprawidłowy rodzaj (dozwolone: 00, 01, 02, 03)";

            if (string.IsNullOrWhiteSpace(dto.Manufacturer))
                return "Producent jest wymagany";

            if (string.IsNullOrWhiteSpace(dto.ProductionYear) || !YearRegex.IsMatch(dto.ProductionYear))
                return "Rok produkcji musi mieć dokładnie 4 cyfry";

            if (dto.PumpOther && string.IsNullOrWhiteSpace(dto.PumpOtherType))
                return "Dla pompy 'inna' należy podać typ";

            if (!dto.PumpPiston && !dto.PumpDiaphragm && !dto.PumpOther)
                return "Należy wybrać co najmniej jeden typ pompy";

            return null;
        }

        #endregion
    }
}
