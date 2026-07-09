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

        // ── Helpers ────────────────────────────────────────────────────
    }
}
