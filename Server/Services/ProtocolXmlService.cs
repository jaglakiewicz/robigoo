using System.Text;
using System.Xml;
using System.Xml.Linq;
using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// Service for generating XML from InspectionProtocol and transforming to PDF
    /// </summary>
    public interface IProtocolXmlService
    {
        /// <summary>
        /// Generate XML representation of the inspection protocol
        /// </summary>
        string GenerateXml(InspectionProtocol protocol);

        /// <summary>
        /// Get the current XSL template version
        /// </summary>
        string GetCurrentXslVersion();

        /// <summary>
        /// Get the XSL content for a specific version
        /// </summary>
        string GetXslTemplate(string version);

        /// <summary>
        /// Get available XSL template versions
        /// </summary>
        IEnumerable<string> GetAvailableXslVersions();
    }

    /// <summary>
    /// Implementation of protocol XML service
    /// </summary>
    public class ProtocolXmlService : IProtocolXmlService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProtocolXmlService> _logger;

        // Current XSL version - increment when template changes
        private const string CurrentXslVersion = "1.0";

        public ProtocolXmlService(IWebHostEnvironment environment, ILogger<ProtocolXmlService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Generate XML representation of the inspection protocol
        /// </summary>
        public string GenerateXml(InspectionProtocol protocol)
        {
            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("InspectionProtocol",
                    new XAttribute("version", CurrentXslVersion),
                    new XAttribute("generatedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),

                    // Basic Info
                    new XElement("Id", protocol.Id),
                    new XElement("ProtocolNumber", protocol.ProtocolNumber ?? ""),
                    new XElement("InspectionDate", protocol.InspectionDate.ToString("yyyy-MM-dd")),
                    new XElement("InspectionLocation", protocol.InspectionLocation ?? ""),
                    new XElement("GeneratedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),

                    // Inspector Data
                    new XElement("InspectorName", protocol.InspectorName ?? ""),
                    new XElement("InspectorLicenseNumber", protocol.InspectorLicenseNumber ?? ""),

                    // Client Data
                    new XElement("ClientId", protocol.ClientId ?? ""),
                    new XElement("ClientName", protocol.ClientName ?? ""),
                    new XElement("ClientAddress", protocol.ClientAddress ?? ""),
                    new XElement("ClientTaxId", protocol.ClientTaxId ?? ""),

                    // Sprayer Data
                    new XElement("CropSprayerSerialNumber", protocol.CropSprayerSerialNumber ?? ""),
                    new XElement("CropSprayerName", protocol.CropSprayerName ?? ""),
                    new XElement("CropSprayerType", protocol.CropSprayerType ?? ""),
                    new XElement("CropSprayerKind", protocol.CropSprayerKind ?? ""),
                    new XElement("CropSprayerManufacturer", protocol.CropSprayerManufacturer ?? ""),
                    new XElement("CropSprayerProductionYear", protocol.CropSprayerProductionYear ?? ""),
                    new XElement("TankCapacity", protocol.TankCapacity?.ToString() ?? ""),
                    new XElement("BoomWidth", protocol.BoomWidth?.ToString() ?? ""),
                    new XElement("SectionCount", protocol.SectionCount?.ToString() ?? ""),

                    // Section 1: General
                    new XElement("GeneralConditionPassed", BoolToString(protocol.GeneralConditionPassed)),
                    new XElement("MarkingsReadablePassed", BoolToString(protocol.MarkingsReadablePassed)),
                    new XElement("EquipmentCompletePassed", BoolToString(protocol.EquipmentCompletePassed)),
                    new XElement("GeneralSectionNotes", protocol.GeneralSectionNotes ?? ""),

                    // Section 2: Pump
                    new XElement("PumpOperationPassed", BoolToString(protocol.PumpOperationPassed)),
                    new XElement("PumpSealingPassed", BoolToString(protocol.PumpSealingPassed)),
                    new XElement("PressurePulsationPassed", BoolToString(protocol.PressurePulsationPassed)),
                    new XElement("PumpSectionNotes", protocol.PumpSectionNotes ?? ""),

                    // Section 3: Agitation
                    new XElement("AgitatorOperationPassed", BoolToString(protocol.AgitatorOperationPassed)),
                    new XElement("AgitatorSectionNotes", protocol.AgitatorSectionNotes ?? ""),

                    // Section 4: Tank
                    new XElement("TankConditionPassed", BoolToString(protocol.TankConditionPassed)),
                    new XElement("TankSealingPassed", BoolToString(protocol.TankSealingPassed)),
                    new XElement("LevelIndicatorPassed", BoolToString(protocol.LevelIndicatorPassed)),
                    new XElement("FlushingSystemPassed", BoolToString(protocol.FlushingSystemPassed)),
                    new XElement("TankSectionNotes", protocol.TankSectionNotes ?? ""),

                    // Section 5: Measuring
                    new XElement("ManometerPassed", BoolToString(protocol.ManometerPassed)),
                    new XElement("ManometerReading2Bar", protocol.ManometerReading2Bar?.ToString("F1") ?? ""),
                    new XElement("ManometerReading4Bar", protocol.ManometerReading4Bar?.ToString("F1") ?? ""),
                    new XElement("ManometerReading6Bar", protocol.ManometerReading6Bar?.ToString("F1") ?? ""),
                    new XElement("ManometerDialSizePassed", BoolToString(protocol.ManometerDialSizePassed)),
                    new XElement("MeasuringSectionNotes", protocol.MeasuringSectionNotes ?? ""),

                    // Section 6: Piping
                    new XElement("PipesConditionPassed", BoolToString(protocol.PipesConditionPassed)),
                    new XElement("ConnectionsSealingPassed", BoolToString(protocol.ConnectionsSealingPassed)),
                    new XElement("PipingSectionNotes", protocol.PipingSectionNotes ?? ""),

                    // Section 7: Filtration
                    new XElement("SuctionFilterPassed", BoolToString(protocol.SuctionFilterPassed)),
                    new XElement("PressureFilterPassed", BoolToString(protocol.PressureFilterPassed)),
                    new XElement("NozzleFiltersPassed", BoolToString(protocol.NozzleFiltersPassed)),
                    new XElement("FiltrationSectionNotes", protocol.FiltrationSectionNotes ?? ""),

                    // Section 8: Boom
                    new XElement("FieldBoomConditionPassed", BoolToString(protocol.FieldBoomConditionPassed)),
                    new XElement("BoomStabilityPassed", BoolToString(protocol.BoomStabilityPassed)),
                    new XElement("BoomHeightPassed", BoolToString(protocol.BoomHeightPassed)),
                    new XElement("BoomSymmetryPassed", BoolToString(protocol.BoomSymmetryPassed)),
                    new XElement("OrchardSprayerConditionPassed", BoolToString(protocol.OrchardSprayerConditionPassed)),
                    new XElement("AirStreamDirectionPassed", BoolToString(protocol.AirStreamDirectionPassed)),
                    new XElement("BoomSectionNotes", protocol.BoomSectionNotes ?? ""),

                    // Section 9: Nozzles
                    new XElement("NozzleUniformityPassed", BoolToString(protocol.NozzleUniformityPassed)),
                    new XElement("NozzleFlowRatePassed", BoolToString(protocol.NozzleFlowRatePassed)),
                    new XElement("NozzleConditionPassed", BoolToString(protocol.NozzleConditionPassed)),
                    new XElement("NozzleMeasurements", protocol.NozzleMeasurements ?? ""),
                    new XElement("NozzlesSectionNotes", protocol.NozzlesSectionNotes ?? ""),

                    // Section 10: Distribution
                    new XElement("TransverseDistributionPassed", BoolToString(protocol.TransverseDistributionPassed)),
                    new XElement("CoefficientOfVariation", protocol.CoefficientOfVariation?.ToString("F1") ?? ""),
                    new XElement("DistributionSectionNotes", protocol.DistributionSectionNotes ?? ""),

                    // Final Result
                    new XElement("FinalResult", BoolToString(protocol.FinalResult)),
                    new XElement("ValidUntil", protocol.ValidUntil?.ToString("yyyy-MM-dd") ?? ""),
                    new XElement("ControlStickerNumber", protocol.ControlStickerNumber ?? ""),
                    new XElement("GeneralNotes", protocol.GeneralNotes ?? ""),

                    // Timestamps
                    new XElement("CreatedAt", protocol.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    new XElement("UpdatedAt", protocol.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")
                )
            );

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            xml.Save(xmlWriter);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Get the current XSL template version
        /// </summary>
        public string GetCurrentXslVersion()
        {
            return CurrentXslVersion;
        }

        /// <summary>
        /// Get the XSL content for a specific version
        /// </summary>
        public string GetXslTemplate(string version)
        {
            // XSL templates are stored in Misc/Resources/Stylesheets (relative to workspace root)
            var workspaceRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, ".."));
            var templatePath = Path.Combine(workspaceRoot, "Misc", "Resources", "Stylesheets", $"inspection-protocol-v{version}.xsl");
            
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("XSL template version {Version} not found at {Path}", version, templatePath);
                // Fall back to current version
                templatePath = Path.Combine(workspaceRoot, "Misc", "Resources", "Stylesheets", $"inspection-protocol-v{CurrentXslVersion}.xsl");
            }

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"XSL template not found: {templatePath}");
            }

            return File.ReadAllText(templatePath, Encoding.UTF8);
        }

        /// <summary>
        /// Get available XSL template versions
        /// </summary>
        public IEnumerable<string> GetAvailableXslVersions()
        {
            // XSL templates are stored in Misc/Resources/Stylesheets (relative to workspace root)
            var workspaceRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, ".."));
            var stylesheetsPath = Path.Combine(workspaceRoot, "Misc", "Resources", "Stylesheets");
            
            if (!Directory.Exists(stylesheetsPath))
            {
                return new[] { CurrentXslVersion };
            }

            var versions = Directory.GetFiles(stylesheetsPath, "inspection-protocol-v*.xsl")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(f => f.Replace("inspection-protocol-v", ""))
                .OrderByDescending(v => v)
                .ToList();

            if (!versions.Any())
            {
                versions.Add(CurrentXslVersion);
            }

            return versions;
        }

        private static string BoolToString(bool? value)
        {
            return value switch
            {
                true => "true",
                false => "false",
                null => ""
            };
        }
    }
}
