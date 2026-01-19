/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
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

        private static readonly Regex YearRegex = new("^\\d{4}$", RegexOptions.Compiled);

        #endregion

        #region Constructor

        public CropSprayersController(AppDbContext context, ILogger<CropSprayersController> logger)
        {
            _context = context;
            _logger = logger;
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

            // Capture old owner values for history tracking
            var oldOwnerId = cropSprayer.OwnerId;
            var oldOwnerName = cropSprayer.OwnerName;
            
            _logger.LogInformation("Old values - OwnerId: {OwnerId}, OwnerName: {OwnerName}", oldOwnerId, oldOwnerName);

            var validationError = ValidateDto(dto, isCreate: false);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            cropSprayer.SprayerName = dto.SprayerName.Trim();
            cropSprayer.Type = dto.Type;
            cropSprayer.Kind = dto.Kind;
            cropSprayer.Manufacturer = dto.Manufacturer.Trim();
            cropSprayer.ProductionYear = dto.ProductionYear;
            cropSprayer.PurchaseDate = dto.PurchaseDate;
            cropSprayer.PumpPiston = dto.PumpPiston;
            cropSprayer.PumpDiaphragm = dto.PumpDiaphragm;
            cropSprayer.PumpOther = dto.PumpOther;
            cropSprayer.PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim();
            cropSprayer.PumpFlowRate = dto.PumpFlowRate;
            cropSprayer.TankCapacity = dto.TankCapacity;
            cropSprayer.HasFlushing = dto.HasFlushing;
            cropSprayer.HasDiluter = dto.HasDiluter;
            cropSprayer.HasWashingDevice = dto.HasWashingDevice;
            cropSprayer.HasManometer = dto.HasManometer;
            cropSprayer.HasComputer = dto.HasComputer;
            cropSprayer.BoomWidth = dto.BoomWidth;
            cropSprayer.BoomWet = dto.BoomWet;
            cropSprayer.BoomDry = dto.BoomDry;
            cropSprayer.BoomDampeningMechanism = dto.BoomDampeningMechanism;
            cropSprayer.SectionCount = dto.SectionCount;
            cropSprayer.NozzlesFieldFeatures = dto.NozzlesFieldFeatures;
            cropSprayer.NozzlesGardenFeatures = dto.NozzlesGardenFeatures;
            cropSprayer.FanType = dto.FanType;

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

            cropSprayer.OwnerId = dto.OwnerId;
            cropSprayer.OwnerName = dto.OwnerName;
            cropSprayer.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("After assignment - CropSprayer.OwnerId: {OwnerId}, CropSprayer.OwnerName: {OwnerName}", cropSprayer.OwnerId, cropSprayer.OwnerName);

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("After SaveChangesAsync - CropSprayer.OwnerId: {OwnerId}, CropSprayer.OwnerName: {OwnerName}", cropSprayer.OwnerId, cropSprayer.OwnerName);

            // Look up real owner name from Clients table
            string? realOwnerName = null;
            if (!string.IsNullOrEmpty(cropSprayer.OwnerId))
            {
                var owner = await _context.Clients.FindAsync(cropSprayer.OwnerId);
                realOwnerName = owner?.DisplayName;
            }

            var result = ToDetailDto(cropSprayer, realOwnerName);
            _logger.LogInformation("ToDetailDto result - OwnerId: {OwnerId}, OwnerName: {OwnerName}", result.OwnerId, result.OwnerName);

            return Ok(result);
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
