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
    [Route("api/[controller]")]
    [Authorize]
    public class MachinesController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;

        private static readonly Regex YearRegex = new("^\\d{4}$", RegexOptions.Compiled);

        #endregion

        #region Constructor

        public MachinesController(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Lista maszyn z prostym filtrem tekstowym i po typie/rodzaju.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MachineListItemDto>>> GetMachines(
            [FromQuery] string? q,
            [FromQuery] string? type,
            [FromQuery] string? kind,
            [FromQuery] string? manufacturer,
            [FromQuery] string? yearFrom,
            [FromQuery] string? yearTo)
        {
            var query = _context.Machines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                query = query.Where(m =>
                    m.SerialNumber.ToLower().Contains(term) ||
                    m.SprayerName.ToLower().Contains(term) ||
                    m.Manufacturer.ToLower().Contains(term) ||
                    m.ProductionYear.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(m => m.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(kind))
            {
                query = query.Where(m => m.Kind == kind);
            }

            if (!string.IsNullOrWhiteSpace(manufacturer))
            {
                var manufacturerTerm = manufacturer.Trim().ToLowerInvariant();
                query = query.Where(m => m.Manufacturer.ToLower().Contains(manufacturerTerm));
            }

            if (!string.IsNullOrWhiteSpace(yearFrom) && YearRegex.IsMatch(yearFrom))
            {
                query = query.Where(m => string.Compare(m.ProductionYear, yearFrom) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(yearTo) && YearRegex.IsMatch(yearTo))
            {
                query = query.Where(m => string.Compare(m.ProductionYear, yearTo) <= 0);
            }

            var result = await query
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MachineListItemDto
                {
                    SerialNumber = m.SerialNumber,
                    SprayerName = m.SprayerName,
                    Manufacturer = m.Manufacturer,
                    ProductionYear = m.ProductionYear,
                    Type = m.Type,
                    Kind = m.Kind,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Szczegóły maszyny.
        /// </summary>
        [HttpGet("{serialNumber}")]
        public async Task<ActionResult<MachineDetailDto>> GetMachine(string serialNumber)
        {
            var machine = await _context.Machines.FindAsync(serialNumber);

            if (machine == null)
                return NotFound();

            return Ok(ToDetailDto(machine));
        }

        /// <summary>
        /// Dodanie nowej maszyny.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<MachineDetailDto>> CreateMachine([FromBody] MachineCreateUpdateDto dto)
        {
            var validationError = ValidateDto(dto, isCreate: true);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            if (await _context.Machines.AnyAsync(m => m.SerialNumber == dto.SerialNumber))
                return BadRequest(new { message = "Maszyna o podanym numerze już istnieje" });

            var machine = new Machine
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.Machines.Add(machine);
            await _context.SaveChangesAsync();

            var detail = ToDetailDto(machine);
            return CreatedAtAction(nameof(GetMachine), new { serialNumber = machine.SerialNumber }, detail);
        }

        /// <summary>
        /// Aktualizacja istniejącej maszyny.
        /// </summary>
        [HttpPut("{serialNumber}")]
        public async Task<ActionResult<MachineDetailDto>> UpdateMachine(string serialNumber, [FromBody] MachineCreateUpdateDto dto)
        {
            if (!string.Equals(serialNumber, dto.SerialNumber, StringComparison.Ordinal))
                return BadRequest(new { message = "Nie można zmienić numeru seryjnego maszyny" });

            var machine = await _context.Machines.FindAsync(serialNumber);
            if (machine == null)
                return NotFound();

            var validationError = ValidateDto(dto, isCreate: false);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            machine.SprayerName = dto.SprayerName.Trim();
            machine.Type = dto.Type;
            machine.Kind = dto.Kind;
            machine.Manufacturer = dto.Manufacturer.Trim();
            machine.ProductionYear = dto.ProductionYear;
            machine.PurchaseDate = dto.PurchaseDate;
            machine.PumpPiston = dto.PumpPiston;
            machine.PumpDiaphragm = dto.PumpDiaphragm;
            machine.PumpOther = dto.PumpOther;
            machine.PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim();
            machine.PumpFlowRate = dto.PumpFlowRate;
            machine.TankCapacity = dto.TankCapacity;
            machine.HasFlushing = dto.HasFlushing;
            machine.HasDiluter = dto.HasDiluter;
            machine.HasWashingDevice = dto.HasWashingDevice;
            machine.HasManometer = dto.HasManometer;
            machine.HasComputer = dto.HasComputer;
            machine.BoomWidth = dto.BoomWidth;
            machine.BoomWet = dto.BoomWet;
            machine.BoomDry = dto.BoomDry;
            machine.BoomDampeningMechanism = dto.BoomDampeningMechanism;
            machine.SectionCount = dto.SectionCount;
            machine.NozzlesFieldFeatures = dto.NozzlesFieldFeatures;
            machine.NozzlesGardenFeatures = dto.NozzlesGardenFeatures;
            machine.FanType = dto.FanType;
            machine.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ToDetailDto(machine));
        }

        /// <summary>
        /// Usunięcie maszyny.
        /// </summary>
        [HttpDelete("{serialNumber}")]
        public async Task<IActionResult> DeleteMachine(string serialNumber)
        {
            var machine = await _context.Machines.FindAsync(serialNumber);
            if (machine == null)
                return NotFound();

            _context.Machines.Remove(machine);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Methods - Private

        private static MachineDetailDto ToDetailDto(Machine m)
        {
            return new MachineDetailDto
            {
                SerialNumber = m.SerialNumber,
                SprayerName = m.SprayerName,
                Type = m.Type,
                Kind = m.Kind,
                Manufacturer = m.Manufacturer,
                ProductionYear = m.ProductionYear,
                PurchaseDate = m.PurchaseDate,
                PumpPiston = m.PumpPiston,
                PumpDiaphragm = m.PumpDiaphragm,
                PumpOther = m.PumpOther,
                PumpOtherType = m.PumpOtherType,
                PumpFlowRate = m.PumpFlowRate,
                TankCapacity = m.TankCapacity,
                HasFlushing = m.HasFlushing,
                HasDiluter = m.HasDiluter,
                HasWashingDevice = m.HasWashingDevice,
                HasManometer = m.HasManometer,
                HasComputer = m.HasComputer,
                BoomWidth = m.BoomWidth,
                BoomWet = m.BoomWet,
                BoomDry = m.BoomDry,
                BoomDampeningMechanism = m.BoomDampeningMechanism,
                SectionCount = m.SectionCount,
                NozzlesFieldFeatures = m.NozzlesFieldFeatures,
                NozzlesGardenFeatures = m.NozzlesGardenFeatures,
                FanType = m.FanType,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            };
        }

        private static string? ValidateDto(MachineCreateUpdateDto dto, bool isCreate)
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
