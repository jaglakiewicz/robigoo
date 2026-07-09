using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Application.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        private static readonly string[] PolishMonthNames =
        {
            "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
            "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
        };

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RecentInspectionDto>> GetRecentInspectionsAsync(int count = 5, CancellationToken cancellationToken = default)
        {
            count = Math.Clamp(count, 1, 20);

            return await _context.Set<InspectionProtocol>()
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new RecentInspectionDto
                {
                    Id = p.Id,
                    ProtocolNumber = p.ProtocolNumber,
                    InspectionDate = p.InspectionDate,
                    InspectorName = p.InspectorName,
                    ClientName = p.ClientName,
                    CropSprayerName = p.CropSprayerName,
                    CropSprayerSerialNumber = p.CropSprayerSerialNumber,
                    CropSprayerType = p.CropSprayerType,
                    FinalResult = p.FinalResult,
                    ValidUntil = p.ValidUntil
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RecentCropSprayerDto>> GetRecentCropSprayersAsync(int count = 5, CancellationToken cancellationToken = default)
        {
            count = Math.Clamp(count, 1, 20);

            return await (from cs in _context.Set<CropSprayer>().AsNoTracking()
                          join c in _context.Set<Client>().AsNoTracking()
                              on cs.OwnerId equals c.Id into clients
                          from client in clients.DefaultIfEmpty()
                          orderby cs.CreatedAt descending
                          select new RecentCropSprayerDto
                          {
                              SerialNumber = cs.SerialNumber,
                              SprayerName = cs.SprayerName,
                              Manufacturer = cs.Manufacturer,
                              ProductionYear = cs.ProductionYear,
                              Type = cs.Type,
                              Kind = cs.Kind,
                              OwnerName = client != null ? client.DisplayName : cs.OwnerName,
                              OwnerId = cs.OwnerId,
                              CreatedAt = cs.CreatedAt
                          })
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UpcomingInspectionDto>> GetUpcomingInspectionsAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 100);
            var today = DateTime.UtcNow.Date;
            var threeYearsFromNow = today.AddYears(3);

            // Get all protocols that have a serial number and valid-until date,
            // then compute the latest per sprayer in memory to avoid complex GroupBy translation issues with SQLite.
            var protocols = await _context.Set<InspectionProtocol>()
                .AsNoTracking()
                .Where(p => p.CropSprayerSerialNumber != null && p.ValidUntil != null)
                .Select(p => new
                {
                    p.CropSprayerSerialNumber,
                    p.CropSprayerName,
                    p.CropSprayerType,
                    p.ClientName,
                    p.ClientId,
                    p.InspectionDate,
                    p.ValidUntil,
                    p.FinalResult
                })
                .ToListAsync(cancellationToken);

            var latestProtocols = protocols
                .GroupBy(p => p.CropSprayerSerialNumber!)
                .Select(g => g.OrderByDescending(p => p.InspectionDate).First())
                .Where(p => p.ValidUntil <= threeYearsFromNow)
                .OrderBy(p => p.ValidUntil)
                .Take(limit)
                .ToList();

            if (!latestProtocols.Any())
                return Array.Empty<UpcomingInspectionDto>();

            // Get crop sprayer details with owner IDs
            var serialNumbers = latestProtocols.Select(p => p.CropSprayerSerialNumber).ToList();
            var sprayers = await _context.Set<CropSprayer>()
                .AsNoTracking()
                .Where(cs => serialNumbers.Contains(cs.SerialNumber))
                .ToDictionaryAsync(cs => cs.SerialNumber, cancellationToken);

            // Get client details for addresses
            var ownerIds = sprayers.Values
                .Where(cs => cs.OwnerId != null)
                .Select(cs => cs.OwnerId!)
                .Distinct()
                .ToList();

            var clients = ownerIds.Any()
                ? await _context.Set<Client>()
                    .AsNoTracking()
                    .Where(c => ownerIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, cancellationToken)
                : new Dictionary<string, Client>();

            return latestProtocols.Select(protocol =>
            {
                sprayers.TryGetValue(protocol.CropSprayerSerialNumber!, out var sprayer);
                Client? client = null;
                if (sprayer?.OwnerId != null)
                    clients.TryGetValue(sprayer.OwnerId, out client);

                return new UpcomingInspectionDto
                {
                    CropSprayerSerialNumber = protocol.CropSprayerSerialNumber!,
                    CropSprayerName = protocol.CropSprayerName ?? sprayer?.SprayerName ?? "",
                    CropSprayerType = protocol.CropSprayerType ?? sprayer?.Type,
                    OwnerName = sprayer?.OwnerName ?? protocol.ClientName,
                    OwnerId = sprayer?.OwnerId ?? protocol.ClientId,
                    OwnerCity = client?.City,
                    OwnerStreet = client?.Street,
                    OwnerBuildingNumber = client?.BuildingNumber,
                    OwnerZipCode = client?.ZipCode,
                    OwnerVoivodeship = client?.Voivodeship,
                    LastInspectionDate = protocol.InspectionDate,
                    ValidUntil = protocol.ValidUntil,
                    RemainingDays = protocol.ValidUntil.HasValue
                        ? (int)(protocol.ValidUntil.Value.Date - today).TotalDays
                        : 0
                };
            }).ToList();
        }

        public async Task<DashboardStatisticsDto> GetStatisticsAsync(int year, CancellationToken cancellationToken = default)
        {
            var monthlyCounts = await _context.Set<InspectionProtocol>()
                .AsNoTracking()
                .Where(p => p.InspectionDate.Year == year)
                .GroupBy(p => p.InspectionDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var monthlyLookup = monthlyCounts.ToDictionary(m => m.Month, m => m.Count);

            var months = Enumerable.Range(1, 12).Select(m => new MonthlyStatDto
            {
                Month = m,
                MonthName = PolishMonthNames[m],
                Count = monthlyLookup.GetValueOrDefault(m, 0)
            }).ToList();

            return new DashboardStatisticsDto
            {
                Year = year,
                TotalThisYear = months.Sum(m => m.Count),
                Months = months
            };
        }
    }
}
