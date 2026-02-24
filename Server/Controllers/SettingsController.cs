using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SettingsController> _logger;
        private readonly AppDbContext _db;

        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private string XslDir => Path.Combine(_env.ContentRootPath, "..", "Misc", "Resources", "Stylesheets");

        public SettingsController(IWebHostEnvironment env, ILogger<SettingsController> logger, AppDbContext db)
        {
            _env = env;
            _logger = logger;
            _db = db;
        }

        // ── App Settings ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var row = await _db.AppSettings.FindAsync(1);
            var data = row != null
                ? JsonSerializer.Deserialize<AppSettingsData>(row.DataJson, _json) ?? new AppSettingsData()
                : new AppSettingsData();
            return Ok(data);
        }

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveSettings([FromBody] AppSettingsData dto)
        {
            var login = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
            var row = await _db.AppSettings.FindAsync(1);
            if (row == null)
            {
                row = new AppSettings { Id = 1 };
                _db.AppSettings.Add(row);
            }
            row.DataJson = JsonSerializer.Serialize(dto, _json);
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = login;
            await _db.SaveChangesAsync();
            return Ok(dto);
        }

        // ── Printers ───────────────────────────────────────────────────

        [HttpGet("printers")]
        public IActionResult GetPrinters() =>
            Ok(new { printers = new List<string>(), defaultPrinter = "" });

        // ── XSL Templates ──────────────────────────────────────────────

        [HttpGet("xsl-templates")]
        public IActionResult GetXslTemplates()
        {
            Directory.CreateDirectory(XslDir);
            var files = Directory.GetFiles(XslDir, "*.xsl")
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    uploadedAt = System.IO.File.GetLastWriteTimeUtc(f).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    size = new FileInfo(f).Length
                })
                .OrderByDescending(f => f.uploadedAt)
                .ToList();

            var defaultFile = GetDefaultXslName();
            return Ok(new { templates = files, defaultTemplate = defaultFile });
        }

        [HttpPost("xsl-templates/upload")]
        public async Task<IActionResult> UploadXslTemplate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Brak pliku" });

            if (!file.FileName.EndsWith(".xsl", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Dozwolone są tylko pliki .xsl" });

            if (file.Length > 1_048_576) // 1 MB
                return BadRequest(new { message = "Plik jest za duży (max 1 MB)" });

            Directory.CreateDirectory(XslDir);
            var safeName = Path.GetFileName(file.FileName);
            var dest = Path.Combine(XslDir, safeName);

            await using var stream = System.IO.File.Create(dest);
            await file.CopyToAsync(stream);

            _logger.LogInformation("XSL template uploaded: {Name}", safeName);
            return Ok(new { name = safeName });
        }

        [HttpDelete("xsl-templates/{name}")]
        public IActionResult DeleteXslTemplate(string name)
        {
            var safeName = Path.GetFileName(name);
            var path = Path.Combine(XslDir, safeName);
            if (!System.IO.File.Exists(path))
                return NotFound();

            System.IO.File.Delete(path);

            // If deleted file was default, clear default
            if (GetDefaultXslName() == safeName)
                SaveDefaultXslName("");

            return NoContent();
        }

        [HttpPut("xsl-templates/default")]
        public IActionResult SetDefaultXslTemplate([FromBody] SetDefaultXslRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Nazwa jest wymagana" });

            var safeName = Path.GetFileName(req.Name);
            var path = Path.Combine(XslDir, safeName);
            if (!System.IO.File.Exists(path))
                return NotFound(new { message = "Plik nie istnieje" });

            SaveDefaultXslName(safeName);
            return Ok(new { defaultTemplate = safeName });
        }

        // ── Helpers ────────────────────────────────────────────────────

        private string DefaultXslConfigPath => Path.Combine(XslDir, ".default");

        private string GetDefaultXslName()
        {
            if (!System.IO.File.Exists(DefaultXslConfigPath)) return "";
            return System.IO.File.ReadAllText(DefaultXslConfigPath).Trim();
        }

        private void SaveDefaultXslName(string name)
        {
            Directory.CreateDirectory(XslDir);
            System.IO.File.WriteAllText(DefaultXslConfigPath, name);
        }
    }

    public record SetDefaultXslRequest(string Name);
}
