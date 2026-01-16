/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

namespace Server.Models
{
    #region Client List Item DTO

    /// <summary>
    /// DTO for client list items.
    /// </summary>
    public class ClientListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string ClientType { get; set; } = "person";
        public string DisplayName { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Pesel { get; set; }
        public string? CompanyName { get; set; }
        public string? Nip { get; set; }
        public string? Regon { get; set; }
        public string? Voivodeship { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? BuildingNumber { get; set; }
        public string? ApartmentNumber { get; set; }
        public string? ZipCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Client Detail DTO

    /// <summary>
    /// DTO for full client details.
    /// </summary>
    public class ClientDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string ClientType { get; set; } = "person";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Pesel { get; set; }
        public string? CompanyName { get; set; }
        public string? Nip { get; set; }
        public string? Regon { get; set; }
        public string? Voivodeship { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? BuildingNumber { get; set; }
        public string? ApartmentNumber { get; set; }
        public string? ZipCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion

    #region Client Create/Update DTO

    /// <summary>
    /// DTO for creating or updating a client.
    /// </summary>
    public class ClientCreateUpdateDto
    {
        public string ClientType { get; set; } = "person";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Pesel { get; set; }
        public string? CompanyName { get; set; }
        public string? Nip { get; set; }
        public string? Regon { get; set; }
        public string? Voivodeship { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? BuildingNumber { get; set; }
        public string? ApartmentNumber { get; set; }
        public string? ZipCode { get; set; }
    }

    #endregion
}
