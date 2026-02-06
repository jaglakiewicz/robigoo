/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

namespace Server.Models
{
    #region DTOs - InspectionProtocol Create

    /// <summary>
    /// DTO do tworzenia nowego protokołu badania
    /// </summary>
    public record InspectionProtocolCreateDto(
        // Basic data
        string? ProtocolNumber,  // Optional - used for preview
        DateTime InspectionDate,
        string? InspectionLocation,
        string InspectorName,
        string? InspectorLicenseNumber,

        // Client data (either from existing client or manual entry)
        string? ClientId,
        string? ClientName,
        string? ClientAddress,
        string? ClientTaxId,

        // CropSprayer data (either from existing sprayer or manual entry)
        string? CropSprayerSerialNumber,
        string? CropSprayerName,
        string? CropSprayerType,
        string? CropSprayerKind,
        string? CropSprayerManufacturer,
        string? CropSprayerProductionYear,
        decimal? TankCapacity,
        decimal? BoomWidth,
        int? SectionCount,

        // Section 1: General
        bool? GeneralConditionPassed,
        bool? MarkingsReadablePassed,
        bool? EquipmentCompletePassed,
        string? GeneralSectionNotes,

        // Section 2: Pump
        bool? PumpOperationPassed,
        bool? PumpSealingPassed,
        bool? PressurePulsationPassed,
        string? PumpSectionNotes,

        // Section 3: Agitation
        bool? AgitatorOperationPassed,
        string? AgitatorSectionNotes,

        // Section 4: Tank
        bool? TankConditionPassed,
        bool? TankSealingPassed,
        bool? LevelIndicatorPassed,
        bool? FlushingSystemPassed,
        string? TankSectionNotes,

        // Section 5: Measuring
        bool? ManometerPassed,
        decimal? ManometerReading2Bar,
        decimal? ManometerReading4Bar,
        decimal? ManometerReading6Bar,
        bool? ManometerDialSizePassed,
        string? MeasuringSectionNotes,

        // Section 6: Piping
        bool? PipesConditionPassed,
        bool? ConnectionsSealingPassed,
        string? PipingSectionNotes,

        // Section 7: Filtration
        bool? SuctionFilterPassed,
        bool? PressureFilterPassed,
        bool? NozzleFiltersPassed,
        string? FiltrationSectionNotes,

        // Section 8: Boom/Spray Equipment
        bool? FieldBoomConditionPassed,
        bool? BoomStabilityPassed,
        bool? BoomHeightPassed,
        bool? BoomSymmetryPassed,
        bool? OrchardSprayerConditionPassed,
        bool? AirStreamDirectionPassed,
        string? BoomSectionNotes,

        // Section 9: Nozzles
        bool? NozzleUniformityPassed,
        bool? NozzleFlowRatePassed,
        bool? NozzleConditionPassed,
        string? NozzleMeasurements,
        string? NozzlesSectionNotes,

        // Section 10: Distribution
        bool? TransverseDistributionPassed,
        decimal? CoefficientOfVariation,
        string? DistributionSectionNotes,

        // Final result
        bool? FinalResult,
        DateTime? ValidUntil,
        string? ControlStickerNumber,
        string? GeneralNotes
    );

    #endregion

    #region DTOs - InspectionProtocol Response

    /// <summary>
    /// DTO odpowiedzi z pełnymi danymi protokołu
    /// </summary>
    public record InspectionProtocolResponseDto(
        long Id,
        string ProtocolNumber,
        DateTime InspectionDate,
        string? InspectionLocation,
        string InspectorName,
        string? InspectorLicenseNumber,

        // Client
        string? ClientId,
        string? ClientName,
        string? ClientAddress,
        string? ClientTaxId,

        // CropSprayer
        string? CropSprayerSerialNumber,
        string? CropSprayerName,
        string? CropSprayerType,
        string? CropSprayerKind,
        string? CropSprayerManufacturer,
        string? CropSprayerProductionYear,
        decimal? TankCapacity,
        decimal? BoomWidth,
        int? SectionCount,

        // Section 1
        bool? GeneralConditionPassed,
        bool? MarkingsReadablePassed,
        bool? EquipmentCompletePassed,
        string? GeneralSectionNotes,

        // Section 2
        bool? PumpOperationPassed,
        bool? PumpSealingPassed,
        bool? PressurePulsationPassed,
        string? PumpSectionNotes,

        // Section 3
        bool? AgitatorOperationPassed,
        string? AgitatorSectionNotes,

        // Section 4
        bool? TankConditionPassed,
        bool? TankSealingPassed,
        bool? LevelIndicatorPassed,
        bool? FlushingSystemPassed,
        string? TankSectionNotes,

        // Section 5
        bool? ManometerPassed,
        decimal? ManometerReading2Bar,
        decimal? ManometerReading4Bar,
        decimal? ManometerReading6Bar,
        bool? ManometerDialSizePassed,
        string? MeasuringSectionNotes,

        // Section 6
        bool? PipesConditionPassed,
        bool? ConnectionsSealingPassed,
        string? PipingSectionNotes,

        // Section 7
        bool? SuctionFilterPassed,
        bool? PressureFilterPassed,
        bool? NozzleFiltersPassed,
        string? FiltrationSectionNotes,

        // Section 8
        bool? FieldBoomConditionPassed,
        bool? BoomStabilityPassed,
        bool? BoomHeightPassed,
        bool? BoomSymmetryPassed,
        bool? OrchardSprayerConditionPassed,
        bool? AirStreamDirectionPassed,
        string? BoomSectionNotes,

        // Section 9
        bool? NozzleUniformityPassed,
        bool? NozzleFlowRatePassed,
        bool? NozzleConditionPassed,
        string? NozzleMeasurements,
        string? NozzlesSectionNotes,

        // Section 10
        bool? TransverseDistributionPassed,
        decimal? CoefficientOfVariation,
        string? DistributionSectionNotes,

        // Final
        bool? FinalResult,
        DateTime? ValidUntil,
        string? ControlStickerNumber,
        string? GeneralNotes,

        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    #endregion

    #region DTOs - InspectionProtocol List Item

    /// <summary>
    /// DTO dla listy protokołów (uproszczona wersja)
    /// </summary>
    public record InspectionProtocolListItemDto(
        long Id,
        string ProtocolNumber,
        DateTime InspectionDate,
        string InspectorName,
        string? ClientName,
        string? CropSprayerName,
        string? CropSprayerSerialNumber,
        string? CropSprayerType,
        bool? FinalResult,
        DateTime? ValidUntil,
        DateTime CreatedAt
    );

    #endregion

    #region DTOs - XML/PDF

    /// <summary>
    /// DTO for protocol XML response
    /// </summary>
    public record ProtocolXmlResponseDto(
        long Id,
        string ProtocolNumber,
        string Xml,
        string XslVersion,
        DateTime GeneratedAt
    );

    /// <summary>
    /// DTO for XSL template response
    /// </summary>
    public record XslTemplateResponseDto(
        string Version,
        string Xsl
    );

    /// <summary>
    /// DTO for protocol XML preview (includes both XML and XSL for client-side rendering)
    /// </summary>
    public record ProtocolXmlPreviewResponseDto(
        string Xml,
        string Xsl,
        string XslVersion
    );

    #endregion
}
