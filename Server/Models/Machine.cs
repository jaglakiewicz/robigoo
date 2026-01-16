/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using System.ComponentModel.DataAnnotations;

#endregion

namespace Server.Models
{
    #region Machine Model

    public class Machine
    {
        #region Properties

        [Key]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty; // nr seryjny / ewidencyjny (PK)

        [Required]
        [MaxLength(255)]
        public string SprayerName { get; set; } = string.Empty; // nazwa opryskiwacza

        /// <summary>
        /// Typ opryskiwacza: "00" = polowy, "01" = sadowniczy
        /// </summary>
        [Required]
        [MaxLength(2)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Rodzaj: "00" = zawieszany, "01" = przyczepiany, "02" = samobieżny, "03" = inny
        /// </summary>
        [Required]
        [MaxLength(2)]
        public string Kind { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Rok produkcji jako 4-cyfrowy tekst, np. "2024".
        /// </summary>
        [Required]
        [MaxLength(4)]
        public string ProductionYear { get; set; } = string.Empty;

        public DateTime? PurchaseDate { get; set; }
        public bool PumpPiston { get; set; }
        public bool PumpDiaphragm { get; set; }
        public bool PumpOther { get; set; }

        [MaxLength(255)]
        public string? PumpOtherType { get; set; }

        public decimal? PumpFlowRate { get; set; } // dm3/min
        public decimal? TankCapacity { get; set; } // litry

        public bool HasFlushing { get; set; }
        public bool HasDiluter { get; set; }
        public bool HasWashingDevice { get; set; }
        public bool HasManometer { get; set; }
        public bool HasComputer { get; set; }

        public decimal? BoomWidth { get; set; } // metry
        public bool BoomWet { get; set; }
        public bool BoomDry { get; set; }
        public bool BoomDampeningMechanism { get; set; }

        public int? SectionCount { get; set; }

        [MaxLength(1000)]
        public string? NozzlesFieldFeatures { get; set; }

        [MaxLength(1000)]
        public string? NozzlesGardenFeatures { get; set; }

        [MaxLength(255)]
        public string? FanType { get; set; }

        /// <summary>
        /// ID właściciela/klienta (opcjonalne powiązanie z klientem).
        /// </summary>
        [MaxLength(100)]
        public string? OwnerId { get; set; }

        /// <summary>
        /// Nazwa właściciela (denormalizowana dla wygody).
        /// </summary>
        [MaxLength(500)]
        public string? OwnerName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        #endregion
    }

    #endregion
}
