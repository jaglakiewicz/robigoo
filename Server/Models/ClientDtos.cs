/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using System.ComponentModel.DataAnnotations;

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
    /// <remarks>
    /// Server-side validation: Requirements 6.1, 6.4
    /// </remarks>
    public class ClientCreateUpdateDto
    {
        /// <summary>
        /// Type of client: "person" or "company".
        /// </summary>
        [Required(ErrorMessage = "Client type is required")]
        [MaxLength(20, ErrorMessage = "Client type cannot exceed 20 characters")]
        [RegularExpression("^(person|company)$", ErrorMessage = "Client type must be 'person' or 'company'")]
        public string ClientType { get; set; } = "person";

        /// <summary>
        /// First name (required for person type).
        /// </summary>
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name (required for person type).
        /// </summary>
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string? LastName { get; set; }

        /// <summary>
        /// PESEL number (Polish national ID, 11 digits).
        /// </summary>
        [MaxLength(11, ErrorMessage = "PESEL cannot exceed 11 characters")]
        [RegularExpression("^[0-9]{11}$", ErrorMessage = "PESEL must be exactly 11 digits")]
        public string? Pesel { get; set; }

        /// <summary>
        /// Company name (required for company type).
        /// </summary>
        [MaxLength(255, ErrorMessage = "Company name cannot exceed 255 characters")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// NIP number (Polish tax ID, 10 digits).
        /// </summary>
        [MaxLength(10, ErrorMessage = "NIP cannot exceed 10 characters")]
        [RegularExpression("^[0-9]{10}$", ErrorMessage = "NIP must be exactly 10 digits")]
        public string? Nip { get; set; }

        /// <summary>
        /// REGON number (Polish business registry, 9 or 14 digits).
        /// </summary>
        [MaxLength(14, ErrorMessage = "REGON cannot exceed 14 characters")]
        [RegularExpression("^[0-9]{9}([0-9]{5})?$", ErrorMessage = "REGON must be 9 or 14 digits")]
        public string? Regon { get; set; }

        /// <summary>
        /// Voivodeship (Polish administrative region).
        /// </summary>
        [MaxLength(100, ErrorMessage = "Voivodeship cannot exceed 100 characters")]
        public string? Voivodeship { get; set; }

        /// <summary>
        /// City name.
        /// </summary>
        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        /// <summary>
        /// Street name.
        /// </summary>
        [MaxLength(200, ErrorMessage = "Street cannot exceed 200 characters")]
        public string? Street { get; set; }

        /// <summary>
        /// Building number.
        /// </summary>
        [MaxLength(20, ErrorMessage = "Building number cannot exceed 20 characters")]
        public string? BuildingNumber { get; set; }

        /// <summary>
        /// Apartment number.
        /// </summary>
        [MaxLength(20, ErrorMessage = "Apartment number cannot exceed 20 characters")]
        public string? ApartmentNumber { get; set; }

        /// <summary>
        /// Zip/postal code (Polish format: XX-XXX).
        /// </summary>
        [MaxLength(10, ErrorMessage = "Zip code cannot exceed 10 characters")]
        [RegularExpression("^[0-9]{2}-[0-9]{3}$", ErrorMessage = "Zip code must be in format XX-XXX")]
        public string? ZipCode { get; set; }
    }

    #endregion
}
