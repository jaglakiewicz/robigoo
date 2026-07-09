using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    /// <summary>
    /// Single-row application settings stored as a JSON blob.
    /// Adding new fields to AppSettingsData never requires a schema migration.
    /// </summary>
    public class AppSettings
    {
        [Key]
        public int Id { get; set; } = 1;

        /// <summary>JSON-serialized AppSettingsData</summary>
        public string DataJson { get; set; } = "{}";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    // ── Strongly-typed settings tree ──────────────────────────────────────

    public class AppSettingsData
    {
        public OrganizationSettings Organization { get; set; } = new();
        public DocumentsSettings Documents { get; set; } = new();
    }

    // ── 1. Organization ───────────────────────────────────────────────────
    public class OrganizationSettings
    {
        public string StationName { get; set; } = string.Empty;
        public string UnitAuthorizationNumber { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string PostOffice { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;   // NIP
        public string Regon { get; set; } = string.Empty;
    }

    // ── 2. Documents ──────────────────────────────────────────────────────
    public class DocumentsSettings
    {
        public ProtocolDocSettings Protocol { get; set; } = new();
        public DocumentPrintSettings Register { get; set; } = new();
        public DocumentPrintSettings ControlMarks { get; set; } = new();
    }

    public class ProtocolDocSettings
    {
        public int InspectionValidityYears { get; set; } = 3;
        public string NumberPrefix { get; set; } = string.Empty;
        public int SequencePadding { get; set; } = 3;
        public string NumberFormat { get; set; } = "{PREFIX}/{YEAR}/{SEQ}";
        public string Header { get; set; } = string.Empty;
        public string Footer { get; set; } = string.Empty;
    }

    public class DocumentPrintSettings
    {
        public string Header { get; set; } = string.Empty;
        public string Footer { get; set; } = string.Empty;
    }
}
