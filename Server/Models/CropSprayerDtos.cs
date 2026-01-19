/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

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

    public class CropSprayerCreateUpdateDto
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
    }

    #endregion
}
