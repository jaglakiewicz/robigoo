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

        #endregion

        #region Constructor

        public InspectionProtocolsController(
            AppDbContext context, 
            ILogger<InspectionProtocolsController> logger,
            IProtocolXmlService xmlService)
        {
            _context = context;
            _logger = logger;
            _xmlService = xmlService;
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
            var prefix = $"SKO/{year}/";

            var lastProtocol = await _context.Set<InspectionProtocol>()
                .Where(p => p.ProtocolNumber.StartsWith(prefix))
                .OrderByDescending(p => p.ProtocolNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastProtocol != null)
            {
                var lastNumberPart = lastProtocol.ProtocolNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            var nextProtocolNumber = $"{prefix}{nextNumber:D3}";
            return Ok(new { protocolNumber = nextProtocolNumber });
        }

        /// <summary>
        /// Utworzenie nowego protokołu
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InspectionProtocolResponseDto>> CreateProtocol([FromBody] InspectionProtocolCreateDto dto)
        {
            // Generate protocol number
            var year = dto.InspectionDate.Year;
            var prefix = $"SKO/{year}/";

            var lastProtocol = await _context.Set<InspectionProtocol>()
                .Where(p => p.ProtocolNumber.StartsWith(prefix))
                .OrderByDescending(p => p.ProtocolNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastProtocol != null)
            {
                var lastNumberPart = lastProtocol.ProtocolNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            var protocolNumber = $"{prefix}{nextNumber:D3}";

            var protocol = new InspectionProtocol
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

            _context.Set<InspectionProtocol>().Add(protocol);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProtocol), new { id = protocol.Id }, ToResponseDto(protocol));
        }

        /// <summary>
        /// Aktualizacja protokołu
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<ActionResult<InspectionProtocolResponseDto>> UpdateProtocol(long id, [FromBody] InspectionProtocolCreateDto dto)
        {
            var protocol = await _context.Set<InspectionProtocol>().FindAsync(id);
            if (protocol == null)
                return NotFound();

            protocol.InspectionDate = dto.InspectionDate;
            protocol.InspectionLocation = dto.InspectionLocation;
            protocol.InspectorName = dto.InspectorName;
            protocol.InspectorLicenseNumber = dto.InspectorLicenseNumber;

            protocol.ClientId = dto.ClientId;
            protocol.ClientName = dto.ClientName;
            protocol.ClientAddress = dto.ClientAddress;
            protocol.ClientTaxId = dto.ClientTaxId;

            protocol.CropSprayerSerialNumber = dto.CropSprayerSerialNumber;
            protocol.CropSprayerName = dto.CropSprayerName;
            protocol.CropSprayerType = dto.CropSprayerType;
            protocol.CropSprayerKind = dto.CropSprayerKind;
            protocol.CropSprayerManufacturer = dto.CropSprayerManufacturer;
            protocol.CropSprayerProductionYear = dto.CropSprayerProductionYear;
            protocol.TankCapacity = dto.TankCapacity;
            protocol.BoomWidth = dto.BoomWidth;
            protocol.SectionCount = dto.SectionCount;

            protocol.GeneralConditionPassed = dto.GeneralConditionPassed;
            protocol.MarkingsReadablePassed = dto.MarkingsReadablePassed;
            protocol.EquipmentCompletePassed = dto.EquipmentCompletePassed;
            protocol.GeneralSectionNotes = dto.GeneralSectionNotes;

            protocol.PumpOperationPassed = dto.PumpOperationPassed;
            protocol.PumpSealingPassed = dto.PumpSealingPassed;
            protocol.PressurePulsationPassed = dto.PressurePulsationPassed;
            protocol.PumpSectionNotes = dto.PumpSectionNotes;

            protocol.AgitatorOperationPassed = dto.AgitatorOperationPassed;
            protocol.AgitatorSectionNotes = dto.AgitatorSectionNotes;

            protocol.TankConditionPassed = dto.TankConditionPassed;
            protocol.TankSealingPassed = dto.TankSealingPassed;
            protocol.LevelIndicatorPassed = dto.LevelIndicatorPassed;
            protocol.FlushingSystemPassed = dto.FlushingSystemPassed;
            protocol.TankSectionNotes = dto.TankSectionNotes;

            protocol.ManometerPassed = dto.ManometerPassed;
            protocol.ManometerReading2Bar = dto.ManometerReading2Bar;
            protocol.ManometerReading4Bar = dto.ManometerReading4Bar;
            protocol.ManometerReading6Bar = dto.ManometerReading6Bar;
            protocol.ManometerDialSizePassed = dto.ManometerDialSizePassed;
            protocol.MeasuringSectionNotes = dto.MeasuringSectionNotes;

            protocol.PipesConditionPassed = dto.PipesConditionPassed;
            protocol.ConnectionsSealingPassed = dto.ConnectionsSealingPassed;
            protocol.PipingSectionNotes = dto.PipingSectionNotes;

            protocol.SuctionFilterPassed = dto.SuctionFilterPassed;
            protocol.PressureFilterPassed = dto.PressureFilterPassed;
            protocol.NozzleFiltersPassed = dto.NozzleFiltersPassed;
            protocol.FiltrationSectionNotes = dto.FiltrationSectionNotes;

            protocol.FieldBoomConditionPassed = dto.FieldBoomConditionPassed;
            protocol.BoomStabilityPassed = dto.BoomStabilityPassed;
            protocol.BoomHeightPassed = dto.BoomHeightPassed;
            protocol.BoomSymmetryPassed = dto.BoomSymmetryPassed;
            protocol.OrchardSprayerConditionPassed = dto.OrchardSprayerConditionPassed;
            protocol.AirStreamDirectionPassed = dto.AirStreamDirectionPassed;
            protocol.BoomSectionNotes = dto.BoomSectionNotes;

            protocol.NozzleUniformityPassed = dto.NozzleUniformityPassed;
            protocol.NozzleFlowRatePassed = dto.NozzleFlowRatePassed;
            protocol.NozzleConditionPassed = dto.NozzleConditionPassed;
            protocol.NozzleMeasurements = dto.NozzleMeasurements;
            protocol.NozzlesSectionNotes = dto.NozzlesSectionNotes;

            protocol.TransverseDistributionPassed = dto.TransverseDistributionPassed;
            protocol.CoefficientOfVariation = dto.CoefficientOfVariation;
            protocol.DistributionSectionNotes = dto.DistributionSectionNotes;

            protocol.FinalResult = dto.FinalResult;
            protocol.ValidUntil = dto.ValidUntil;
            protocol.ControlStickerNumber = dto.ControlStickerNumber;
            protocol.GeneralNotes = dto.GeneralNotes;

            protocol.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ToResponseDto(protocol));
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
                p.MarkingsReadablePassed,
                p.EquipmentCompletePassed,
                p.GeneralSectionNotes,

                p.PumpOperationPassed,
                p.PumpSealingPassed,
                p.PressurePulsationPassed,
                p.PumpSectionNotes,

                p.AgitatorOperationPassed,
                p.AgitatorSectionNotes,

                p.TankConditionPassed,
                p.TankSealingPassed,
                p.LevelIndicatorPassed,
                p.FlushingSystemPassed,
                p.TankSectionNotes,

                p.ManometerPassed,
                p.ManometerReading2Bar,
                p.ManometerReading4Bar,
                p.ManometerReading6Bar,
                p.ManometerDialSizePassed,
                p.MeasuringSectionNotes,

                p.PipesConditionPassed,
                p.ConnectionsSealingPassed,
                p.PipingSectionNotes,

                p.SuctionFilterPassed,
                p.PressureFilterPassed,
                p.NozzleFiltersPassed,
                p.FiltrationSectionNotes,

                p.FieldBoomConditionPassed,
                p.BoomStabilityPassed,
                p.BoomHeightPassed,
                p.BoomSymmetryPassed,
                p.OrchardSprayerConditionPassed,
                p.AirStreamDirectionPassed,
                p.BoomSectionNotes,

                p.NozzleUniformityPassed,
                p.NozzleFlowRatePassed,
                p.NozzleConditionPassed,
                p.NozzleMeasurements,
                p.NozzlesSectionNotes,

                p.TransverseDistributionPassed,
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
