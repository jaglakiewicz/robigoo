/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

#endregion

namespace Server.Models
{
    #region DTOs - Inspection

    public record InspectionItemDto(string Description, bool Passed);
    public record InspectionCreateDto(string VehiclePlate, string InspectorName, DateTime InspectionDate, string? Notes, List<InspectionItemDto> Items);

    #endregion

    #region DTOs - Inspection Response

    // Response DTO to avoid circular references
    public record InspectionResponseDto(
        long Id,
        string VehiclePlate,
        string InspectorName,
        DateTime InspectionDate,
        string? Notes,
        List<InspectionItemDto> Items
    );

    #endregion
}

