#region Imports

using System.ComponentModel.DataAnnotations;

#endregion

namespace Server.Models
{
    #region Client Model

    /// <summary>
    /// Represents a client (customer) - can be either a person or a company.
    /// </summary>
    public class Client
    {
        #region Properties

        /// <summary>
        /// Unique identifier (GUID string)
        /// </summary>
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of client: "person" or "company"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ClientType { get; set; } = "person";

        /// <summary>
        /// Display name (computed: FirstName + LastName for person, CompanyName for company)
        /// </summary>
        [MaxLength(500)]
        public string DisplayName { get; set; } = string.Empty;

        #region Person Fields

        [MaxLength(255)]
        public string? FirstName { get; set; }

        [MaxLength(255)]
        public string? LastName { get; set; }

        /// <summary>
        /// PESEL number (Polish national ID)
        /// </summary>
        [MaxLength(11)]
        public string? Pesel { get; set; }

        #endregion

        #region Company Fields

        [MaxLength(500)]
        public string? CompanyName { get; set; }

        /// <summary>
        /// NIP (Polish Tax Identification Number)
        /// </summary>
        [MaxLength(10)]
        public string? Nip { get; set; }

        /// <summary>
        /// REGON (Polish statistical number)
        /// </summary>
        [MaxLength(14)]
        public string? Regon { get; set; }

        #endregion

        #region Address Fields

        [MaxLength(100)]
        public string? Voivodeship { get; set; }

        [MaxLength(255)]
        public string? City { get; set; }

        [MaxLength(500)]
        public string? Street { get; set; }

        [MaxLength(20)]
        public string? BuildingNumber { get; set; }

        [MaxLength(20)]
        public string? ApartmentNumber { get; set; }

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        /// <summary>
        /// Post office name (Poczta)
        /// </summary>
        [MaxLength(255)]
        public string? Post { get; set; }

        #endregion

        #region Timestamps

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        #endregion

        #endregion
    }

    #endregion
}
