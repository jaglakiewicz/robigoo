/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace Server.Models
{
    #region InspectionProtocol Model

    /// <summary>
    /// Protokół badania technicznego opryskiwaczy polowych i sadowniczych
    /// zgodnie z wymaganiami SKO (Stacje Kontroli Opryskiwaczy)
    /// </summary>
    public class InspectionProtocol
    {
        #region Basic Properties

        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Numer protokołu (np. "SKO/2026/001")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ProtocolNumber { get; set; } = string.Empty;

        /// <summary>
        /// Data badania
        /// </summary>
        [Required]
        public DateTime InspectionDate { get; set; }

        /// <summary>
        /// Miejsce badania
        /// </summary>
        [MaxLength(500)]
        public string? InspectionLocation { get; set; }

        #endregion

        #region Inspector Data

        /// <summary>
        /// Imię i nazwisko diagnosty
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string InspectorName { get; set; } = string.Empty;

        /// <summary>
        /// Numer uprawnień diagnosty
        /// </summary>
        [MaxLength(100)]
        public string? InspectorLicenseNumber { get; set; }

        #endregion

        #region Client Reference

        /// <summary>
        /// ID klienta (właściciela opryskiwacza)
        /// </summary>
        [MaxLength(100)]
        public string? ClientId { get; set; }

        /// <summary>
        /// Nazwa klienta (denormalizowana dla raportów)
        /// </summary>
        [MaxLength(500)]
        public string? ClientName { get; set; }

        /// <summary>
        /// Adres klienta (denormalizowana dla raportów)
        /// </summary>
        [MaxLength(1000)]
        public string? ClientAddress { get; set; }

        /// <summary>
        /// NIP/PESEL klienta
        /// </summary>
        [MaxLength(20)]
        public string? ClientTaxId { get; set; }

        #endregion

        #region CropSprayer Reference

        /// <summary>
        /// Numer seryjny opryskiwacza
        /// </summary>
        [MaxLength(100)]
        public string? CropSprayerSerialNumber { get; set; }

        /// <summary>
        /// Nazwa opryskiwacza (denormalizowana)
        /// </summary>
        [MaxLength(255)]
        public string? CropSprayerName { get; set; }

        /// <summary>
        /// Typ: "00" = polowy, "01" = sadowniczy
        /// </summary>
        [MaxLength(2)]
        public string? CropSprayerType { get; set; }

        /// <summary>
        /// Rodzaj: "00" = zawieszany, "01" = przyczepiany, "02" = samobieżny, "03" = inny
        /// </summary>
        [MaxLength(2)]
        public string? CropSprayerKind { get; set; }

        /// <summary>
        /// Producent opryskiwacza
        /// </summary>
        [MaxLength(255)]
        public string? CropSprayerManufacturer { get; set; }

        /// <summary>
        /// Rok produkcji
        /// </summary>
        [MaxLength(4)]
        public string? CropSprayerProductionYear { get; set; }

        /// <summary>
        /// Pojemność zbiornika [l]
        /// </summary>
        public decimal? TankCapacity { get; set; }

        /// <summary>
        /// Szerokość robocza belki [m]
        /// </summary>
        public decimal? BoomWidth { get; set; }

        /// <summary>
        /// Liczba sekcji
        /// </summary>
        public int? SectionCount { get; set; }

        #endregion

        #region Inspection Sections - General (Sekcja 1: Stan ogólny)

        /// <summary>
        /// 1.1 Stan techniczny ogólny - wynik
        /// </summary>
        public bool? GeneralConditionPassed { get; set; }

        /// <summary>
        /// 1.2 Czytelność oznaczeń - wynik
        /// </summary>
        public bool? MarkingsReadablePassed { get; set; }

        /// <summary>
        /// 1.3 Kompletność wyposażenia - wynik
        /// </summary>
        public bool? EquipmentCompletePassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 1
        /// </summary>
        [MaxLength(2000)]
        public string? GeneralSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Pump (Sekcja 2: Pompa)

        /// <summary>
        /// 2.1 Sprawność pompy - wynik
        /// </summary>
        public bool? PumpOperationPassed { get; set; }

        /// <summary>
        /// 2.2 Szczelność pompy - wynik
        /// </summary>
        public bool? PumpSealingPassed { get; set; }

        /// <summary>
        /// 2.3 Pulsacja ciśnienia - wynik
        /// </summary>
        public bool? PressurePulsationPassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 2
        /// </summary>
        [MaxLength(2000)]
        public string? PumpSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Agitation (Sekcja 3: Mieszalnik)

        /// <summary>
        /// 3.1 Sprawność mieszalnika - wynik
        /// </summary>
        public bool? AgitatorOperationPassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 3
        /// </summary>
        [MaxLength(2000)]
        public string? AgitatorSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Tank (Sekcja 4: Zbiornik)

        /// <summary>
        /// 4.1 Stan zbiornika - wynik
        /// </summary>
        public bool? TankConditionPassed { get; set; }

        /// <summary>
        /// 4.2 Szczelność zbiornika - wynik
        /// </summary>
        public bool? TankSealingPassed { get; set; }

        /// <summary>
        /// 4.3 Wskaźnik poziomu cieczy - wynik
        /// </summary>
        public bool? LevelIndicatorPassed { get; set; }

        /// <summary>
        /// 4.4 System płukania - wynik
        /// </summary>
        public bool? FlushingSystemPassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 4
        /// </summary>
        [MaxLength(2000)]
        public string? TankSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Measuring (Sekcja 5: Przyrządy pomiarowe)

        /// <summary>
        /// 5.1 Manometr - wynik
        /// </summary>
        public bool? ManometerPassed { get; set; }

        /// <summary>
        /// 5.1a Odczyt przy ciśnieniu 2 bar [bar]
        /// </summary>
        public decimal? ManometerReading2Bar { get; set; }

        /// <summary>
        /// 5.1b Odczyt przy ciśnieniu 4 bar [bar]
        /// </summary>
        public decimal? ManometerReading4Bar { get; set; }

        /// <summary>
        /// 5.1c Odczyt przy ciśnieniu 6 bar [bar]
        /// </summary>
        public decimal? ManometerReading6Bar { get; set; }

        /// <summary>
        /// 5.2 Średnica tarczy manometru spełnia wymagania - wynik
        /// </summary>
        public bool? ManometerDialSizePassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 5
        /// </summary>
        [MaxLength(2000)]
        public string? MeasuringSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Piping (Sekcja 6: Przewody)

        /// <summary>
        /// 6.1 Stan przewodów - wynik
        /// </summary>
        public bool? PipesConditionPassed { get; set; }

        /// <summary>
        /// 6.2 Szczelność połączeń - wynik
        /// </summary>
        public bool? ConnectionsSealingPassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 6
        /// </summary>
        [MaxLength(2000)]
        public string? PipingSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Filtration (Sekcja 7: Filtracja)

        /// <summary>
        /// 7.1 Filtr ssawny - wynik
        /// </summary>
        public bool? SuctionFilterPassed { get; set; }

        /// <summary>
        /// 7.2 Filtr tłoczny - wynik
        /// </summary>
        public bool? PressureFilterPassed { get; set; }

        /// <summary>
        /// 7.3 Filtry rozpylaczy - wynik
        /// </summary>
        public bool? NozzleFiltersPassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 7
        /// </summary>
        [MaxLength(2000)]
        public string? FiltrationSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Boom/Spray Equipment (Sekcja 8: Belka/Urządzenie rozpylające)

        /// <summary>
        /// 8.1 Stan belki polowej - wynik (dla opryskiwaczy polowych)
        /// </summary>
        public bool? FieldBoomConditionPassed { get; set; }

        /// <summary>
        /// 8.2 Stabilność belki - wynik
        /// </summary>
        public bool? BoomStabilityPassed { get; set; }

        /// <summary>
        /// 8.3 Wysokość belki - wynik
        /// </summary>
        public bool? BoomHeightPassed { get; set; }

        /// <summary>
        /// 8.4 Symetria belki - wynik
        /// </summary>
        public bool? BoomSymmetryPassed { get; set; }

        /// <summary>
        /// 8.5 Stan urządzenia rozpylającego sadowniczego - wynik (dla opryskiwaczy sadowniczych)
        /// </summary>
        public bool? OrchardSprayerConditionPassed { get; set; }

        /// <summary>
        /// 8.6 Kierunek strumienia powietrza - wynik (dla opryskiwaczy sadowniczych)
        /// </summary>
        public bool? AirStreamDirectionPassed { get; set; }

        /// <summary>
        /// Uwagi do sekcji 8
        /// </summary>
        [MaxLength(2000)]
        public string? BoomSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Nozzles (Sekcja 9: Rozpylacze)

        /// <summary>
        /// 9.1 Jednorodność rozpylaczy - wynik
        /// </summary>
        public bool? NozzleUniformityPassed { get; set; }

        /// <summary>
        /// 9.2 Wydatek rozpylaczy w normie - wynik
        /// </summary>
        public bool? NozzleFlowRatePassed { get; set; }

        /// <summary>
        /// 9.3 Stan rozpylaczy - wynik
        /// </summary>
        public bool? NozzleConditionPassed { get; set; }

        /// <summary>
        /// Dane pomiarowe rozpylaczy (JSON lub tekst)
        /// </summary>
        [MaxLength(4000)]
        public string? NozzleMeasurements { get; set; }

        /// <summary>
        /// Uwagi do sekcji 9
        /// </summary>
        [MaxLength(2000)]
        public string? NozzlesSectionNotes { get; set; }

        #endregion

        #region Inspection Sections - Distribution (Sekcja 10: Równomierność rozprowadzania)

        /// <summary>
        /// 10.1 Poprzeczna równomierność rozprowadzania - wynik
        /// </summary>
        public bool? TransverseDistributionPassed { get; set; }

        /// <summary>
        /// 10.1a Współczynnik zmienności CV [%]
        /// </summary>
        public decimal? CoefficientOfVariation { get; set; }

        /// <summary>
        /// Uwagi do sekcji 10
        /// </summary>
        [MaxLength(2000)]
        public string? DistributionSectionNotes { get; set; }

        #endregion

        #region Final Result

        /// <summary>
        /// Wynik końcowy badania: true = pozytywny, false = negatywny
        /// </summary>
        public bool? FinalResult { get; set; }

        /// <summary>
        /// Data ważności badania (zazwyczaj 3 lata od daty badania)
        /// </summary>
        public DateTime? ValidUntil { get; set; }

        /// <summary>
        /// Numer nalepki kontrolnej (jeśli wynik pozytywny)
        /// </summary>
        [MaxLength(50)]
        public string? ControlStickerNumber { get; set; }

        /// <summary>
        /// Uwagi ogólne / zalecenia
        /// </summary>
        [MaxLength(4000)]
        public string? GeneralNotes { get; set; }

        #endregion

        #region Timestamps

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        #endregion

        #region XML/PDF Storage

        /// <summary>
        /// Pełne dane protokołu w formacie XML (do generowania PDF)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? ProtocolXml { get; set; }

        /// <summary>
        /// Wersja szablonu XSL użyta do wygenerowania PDF
        /// Pozwala na odtworzenie PDF nawet po aktualizacji szablonu
        /// </summary>
        [MaxLength(20)]
        public string? XslTemplateVersion { get; set; }

        /// <summary>
        /// Data wygenerowania XML
        /// </summary>
        public DateTime? XmlGeneratedAt { get; set; }

        #endregion
    }

    #endregion
}
