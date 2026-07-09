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
using Server.Services;

#endregion

namespace Server.Controllers
{
    [ApiController]
    [Route("api/inspection-protocols")]
    [Authorize]
    public class InspectionProtocolsController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly ILogger<InspectionProtocolsController> _logger;
        private readonly IProtocolXmlService _xmlService;
        private readonly IConcurrencyController _concurrencyController;
        private readonly IDatabaseWriteQueue _writeQueue;

        #endregion

        #region Constructor

        public InspectionProtocolsController(
            AppDbContext context, 
            ILogger<InspectionProtocolsController> logger,
            IProtocolXmlService xmlService,
            IConcurrencyController concurrencyController,
            IDatabaseWriteQueue writeQueue)
        {
            _context = context;
            _logger = logger;
            _xmlService = xmlService;
            _concurrencyController = concurrencyController;
            _writeQueue = writeQueue;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Lista protokołów z filtrowaniem
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InspectionProtocolListItemDto>>> GetProtocols(
            [FromQuery] string? q,
            [FromQuery] string? clientId,
            [FromQuery] string? cropSprayerSerialNumber,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] bool? finalResult)
        {
            var query = _context.Set<InspectionProtocol>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.ProtocolNumber.ToLower().Contains(term) ||
                    (p.ClientName != null && p.ClientName.ToLower().Contains(term)) ||
                    (p.CropSprayerName != null && p.CropSprayerName.ToLower().Contains(term)) ||
                    (p.CropSprayerSerialNumber != null && p.CropSprayerSerialNumber.ToLower().Contains(term)) ||
                    p.InspectorName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(p => p.ClientId == clientId);
            }

            if (!string.IsNullOrWhiteSpace(cropSprayerSerialNumber))
            {
                query = query.Where(p => p.CropSprayerSerialNumber == cropSprayerSerialNumber);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(p => p.InspectionDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(p => p.InspectionDate <= dateTo.Value);
            }

            if (finalResult.HasValue)
            {
                query = query.Where(p => p.FinalResult == finalResult.Value);
            }

            var result = await query
                .OrderByDescending(p => p.InspectionDate)
                .ThenByDescending(p => p.CreatedAt)
                .Select(p => new InspectionProtocolListItemDto(
                    p.Id,
                    p.ProtocolNumber,
                    p.InspectionDate,
                    p.InspectorName,
                    p.ClientName,
                    p.CropSprayerName,
                    p.CropSprayerSerialNumber,
                    p.CropSprayerType,
                    p.FinalResult,
                    p.ValidUntil,
                    p.CreatedAt
                ))
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Rejestr przebadanego sprzętu za wybrany okres
        /// </summary>
        [HttpGet("registry")]
        public async Task<ActionResult<IEnumerable<EquipmentRegistryItemDto>>> GetRegistry(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            var query = _context.Set<InspectionProtocol>().AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(p => p.InspectionDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(p => p.InspectionDate <= dateTo.Value);

            var result = await query
                .OrderBy(p => p.InspectionDate)
                .ThenBy(p => p.ProtocolNumber)
                .Select(p => new EquipmentRegistryItemDto(
                    p.Id,
                    p.ProtocolNumber,
                    p.InspectionDate,
                    p.InspectorName,
                    p.ClientName,
                    p.ClientAddress,
                    p.ClientTaxId,
                    p.CropSprayerType,
                    p.CropSprayerKind,
                    p.CropSprayerManufacturer,
                    p.CropSprayerSerialNumber,
                    p.CropSprayerName,
                    p.CropSprayerProductionYear,
                    p.FinalResult,
                    p.ControlStickerNumber,
                    p.ValidUntil
                ))
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Ewidencja znaków kontrolnych za wybrany okres
        /// </summary>
        [HttpGet("control-marks")]
        public async Task<ActionResult<IEnumerable<ControlMarkRegistryItemDto>>> GetControlMarks(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            var query = _context.Set<InspectionProtocol>()
                .Where(p => p.ControlStickerNumber != null && p.ControlStickerNumber != "")
                .AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(p => p.InspectionDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(p => p.InspectionDate <= dateTo.Value);

            var result = await query
                .OrderBy(p => p.ControlStickerNumber)
                .ThenBy(p => p.InspectionDate)
                .Select(p => new ControlMarkRegistryItemDto(
                    p.Id,
                    p.ControlStickerNumber,
                    p.InspectionDate,
                    p.ClientName,
                    p.ProtocolNumber
                ))
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Szczegóły protokołu
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<ActionResult<InspectionProtocolResponseDto>> GetProtocol(long id)
        {
            var protocol = await _context.Set<InspectionProtocol>().FindAsync(id);

            if (protocol == null)
                return NotFound();

            return Ok(ToResponseDto(protocol));
        }

        /// <summary>
        /// Wygeneruj następny numer protokołu
        /// </summary>
        [HttpGet("next-number")]
        public async Task<ActionResult<object>> GetNextProtocolNumber()
        {
            var year = DateTime.UtcNow.Year;
            var suffix = $"/{year}";

            var lastProtocol = await _context.Set<InspectionProtocol>()
                .Where(p => p.ProtocolNumber.EndsWith(suffix))
                .OrderByDescending(p => p.ProtocolNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastProtocol != null)
            {
                var lastNumberPart = lastProtocol.ProtocolNumber.Replace(suffix, "");
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            var nextProtocolNumber = $"{nextNumber:D4}{suffix}";
            return Ok(new { protocolNumber = nextProtocolNumber });
        }

        /// <summary>
        /// Utworzenie nowego protokołu
        /// Uses write queue for critical database operations.
        /// Requirements: 5.3 - Critical write operations (inspection protocol saves) should be queued
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InspectionProtocolResponseDto>> CreateProtocol([FromBody] InspectionProtocolCreateDto dto)
        {
            InspectionProtocol? protocol = null;
            QueuedOperationResult? queueResult = null;

            // Create the write operation for the queue
            var writeOperation = new WriteOperation
            {
                EntityType = "InspectionProtocol",
                EntityId = "new",
                Timeout = TimeSpan.FromSeconds(30),
                Operation = async (cancellationToken) =>
                {
                    // Generate protocol number
                    var year = dto.InspectionDate.Year;
                    var suffix = $"/{year}";

                    var lastProtocol = await _context.Set<InspectionProtocol>()
                        .Where(p => p.ProtocolNumber.EndsWith(suffix))
                        .OrderByDescending(p => p.ProtocolNumber)
                        .FirstOrDefaultAsync(cancellationToken);

                    int nextNumber = 1;
                    if (lastProtocol != null)
                    {
                        var lastNumberPart = lastProtocol.ProtocolNumber.Replace(suffix, "");
                        if (int.TryParse(lastNumberPart, out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                        }
                    }

                    var protocolNumber = $"{nextNumber:D4}{suffix}";

                    protocol = new InspectionProtocol
                    {
                        ProtocolNumber = protocolNumber,
                        InspectionDate = dto.InspectionDate,
                        InspectionLocation = dto.InspectionLocation,
                        InspectorName = dto.InspectorName,
                        InspectorLicenseNumber = dto.InspectorLicenseNumber,

                        ClientId = dto.ClientId,
                        ClientName = dto.ClientName,
                        ClientAddress = dto.ClientAddress,
                        ClientTaxId = dto.ClientTaxId,

                        CropSprayerSerialNumber = dto.CropSprayerSerialNumber,
                        CropSprayerName = dto.CropSprayerName,
                        CropSprayerType = dto.CropSprayerType,
                        CropSprayerKind = dto.CropSprayerKind,
                        CropSprayerManufacturer = dto.CropSprayerManufacturer,
                        CropSprayerProductionYear = dto.CropSprayerProductionYear,
                        TankCapacity = dto.TankCapacity,
                        BoomWidth = dto.BoomWidth,
                        SectionCount = dto.SectionCount,

                        GeneralConditionPassed = dto.GeneralConditionPassed,
                        GeneralConditionPassedDriveOn = dto.GeneralConditionPassedDriveOn,
                        MarkingsReadablePassed = dto.MarkingsReadablePassed,
                        MarkingsReadablePassedDriveOn = dto.MarkingsReadablePassedDriveOn,
                        EquipmentCompletePassed = dto.EquipmentCompletePassed,
                        EquipmentCompletePassedDriveOn = dto.EquipmentCompletePassedDriveOn,
                        GeneralSectionNotes = dto.GeneralSectionNotes,

                        PumpOperationPassed = dto.PumpOperationPassed,
                        PumpOperationPassedDriveOn = dto.PumpOperationPassedDriveOn,
                        PumpSealingPassed = dto.PumpSealingPassed,
                        PumpSealingPassedDriveOn = dto.PumpSealingPassedDriveOn,
                        PressurePulsationPassed = dto.PressurePulsationPassed,
                        PressurePulsationPassedDriveOn = dto.PressurePulsationPassedDriveOn,
                        PumpSectionNotes = dto.PumpSectionNotes,

                        AgitatorOperationPassed = dto.AgitatorOperationPassed,
                        AgitatorOperationPassedDriveOn = dto.AgitatorOperationPassedDriveOn,
                        AgitatorSectionNotes = dto.AgitatorSectionNotes,

                        TankConditionPassed = dto.TankConditionPassed,
                        TankConditionPassedDriveOn = dto.TankConditionPassedDriveOn,
                        TankSealingPassed = dto.TankSealingPassed,
                        TankSealingPassedDriveOn = dto.TankSealingPassedDriveOn,
                        LevelIndicatorPassed = dto.LevelIndicatorPassed,
                        LevelIndicatorPassedDriveOn = dto.LevelIndicatorPassedDriveOn,
                        FlushingSystemPassed = dto.FlushingSystemPassed,
                        FlushingSystemPassedDriveOn = dto.FlushingSystemPassedDriveOn,
                        TankSectionNotes = dto.TankSectionNotes,

                        ManometerPassed = dto.ManometerPassed,
                        ManometerPassedDriveOn = dto.ManometerPassedDriveOn,
                        ManometerReading2Bar = dto.ManometerReading2Bar,
                        ManometerReading4Bar = dto.ManometerReading4Bar,
                        ManometerReading6Bar = dto.ManometerReading6Bar,
                        ManometerDialSizePassed = dto.ManometerDialSizePassed,
                        ManometerDialSizePassedDriveOn = dto.ManometerDialSizePassedDriveOn,
                        MeasuringSectionNotes = dto.MeasuringSectionNotes,

                        PipesConditionPassed = dto.PipesConditionPassed,
                        PipesConditionPassedDriveOn = dto.PipesConditionPassedDriveOn,
                        ConnectionsSealingPassed = dto.ConnectionsSealingPassed,
                        ConnectionsSealingPassedDriveOn = dto.ConnectionsSealingPassedDriveOn,
                        PipingSectionNotes = dto.PipingSectionNotes,

                        SuctionFilterPassed = dto.SuctionFilterPassed,
                        SuctionFilterPassedDriveOn = dto.SuctionFilterPassedDriveOn,
                        PressureFilterPassed = dto.PressureFilterPassed,
                        PressureFilterPassedDriveOn = dto.PressureFilterPassedDriveOn,
                        NozzleFiltersPassed = dto.NozzleFiltersPassed,
                        NozzleFiltersPassedDriveOn = dto.NozzleFiltersPassedDriveOn,
                        FiltrationSectionNotes = dto.FiltrationSectionNotes,

                        FieldBoomConditionPassed = dto.FieldBoomConditionPassed,
                        FieldBoomConditionPassedDriveOn = dto.FieldBoomConditionPassedDriveOn,
                        BoomStabilityPassed = dto.BoomStabilityPassed,
                        BoomStabilityPassedDriveOn = dto.BoomStabilityPassedDriveOn,
                        BoomHeightPassed = dto.BoomHeightPassed,
                        BoomHeightPassedDriveOn = dto.BoomHeightPassedDriveOn,
                        BoomSymmetryPassed = dto.BoomSymmetryPassed,
                        BoomSymmetryPassedDriveOn = dto.BoomSymmetryPassedDriveOn,
                        OrchardSprayerConditionPassed = dto.OrchardSprayerConditionPassed,
                        OrchardSprayerConditionPassedDriveOn = dto.OrchardSprayerConditionPassedDriveOn,
                        AirStreamDirectionPassed = dto.AirStreamDirectionPassed,
                        AirStreamDirectionPassedDriveOn = dto.AirStreamDirectionPassedDriveOn,
                        BoomSectionNotes = dto.BoomSectionNotes,

                        NozzleUniformityPassed = dto.NozzleUniformityPassed,
                        NozzleUniformityPassedDriveOn = dto.NozzleUniformityPassedDriveOn,
                        NozzleFlowRatePassed = dto.NozzleFlowRatePassed,
                        NozzleFlowRatePassedDriveOn = dto.NozzleFlowRatePassedDriveOn,
                        NozzleConditionPassed = dto.NozzleConditionPassed,
                        NozzleConditionPassedDriveOn = dto.NozzleConditionPassedDriveOn,
                        NozzleMeasurements = dto.NozzleMeasurements,
                        NozzlesSectionNotes = dto.NozzlesSectionNotes,

                        TransverseDistributionPassed = dto.TransverseDistributionPassed,
                        TransverseDistributionPassedDriveOn = dto.TransverseDistributionPassedDriveOn,
                        CoefficientOfVariation = dto.CoefficientOfVariation,
                        DistributionSectionNotes = dto.DistributionSectionNotes,

                        FinalResult = dto.FinalResult,
                        ValidUntil = dto.ValidUntil,
                        ControlStickerNumber = dto.ControlStickerNumber,
                        GeneralNotes = dto.GeneralNotes,

                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Set<InspectionProtocol>().Add(protocol);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            };

            // Enqueue the operation and get queue position
            queueResult = await _writeQueue.EnqueueAsync(writeOperation);

            _logger.LogInformation(
                "Enqueued InspectionProtocol create operation {OperationId}. Queue position: {QueuePosition}",
                queueResult.OperationId,
                queueResult.QueuePosition);

            // Wait for the operation to complete
            await writeOperation.CompletionSource.Task;

            if (protocol == null)
            {
                _logger.LogError("Protocol creation failed - protocol is null after queue processing");
                return StatusCode(500, new { error = "Failed to create protocol" });
            }

            // Return response with queue information in headers
            Response.Headers.Append("X-Queue-Operation-Id", queueResult.OperationId);
            Response.Headers.Append("X-Queue-Position", queueResult.QueuePosition.ToString());

            return CreatedAtAction(nameof(GetProtocol), new { id = protocol.Id }, ToResponseDto(protocol));
        }

        /// <summary>
        /// Aktualizacja protokołu
        /// Implements optimistic concurrency control with 409 Conflict on concurrent modification.
        /// Uses write queue for critical database operations.
        /// Requirements: 4.3, 4.6, 5.3
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<ActionResult<InspectionProtocolResponseDto>> UpdateProtocol(long id, [FromBody] InspectionProtocolCreateDto dto)
        {
            var protocol = await _context.Set<InspectionProtocol>().FindAsync(id);
            if (protocol == null)
                return NotFound();

            InspectionProtocol? updatedProtocol = null;
            ConcurrencyResult<InspectionProtocol>? concurrencyResult = null;
            QueuedOperationResult? queueResult = null;

            // Create the write operation for the queue
            var writeOperation = new WriteOperation
            {
                EntityType = "InspectionProtocol",
                EntityId = id.ToString(),
                Timeout = TimeSpan.FromSeconds(30),
                Operation = async (cancellationToken) =>
                {
                    // Execute update with optimistic concurrency control
                    // Requirement 4.3: IF a concurrency conflict is detected, THEN THE Backend SHALL return a 409 Conflict response with details
                    // Requirement 4.6: WHEN saving inspection protocols, THE Database_Access_Layer SHALL acquire appropriate locks to prevent race conditions
                    concurrencyResult = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
                    {
                        // Re-fetch the entity to ensure we have the latest version
                        var entity = await _context.Set<InspectionProtocol>().FindAsync(new object[] { id }, cancellationToken);
                        if (entity == null)
                            throw new InvalidOperationException("Entity not found during update");

                        entity.InspectionDate = dto.InspectionDate;
                        entity.InspectionLocation = dto.InspectionLocation;
                        entity.InspectorName = dto.InspectorName;
                        entity.InspectorLicenseNumber = dto.InspectorLicenseNumber;

                        entity.ClientId = dto.ClientId;
                        entity.ClientName = dto.ClientName;
                        entity.ClientAddress = dto.ClientAddress;
                        entity.ClientTaxId = dto.ClientTaxId;

                        entity.CropSprayerSerialNumber = dto.CropSprayerSerialNumber;
                        entity.CropSprayerName = dto.CropSprayerName;
                        entity.CropSprayerType = dto.CropSprayerType;
                        entity.CropSprayerKind = dto.CropSprayerKind;
                        entity.CropSprayerManufacturer = dto.CropSprayerManufacturer;
                        entity.CropSprayerProductionYear = dto.CropSprayerProductionYear;
                        entity.TankCapacity = dto.TankCapacity;
                        entity.BoomWidth = dto.BoomWidth;
                        entity.SectionCount = dto.SectionCount;

                        entity.GeneralConditionPassed = dto.GeneralConditionPassed;
                        entity.GeneralConditionPassedDriveOn = dto.GeneralConditionPassedDriveOn;
                        entity.MarkingsReadablePassed = dto.MarkingsReadablePassed;
                        entity.MarkingsReadablePassedDriveOn = dto.MarkingsReadablePassedDriveOn;
                        entity.EquipmentCompletePassed = dto.EquipmentCompletePassed;
                        entity.EquipmentCompletePassedDriveOn = dto.EquipmentCompletePassedDriveOn;
                        entity.GeneralSectionNotes = dto.GeneralSectionNotes;

                        entity.PumpOperationPassed = dto.PumpOperationPassed;
                        entity.PumpOperationPassedDriveOn = dto.PumpOperationPassedDriveOn;
                        entity.PumpSealingPassed = dto.PumpSealingPassed;
                        entity.PumpSealingPassedDriveOn = dto.PumpSealingPassedDriveOn;
                        entity.PressurePulsationPassed = dto.PressurePulsationPassed;
                        entity.PressurePulsationPassedDriveOn = dto.PressurePulsationPassedDriveOn;
                        entity.PumpSectionNotes = dto.PumpSectionNotes;

                        entity.AgitatorOperationPassed = dto.AgitatorOperationPassed;
                        entity.AgitatorOperationPassedDriveOn = dto.AgitatorOperationPassedDriveOn;
                        entity.AgitatorSectionNotes = dto.AgitatorSectionNotes;

                        entity.TankConditionPassed = dto.TankConditionPassed;
                        entity.TankConditionPassedDriveOn = dto.TankConditionPassedDriveOn;
                        entity.TankSealingPassed = dto.TankSealingPassed;
                        entity.TankSealingPassedDriveOn = dto.TankSealingPassedDriveOn;
                        entity.LevelIndicatorPassed = dto.LevelIndicatorPassed;
                        entity.LevelIndicatorPassedDriveOn = dto.LevelIndicatorPassedDriveOn;
                        entity.FlushingSystemPassed = dto.FlushingSystemPassed;
                        entity.FlushingSystemPassedDriveOn = dto.FlushingSystemPassedDriveOn;
                        entity.TankSectionNotes = dto.TankSectionNotes;

                        entity.ManometerPassed = dto.ManometerPassed;
                        entity.ManometerPassedDriveOn = dto.ManometerPassedDriveOn;
                        entity.ManometerReading2Bar = dto.ManometerReading2Bar;
                        entity.ManometerReading4Bar = dto.ManometerReading4Bar;
                        entity.ManometerReading6Bar = dto.ManometerReading6Bar;
                        entity.ManometerDialSizePassed = dto.ManometerDialSizePassed;
                        entity.ManometerDialSizePassedDriveOn = dto.ManometerDialSizePassedDriveOn;
                        entity.MeasuringSectionNotes = dto.MeasuringSectionNotes;

                        entity.PipesConditionPassed = dto.PipesConditionPassed;
                        entity.PipesConditionPassedDriveOn = dto.PipesConditionPassedDriveOn;
                        entity.ConnectionsSealingPassed = dto.ConnectionsSealingPassed;
                        entity.ConnectionsSealingPassedDriveOn = dto.ConnectionsSealingPassedDriveOn;
                        entity.PipingSectionNotes = dto.PipingSectionNotes;

                        entity.SuctionFilterPassed = dto.SuctionFilterPassed;
                        entity.SuctionFilterPassedDriveOn = dto.SuctionFilterPassedDriveOn;
                        entity.PressureFilterPassed = dto.PressureFilterPassed;
                        entity.PressureFilterPassedDriveOn = dto.PressureFilterPassedDriveOn;
                        entity.NozzleFiltersPassed = dto.NozzleFiltersPassed;
                        entity.NozzleFiltersPassedDriveOn = dto.NozzleFiltersPassedDriveOn;
                        entity.FiltrationSectionNotes = dto.FiltrationSectionNotes;

                        entity.FieldBoomConditionPassed = dto.FieldBoomConditionPassed;
                        entity.FieldBoomConditionPassedDriveOn = dto.FieldBoomConditionPassedDriveOn;
                        entity.BoomStabilityPassed = dto.BoomStabilityPassed;
                        entity.BoomStabilityPassedDriveOn = dto.BoomStabilityPassedDriveOn;
                        entity.BoomHeightPassed = dto.BoomHeightPassed;
                        entity.BoomHeightPassedDriveOn = dto.BoomHeightPassedDriveOn;
                        entity.BoomSymmetryPassed = dto.BoomSymmetryPassed;
                        entity.BoomSymmetryPassedDriveOn = dto.BoomSymmetryPassedDriveOn;
                        entity.OrchardSprayerConditionPassed = dto.OrchardSprayerConditionPassed;
                        entity.OrchardSprayerConditionPassedDriveOn = dto.OrchardSprayerConditionPassedDriveOn;
                        entity.AirStreamDirectionPassed = dto.AirStreamDirectionPassed;
                        entity.AirStreamDirectionPassedDriveOn = dto.AirStreamDirectionPassedDriveOn;
                        entity.BoomSectionNotes = dto.BoomSectionNotes;

                        entity.NozzleUniformityPassed = dto.NozzleUniformityPassed;
                        entity.NozzleUniformityPassedDriveOn = dto.NozzleUniformityPassedDriveOn;
                        entity.NozzleFlowRatePassed = dto.NozzleFlowRatePassed;
                        entity.NozzleFlowRatePassedDriveOn = dto.NozzleFlowRatePassedDriveOn;
                        entity.NozzleConditionPassed = dto.NozzleConditionPassed;
                        entity.NozzleConditionPassedDriveOn = dto.NozzleConditionPassedDriveOn;
                        entity.NozzleMeasurements = dto.NozzleMeasurements;
                        entity.NozzlesSectionNotes = dto.NozzlesSectionNotes;

                        entity.TransverseDistributionPassed = dto.TransverseDistributionPassed;
                        entity.TransverseDistributionPassedDriveOn = dto.TransverseDistributionPassedDriveOn;
                        entity.CoefficientOfVariation = dto.CoefficientOfVariation;
                        entity.DistributionSectionNotes = dto.DistributionSectionNotes;

                        entity.FinalResult = dto.FinalResult;
                        entity.ValidUntil = dto.ValidUntil;
                        entity.ControlStickerNumber = dto.ControlStickerNumber;
                        entity.GeneralNotes = dto.GeneralNotes;

                        entity.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync(cancellationToken);

                        return entity;
                    });

                    updatedProtocol = concurrencyResult?.Result;
                }
            };

            // Enqueue the operation and get queue position
            queueResult = await _writeQueue.EnqueueAsync(writeOperation);

            _logger.LogInformation(
                "Enqueued InspectionProtocol update operation {OperationId} for protocol {ProtocolId}. Queue position: {QueuePosition}",
                queueResult.OperationId,
                id,
                queueResult.QueuePosition);

            // Wait for the operation to complete
            await writeOperation.CompletionSource.Task;

            // Add queue information to response headers
            Response.Headers.Append("X-Queue-Operation-Id", queueResult.OperationId);
            Response.Headers.Append("X-Queue-Position", queueResult.QueuePosition.ToString());

            // Check if concurrency conflict occurred
            if (concurrencyResult != null && !concurrencyResult.Success)
            {
                _logger.LogWarning(
                    "Concurrency conflict detected for InspectionProtocol {Id}: {Message}",
                    id,
                    concurrencyResult.Conflict?.Message);

                return Conflict(new
                {
                    error = "CONCURRENCY_CONFLICT",
                    message = concurrencyResult.Conflict?.Message ?? "The record was modified by another user. Please refresh and try again.",
                    entityType = concurrencyResult.Conflict?.EntityType ?? "InspectionProtocol",
                    entityId = concurrencyResult.Conflict?.EntityId ?? id.ToString(),
                    conflictTime = concurrencyResult.Conflict?.ConflictTime ?? DateTime.UtcNow,
                    queueOperationId = queueResult.OperationId
                });
            }

            if (updatedProtocol == null)
            {
                _logger.LogError("Protocol update failed - protocol is null after queue processing");
                return StatusCode(500, new { error = "Failed to update protocol" });
            }

            return Ok(ToResponseDto(updatedProtocol));
        }

        /// <summary>
        /// Usunięcie protokołu
        /// </summary>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteProtocol(long id)
        {
            var protocol = await _context.Set<InspectionProtocol>().FindAsync(id);
            if (protocol == null)
                return NotFound();

            _context.Set<InspectionProtocol>().Remove(protocol);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get XML data for a protocol (for PDF generation)
        /// </summary>
        [HttpGet("{id:long}/xml")]
        public async Task<ActionResult<ProtocolXmlResponseDto>> GetProtocolXml(long id)
        {
            var protocol = await _context.Set<InspectionProtocol>().FindAsync(id);
            if (protocol == null)
                return NotFound();

            // Generate fresh XML or return stored XML
            var xml = !string.IsNullOrEmpty(protocol.ProtocolXml) 
                ? protocol.ProtocolXml 
                : _xmlService.GenerateXml(protocol);

            var xslVersion = protocol.XslTemplateVersion ?? _xmlService.GetCurrentXslVersion();

            return Ok(new ProtocolXmlResponseDto(
                protocol.Id,
                protocol.ProtocolNumber,
                xml,
                xslVersion,
                protocol.XmlGeneratedAt ?? DateTime.UtcNow
            ));
        }

        /// <summary>
        /// Get XSL template for a specific version
        /// </summary>
        [HttpGet("xsl/{version}")]
        public ActionResult<XslTemplateResponseDto> GetXslTemplate(string version)
        {
            try
            {
                var xsl = _xmlService.GetXslTemplate(version);
                return Ok(new XslTemplateResponseDto(version, xsl));
            }
            catch (FileNotFoundException)
            {
                return NotFound($"XSL template version {version} not found");
            }
        }

        /// <summary>
        /// Get current XSL template version
        /// </summary>
        [HttpGet("xsl/current-version")]
        public ActionResult<object> GetCurrentXslVersion()
        {
            return Ok(new { 
                version = _xmlService.GetCurrentXslVersion(),
                availableVersions = _xmlService.GetAvailableXslVersions()
            });
        }

        /// <summary>
        /// Generate and store XML for a protocol (call when saving)
        /// </summary>
        [HttpPost("{id:long}/generate-xml")]
        public async Task<ActionResult<ProtocolXmlResponseDto>> GenerateProtocolXml(long id)
        {
            var protocol = await _context.Set<InspectionProtocol>().FindAsync(id);
            if (protocol == null)
                return NotFound();

            // Generate XML
            var xml = _xmlService.GenerateXml(protocol);
            var xslVersion = _xmlService.GetCurrentXslVersion();

            // Store in database
            protocol.ProtocolXml = xml;
            protocol.XslTemplateVersion = xslVersion;
            protocol.XmlGeneratedAt = DateTime.UtcNow;
            protocol.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated XML for protocol {ProtocolId} with XSL version {XslVersion}", id, xslVersion);

            return Ok(new ProtocolXmlResponseDto(
                protocol.Id,
                protocol.ProtocolNumber,
                xml,
                xslVersion,
                protocol.XmlGeneratedAt.Value
            ));
        }

        /// <summary>
        /// Preview protocol as XML (without saving) for current form data
        /// </summary>
        [HttpPost("preview-xml")]
        public ActionResult<ProtocolXmlPreviewResponseDto> PreviewProtocolXml([FromBody] InspectionProtocolCreateDto dto)
        {
            // Create a temporary protocol object from DTO
            var tempProtocol = new InspectionProtocol
            {
                ProtocolNumber = dto.ProtocolNumber ?? "PREVIEW",
                InspectionDate = dto.InspectionDate,
                InspectionLocation = dto.InspectionLocation,
                InspectorName = dto.InspectorName,
                InspectorLicenseNumber = dto.InspectorLicenseNumber,
                ClientId = dto.ClientId,
                ClientName = dto.ClientName,
                ClientAddress = dto.ClientAddress,
                ClientTaxId = dto.ClientTaxId,
                CropSprayerSerialNumber = dto.CropSprayerSerialNumber,
                CropSprayerName = dto.CropSprayerName,
                CropSprayerType = dto.CropSprayerType,
                CropSprayerKind = dto.CropSprayerKind,
                CropSprayerManufacturer = dto.CropSprayerManufacturer,
                CropSprayerProductionYear = dto.CropSprayerProductionYear,
                TankCapacity = dto.TankCapacity,
                BoomWidth = dto.BoomWidth,
                SectionCount = dto.SectionCount,
                GeneralConditionPassed = dto.GeneralConditionPassed,
                MarkingsReadablePassed = dto.MarkingsReadablePassed,
                EquipmentCompletePassed = dto.EquipmentCompletePassed,
                GeneralSectionNotes = dto.GeneralSectionNotes,
                PumpOperationPassed = dto.PumpOperationPassed,
                PumpSealingPassed = dto.PumpSealingPassed,
                PressurePulsationPassed = dto.PressurePulsationPassed,
                PumpSectionNotes = dto.PumpSectionNotes,
                AgitatorOperationPassed = dto.AgitatorOperationPassed,
                AgitatorSectionNotes = dto.AgitatorSectionNotes,
                TankConditionPassed = dto.TankConditionPassed,
                TankSealingPassed = dto.TankSealingPassed,
                LevelIndicatorPassed = dto.LevelIndicatorPassed,
                FlushingSystemPassed = dto.FlushingSystemPassed,
                TankSectionNotes = dto.TankSectionNotes,
                ManometerPassed = dto.ManometerPassed,
                ManometerReading2Bar = dto.ManometerReading2Bar,
                ManometerReading4Bar = dto.ManometerReading4Bar,
                ManometerReading6Bar = dto.ManometerReading6Bar,
                ManometerDialSizePassed = dto.ManometerDialSizePassed,
                MeasuringSectionNotes = dto.MeasuringSectionNotes,
                PipesConditionPassed = dto.PipesConditionPassed,
                ConnectionsSealingPassed = dto.ConnectionsSealingPassed,
                PipingSectionNotes = dto.PipingSectionNotes,
                SuctionFilterPassed = dto.SuctionFilterPassed,
                PressureFilterPassed = dto.PressureFilterPassed,
                NozzleFiltersPassed = dto.NozzleFiltersPassed,
                FiltrationSectionNotes = dto.FiltrationSectionNotes,
                FieldBoomConditionPassed = dto.FieldBoomConditionPassed,
                BoomStabilityPassed = dto.BoomStabilityPassed,
                BoomHeightPassed = dto.BoomHeightPassed,
                BoomSymmetryPassed = dto.BoomSymmetryPassed,
                OrchardSprayerConditionPassed = dto.OrchardSprayerConditionPassed,
                AirStreamDirectionPassed = dto.AirStreamDirectionPassed,
                BoomSectionNotes = dto.BoomSectionNotes,
                NozzleUniformityPassed = dto.NozzleUniformityPassed,
                NozzleFlowRatePassed = dto.NozzleFlowRatePassed,
                NozzleConditionPassed = dto.NozzleConditionPassed,
                NozzleMeasurements = dto.NozzleMeasurements,
                NozzlesSectionNotes = dto.NozzlesSectionNotes,
                TransverseDistributionPassed = dto.TransverseDistributionPassed,
                CoefficientOfVariation = dto.CoefficientOfVariation,
                DistributionSectionNotes = dto.DistributionSectionNotes,
                FinalResult = dto.FinalResult,
                ValidUntil = dto.ValidUntil,
                ControlStickerNumber = dto.ControlStickerNumber,
                GeneralNotes = dto.GeneralNotes,
                CreatedAt = DateTime.UtcNow
            };

            var xml = _xmlService.GenerateXml(tempProtocol);
            var xsl = _xmlService.GetXslTemplate(_xmlService.GetCurrentXslVersion());

            return Ok(new ProtocolXmlPreviewResponseDto(
                xml,
                xsl,
                _xmlService.GetCurrentXslVersion()
            ));
        }

        #endregion

        #region Methods - Private

        private static InspectionProtocolResponseDto ToResponseDto(InspectionProtocol p)
        {
            return new InspectionProtocolResponseDto(
                p.Id,
                p.ProtocolNumber,
                p.InspectionDate,
                p.InspectionLocation,
                p.InspectorName,
                p.InspectorLicenseNumber,

                p.ClientId,
                p.ClientName,
                p.ClientAddress,
                p.ClientTaxId,

                p.CropSprayerSerialNumber,
                p.CropSprayerName,
                p.CropSprayerType,
                p.CropSprayerKind,
                p.CropSprayerManufacturer,
                p.CropSprayerProductionYear,
                p.TankCapacity,
                p.BoomWidth,
                p.SectionCount,

                p.GeneralConditionPassed,
                p.GeneralConditionPassedDriveOn,
                p.MarkingsReadablePassed,
                p.MarkingsReadablePassedDriveOn,
                p.EquipmentCompletePassed,
                p.EquipmentCompletePassedDriveOn,
                p.GeneralSectionNotes,

                p.PumpOperationPassed,
                p.PumpOperationPassedDriveOn,
                p.PumpSealingPassed,
                p.PumpSealingPassedDriveOn,
                p.PressurePulsationPassed,
                p.PressurePulsationPassedDriveOn,
                p.PumpSectionNotes,

                p.AgitatorOperationPassed,
                p.AgitatorOperationPassedDriveOn,
                p.AgitatorSectionNotes,

                p.TankConditionPassed,
                p.TankConditionPassedDriveOn,
                p.TankSealingPassed,
                p.TankSealingPassedDriveOn,
                p.LevelIndicatorPassed,
                p.LevelIndicatorPassedDriveOn,
                p.FlushingSystemPassed,
                p.FlushingSystemPassedDriveOn,
                p.TankSectionNotes,

                p.ManometerPassed,
                p.ManometerPassedDriveOn,
                p.ManometerReading2Bar,
                p.ManometerReading4Bar,
                p.ManometerReading6Bar,
                p.ManometerDialSizePassed,
                p.ManometerDialSizePassedDriveOn,
                p.MeasuringSectionNotes,

                p.PipesConditionPassed,
                p.PipesConditionPassedDriveOn,
                p.ConnectionsSealingPassed,
                p.ConnectionsSealingPassedDriveOn,
                p.PipingSectionNotes,

                p.SuctionFilterPassed,
                p.SuctionFilterPassedDriveOn,
                p.PressureFilterPassed,
                p.PressureFilterPassedDriveOn,
                p.NozzleFiltersPassed,
                p.NozzleFiltersPassedDriveOn,
                p.FiltrationSectionNotes,

                p.FieldBoomConditionPassed,
                p.FieldBoomConditionPassedDriveOn,
                p.BoomStabilityPassed,
                p.BoomStabilityPassedDriveOn,
                p.BoomHeightPassed,
                p.BoomHeightPassedDriveOn,
                p.BoomSymmetryPassed,
                p.BoomSymmetryPassedDriveOn,
                p.OrchardSprayerConditionPassed,
                p.OrchardSprayerConditionPassedDriveOn,
                p.AirStreamDirectionPassed,
                p.AirStreamDirectionPassedDriveOn,
                p.BoomSectionNotes,

                p.NozzleUniformityPassed,
                p.NozzleUniformityPassedDriveOn,
                p.NozzleFlowRatePassed,
                p.NozzleFlowRatePassedDriveOn,
                p.NozzleConditionPassed,
                p.NozzleConditionPassedDriveOn,
                p.NozzleMeasurements,
                p.NozzlesSectionNotes,

                p.TransverseDistributionPassed,
                p.TransverseDistributionPassedDriveOn,
                p.CoefficientOfVariation,
                p.DistributionSectionNotes,

                p.FinalResult,
                p.ValidUntil,
                p.ControlStickerNumber,
                p.GeneralNotes,

                p.CreatedAt,
                p.UpdatedAt
            );
        }

        #endregion
    }
}
