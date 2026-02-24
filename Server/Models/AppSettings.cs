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
        public PrintingSettings Printing { get; set; } = new();
        public ProtocolSettings Protocols { get; set; } = new();
        public OrganizationSettings Organization { get; set; } = new();
        public DisplaySettings Display { get; set; } = new();
        public DataSettings Data { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public NotificationSettings Notifications { get; set; } = new();
    }

    // ── 1. Printing ───────────────────────────────────────────────────────
    public class PrintingSettings
    {
        public string DefaultPrinter { get; set; } = string.Empty;
        public string PaperSize { get; set; } = "A4";           // A4 | A3 | Letter
        public string Orientation { get; set; } = "Portrait";   // Portrait | Landscape
        public int Copies { get; set; } = 1;
        public bool ColorPrint { get; set; } = false;
        public bool PrintHeader { get; set; } = true;
        public bool PrintFooter { get; set; } = true;
        public bool PrintPageNumbers { get; set; } = true;
        public bool PrintWatermark { get; set; } = false;
        public string WatermarkText { get; set; } = "KOPIA";
        /// <summary>Margins in mm: top, right, bottom, left</summary>
        public int MarginTopMm { get; set; } = 20;
        public int MarginRightMm { get; set; } = 15;
        public int MarginBottomMm { get; set; } = 20;
        public int MarginLeftMm { get; set; } = 25;
        public string DefaultXslTemplate { get; set; } = string.Empty;
    }

    // ── 2. Protocols ──────────────────────────────────────────────────────
    public class ProtocolSettings
    {
        public string NumberFormat { get; set; } = "{PREFIX}/{YEAR}/{SEQ}";
        public string NumberPrefix { get; set; } = "SKO";
        public int InspectionValidityYears { get; set; } = 3;
        /// <summary>Reset sequential counter: "never" | "yearly" | "monthly"</summary>
        public string SequenceResetPeriod { get; set; } = "yearly";
        public int SequenceStartValue { get; set; } = 1;
        public int SequencePadding { get; set; } = 3;           // digits, e.g. 3 → 001
        public bool AutoSaveOnCreate { get; set; } = true;
        public bool RequireClientOnCreate { get; set; } = false;
        public bool RequireSprayerOnCreate { get; set; } = true;
        /// <summary>Days before expiry to show warning badge</summary>
        public int ExpiryWarningDays { get; set; } = 30;
        /// <summary>Default inspection type: "field" | "orchard"</summary>
        public string DefaultInspectionType { get; set; } = "field";
        public bool AllowEditAfterSign { get; set; } = false;
        public bool GeneratePdfOnCreate { get; set; } = false;
    }

    // ── 3. Organization ───────────────────────────────────────────────────
    public class OrganizationSettings
    {
        public string StationName { get; set; } = string.Empty;
        public string AccreditationNumber { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;         // NIP
        public string BankAccount { get; set; } = string.Empty;
        /// <summary>Base64-encoded logo image</summary>
        public string LogoBase64 { get; set; } = string.Empty;
        public string LogoMimeType { get; set; } = string.Empty;
        /// <summary>Accreditation body name shown on protocols</summary>
        public string AccreditationBody { get; set; } = string.Empty;
        public string AccreditationScope { get; set; } = string.Empty;
    }

    // ── 4. Display ────────────────────────────────────────────────────────
    public class DisplaySettings
    {
        public string Language { get; set; } = "pl";
        public string Theme { get; set; } = "light";             // light | dark | system
        public string DateFormat { get; set; } = "dd.MM.yyyy";
        public string TimeFormat { get; set; } = "HH:mm";
        public string DecimalSeparator { get; set; } = ",";
        public string ThousandsSeparator { get; set; } = " ";
        public string Timezone { get; set; } = "Europe/Warsaw";
        public string Currency { get; set; } = "PLN";
        public int ItemsPerPage { get; set; } = 25;
        public bool ShowTooltips { get; set; } = true;
        public bool CompactMode { get; set; } = false;
    }

    // ── 5. Data ───────────────────────────────────────────────────────────
    public class DataSettings
    {
        public bool AutoSave { get; set; } = true;
        public int AutoSaveIntervalSeconds { get; set; } = 30;
        public string BackupPath { get; set; } = string.Empty;
        public bool AutoBackup { get; set; } = false;
        public string BackupSchedule { get; set; } = "daily";    // daily | weekly | monthly
        public int BackupRetentionDays { get; set; } = 30;
        public string ExportFormat { get; set; } = "pdf";        // pdf | xlsx | csv
        public bool ExportIncludeAttachments { get; set; } = true;
        public int MaxAttachmentSizeMb { get; set; } = 10;
        public bool ArchiveAfterYears { get; set; } = false;
        public int ArchiveAfterYearsValue { get; set; } = 5;
    }

    // ── 6. Security ───────────────────────────────────────────────────────
    public class SecuritySettings
    {
        public int SessionTimeoutMinutes { get; set; } = 60;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
        public int PasswordMinLength { get; set; } = 6;
        public bool PasswordRequireUppercase { get; set; } = false;
        public bool PasswordRequireDigit { get; set; } = false;
        public bool PasswordRequireSpecial { get; set; } = false;
        /// <summary>0 = never force change</summary>
        public int ForcePasswordChangeDays { get; set; } = 0;
        public bool LogSecurityEvents { get; set; } = true;
        public int SecurityLogRetentionDays { get; set; } = 90;
        public bool AllowMultipleSessions { get; set; } = true;
        public bool RequireTwoFactor { get; set; } = false;
    }

    // ── 7. Notifications ──────────────────────────────────────────────────
    public class NotificationSettings
    {
        public bool EmailEnabled { get; set; } = false;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool SmtpUseSsl { get; set; } = true;
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;  // stored encrypted in future
        public string EmailFrom { get; set; } = string.Empty;
        public string EmailFromName { get; set; } = string.Empty;
        public bool NotifyOnProtocolCreate { get; set; } = false;
        public bool NotifyOnProtocolExpiry { get; set; } = true;
        public int NotifyDaysBeforeExpiry { get; set; } = 30;
        public string NotifyRecipientsJson { get; set; } = "[]"; // JSON array of emails
        public bool InAppNotifications { get; set; } = true;
    }
}
