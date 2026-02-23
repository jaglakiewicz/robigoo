using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    #region DTOs - InspectionProtocol Create

    /// <summary>
    /// DTO do tworzenia nowego protokołu badania
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public record InspectionProtocolCreateDto(
        // Basic data
        [property: MaxLength(50, ErrorMessage = "Protocol number cannot exceed 50 characters")]
        string? ProtocolNumber,  // Optional - used for preview
        
        [property: Required(ErrorMessage = "Inspection date is required")]
        DateTime InspectionDate,
        
        [property: MaxLength(255, ErrorMessage = "Inspection location cannot exceed 255 characters")]
        string? InspectionLocation,
        
        [property: Required(ErrorMessage = "Inspector name is required")]
        [property: MaxLength(255, ErrorMessage = "Inspector name cannot exceed 255 characters")]
        string InspectorName,
        
        [property: MaxLength(50, ErrorMessage = "Inspector license number cannot exceed 50 characters")]
        string? InspectorLicenseNumber,

        // Client data (either from existing client or manual entry)
        [property: MaxLength(50, ErrorMessage = "Client ID cannot exceed 50 characters")]
        string? ClientId,
        
        [property: MaxLength(255, ErrorMessage = "Client name cannot exceed 255 characters")]
        string? ClientName,
        
        [property: MaxLength(500, ErrorMessage = "Client address cannot exceed 500 characters")]
        string? ClientAddress,
        
        [property: MaxLength(20, ErrorMessage = "Client tax ID cannot exceed 20 characters")]
        string? ClientTaxId,

        // CropSprayer data (either from existing sprayer or manual entry)
        [property: MaxLength(100, ErrorMessage = "Crop sprayer serial number cannot exceed 100 characters")]
        string? CropSprayerSerialNumber,
        
        [property: MaxLength(255, ErrorMessage = "Crop sprayer name cannot exceed 255 characters")]
        string? CropSprayerName,
        
        [property: MaxLength(2, ErrorMessage = "Crop sprayer type cannot exceed 2 characters")]
        string? CropSprayerType,
        
        [property: MaxLength(2, ErrorMessage = "Crop sprayer kind cannot exceed 2 characters")]
        string? CropSprayerKind,
        
        [property: MaxLength(255, ErrorMessage = "Crop sprayer manufacturer cannot exceed 255 characters")]
        string? CropSprayerManufacturer,
        
        [property: MaxLength(4, ErrorMessage = "Crop sprayer production year cannot exceed 4 characters")]
        string? CropSprayerProductionYear,
        
        [property: Range(0, 50000, ErrorMessage = "Tank capacity must be between 0 and 50000 liters")]
        decimal? TankCapacity,
        
        [property: Range(0, 100, ErrorMessage = "Boom width must be between 0 and 100 meters")]
        decimal? BoomWidth,
        
        [property: Range(0, 100, ErrorMessage = "Section count must be between 0 and 100")]
        int? SectionCount,

        // Section 1: General
        bool? GeneralConditionPassed,
        bool? MarkingsReadablePassed,
        bool? EquipmentCompletePassed,
        
        [property: MaxLength(1000, ErrorMessage = "General section notes cannot exceed 1000 characters")]
        string? GeneralSectionNotes,

        // Section 2: Pump
        bool? PumpOperationPassed,
        bool? PumpSealingPassed,
        bool? PressurePulsationPassed,
        
        [property: MaxLength(1000, ErrorMessage = "Pump section notes cannot exceed 1000 characters")]
        string? PumpSectionNotes,

        // Section 3: Agitation
        bool? AgitatorOperationPassed,
        
        [property: MaxLength(1000, ErrorMessage = "Agitator section notes cannot exceed 1000 characters")]
        string? AgitatorSectionNotes,

        // Section 4: Tank
        bool? TankConditionPassed,
        bool? TankSealingPassed,
        bool? LevelIndicatorPassed,
        bool? FlushingSystemPassed,
        
        [property: MaxLength(1000, ErrorMessage = "Tank section notes cannot exceed 1000 characters")]
        string? TankSectionNotes,

        // Section 5: Measuring
        bool? ManometerPassed,
        
        [property: Range(0, 20, ErrorMessage = "Manometer reading must be between 0 and 20 bar")]
        decimal? ManometerReading2Bar,
        
        [property: Range(0, 20, ErrorMessage = "Manometer reading must be between 0 and 20 bar")]
        decimal? ManometerReading4Bar,
        
        [property: Range(0, 20, ErrorMessage = "Manometer reading must be between 0 and 20 bar")]
        decimal? ManometerReading6Bar,
        
        bool? ManometerDialSizePassed,
        
        [property: MaxLength(1000, ErrorMessage = "Measuring section notes cannot exceed 1000 characters")]
        string? MeasuringSectionNotes,

        // Section 6: Piping
        bool? PipesConditionPassed,
        bool? ConnectionsSealingPassed,
        
        [property: MaxLength(1000, ErrorMessage = "Piping section notes cannot exceed 1000 characters")]
        string? PipingSectionNotes,

        // Section 7: Filtration
        bool? SuctionFilterPassed,
        bool? PressureFilterPassed,
        bool? NozzleFiltersPassed,
        
        [property: MaxLength(1000, ErrorMessage = "Filtration section notes cannot exceed 1000 characters")]
        string? FiltrationSectionNotes,

        // Section 8: Boom/Spray Equipment
        bool? FieldBoomConditionPassed,
        bool? BoomStabilityPassed,
        bool? BoomHeightPassed,
        bool? BoomSymmetryPassed,
        bool? OrchardSprayerConditionPassed,
        bool? AirStreamDirectionPassed,
        
        [property: MaxLength(1000, ErrorMessage = "Boom section notes cannot exceed 1000 characters")]
        string? BoomSectionNotes,

        // Section 9: Nozzles
        bool? NozzleUniformityPassed,
        bool? NozzleFlowRatePassed,
        bool? NozzleConditionPassed,
        
        [property: MaxLength(2000, ErrorMessage = "Nozzle measurements cannot exceed 2000 characters")]
        string? NozzleMeasurements,
        
        [property: MaxLength(1000, ErrorMessage = "Nozzles section notes cannot exceed 1000 characters")]
        string? NozzlesSectionNotes,

        // Section 10: Distribution
        bool? TransverseDistributionPassed,
        
        [property: Range(0, 100, ErrorMessage = "Coefficient of variation must be between 0 and 100%")]
        decimal? CoefficientOfVariation,
        
        [property: MaxLength(1000, ErrorMessage = "Distribution section notes cannot exceed 1000 characters")]
        string? DistributionSectionNotes,

        // Final result
        bool? FinalResult,
        DateTime? ValidUntil,
        
        [property: MaxLength(50, ErrorMessage = "Control sticker number cannot exceed 50 characters")]
        string? ControlStickerNumber,
        
        [property: MaxLength(2000, ErrorMessage = "General notes cannot exceed 2000 characters")]
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
