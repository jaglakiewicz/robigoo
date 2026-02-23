using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    #region DTOs - CropSprayers

    public class CropSprayerListItemDto
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string SprayerName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ProductionYear { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CropSprayerDetailDto
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string SprayerName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ProductionYear { get; set; } = string.Empty;
        public DateTime? PurchaseDate { get; set; }
        public bool PumpPiston { get; set; }
        public bool PumpDiaphragm { get; set; }
        public bool PumpOther { get; set; }
        public string? PumpOtherType { get; set; }
        public decimal? PumpFlowRate { get; set; }
        public decimal? TankCapacity { get; set; }
        public bool HasFlushing { get; set; }
        public bool HasDiluter { get; set; }
        public bool HasWashingDevice { get; set; }
        public bool HasManometer { get; set; }
        public bool HasComputer { get; set; }
        public decimal? BoomWidth { get; set; }
        public bool BoomWet { get; set; }
        public bool BoomDry { get; set; }
        public bool BoomDampeningMechanism { get; set; }
        public int? SectionCount { get; set; }
        public string? NozzlesFieldFeatures { get; set; }
        public string? NozzlesGardenFeatures { get; set; }
        public string? FanType { get; set; }
        public string? OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating or updating a crop sprayer.
    /// </summary>
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class CropSprayerCreateUpdateDto
    {
        /// <summary>
        /// Serial/registration number (primary key).
        /// </summary>
        [Required(ErrorMessage = "Serial number is required")]
        [MaxLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Name of the sprayer.
        /// </summary>
        [Required(ErrorMessage = "Sprayer name is required")]
        [MaxLength(255, ErrorMessage = "Sprayer name cannot exceed 255 characters")]
        public string SprayerName { get; set; } = string.Empty;

        /// <summary>
        /// Type of sprayer: "00" = field, "01" = orchard.
        /// </summary>
        [Required(ErrorMessage = "Type is required")]
        [MaxLength(2, ErrorMessage = "Type cannot exceed 2 characters")]
        [RegularExpression("^(00|01)$", ErrorMessage = "Type must be '00' (field) or '01' (orchard)")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Kind of sprayer: "00" = mounted, "01" = trailed, "02" = self-propelled, "03" = other.
        /// </summary>
        [Required(ErrorMessage = "Kind is required")]
        [MaxLength(2, ErrorMessage = "Kind cannot exceed 2 characters")]
        [RegularExpression("^(00|01|02|03)$", ErrorMessage = "Kind must be '00', '01', '02', or '03'")]
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Manufacturer name.
        /// </summary>
        [Required(ErrorMessage = "Manufacturer is required")]
        [MaxLength(255, ErrorMessage = "Manufacturer cannot exceed 255 characters")]
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Production year as 4-digit text.
        /// </summary>
        [Required(ErrorMessage = "Production year is required")]
        [MaxLength(4, ErrorMessage = "Production year cannot exceed 4 characters")]
        [RegularExpression("^[0-9]{4}$", ErrorMessage = "Production year must be a 4-digit year")]
        [Range(1900, 2100, ErrorMessage = "Production year must be between 1900 and 2100")]
        public string ProductionYear { get; set; } = string.Empty;

        /// <summary>
        /// Date of purchase.
        /// </summary>
        public DateTime? PurchaseDate { get; set; }

        public bool PumpPiston { get; set; }
        public bool PumpDiaphragm { get; set; }
        public bool PumpOther { get; set; }

        /// <summary>
        /// Other pump type description.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Pump other type cannot exceed 100 characters")]
        public string? PumpOtherType { get; set; }

        /// <summary>
        /// Pump flow rate in liters per minute.
        /// </summary>
        [Range(0, 10000, ErrorMessage = "Pump flow rate must be between 0 and 10000 l/min")]
        public decimal? PumpFlowRate { get; set; }

        /// <summary>
        /// Tank capacity in liters.
        /// </summary>
        [Range(0, 50000, ErrorMessage = "Tank capacity must be between 0 and 50000 liters")]
        public decimal? TankCapacity { get; set; }

        public bool HasFlushing { get; set; }
        public bool HasDiluter { get; set; }
        public bool HasWashingDevice { get; set; }
        public bool HasManometer { get; set; }
        public bool HasComputer { get; set; }

        /// <summary>
        /// Boom width in meters.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Boom width must be between 0 and 100 meters")]
        public decimal? BoomWidth { get; set; }

        public bool BoomWet { get; set; }
        public bool BoomDry { get; set; }
        public bool BoomDampeningMechanism { get; set; }

        /// <summary>
        /// Number of sections.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Section count must be between 0 and 100")]
        public int? SectionCount { get; set; }

        /// <summary>
        /// Field nozzle features.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Nozzles field features cannot exceed 500 characters")]
        public string? NozzlesFieldFeatures { get; set; }

        /// <summary>
        /// Garden/orchard nozzle features.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Nozzles garden features cannot exceed 500 characters")]
        public string? NozzlesGardenFeatures { get; set; }

        /// <summary>
        /// Fan type for orchard sprayers.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Fan type cannot exceed 100 characters")]
        public string? FanType { get; set; }

        /// <summary>
        /// Owner client ID.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Owner ID cannot exceed 50 characters")]
        public string? OwnerId { get; set; }

        /// <summary>
        /// Owner name (for display purposes).
        /// </summary>
        [MaxLength(255, ErrorMessage = "Owner name cannot exceed 255 characters")]
        public string? OwnerName { get; set; }
    }

    #endregion
}
