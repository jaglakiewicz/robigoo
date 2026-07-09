namespace Server.Models
{
    public class RecentInspectionDto
    {
        public long Id { get; set; }
        public string ProtocolNumber { get; set; } = string.Empty;
        public DateTime InspectionDate { get; set; }
        public string InspectorName { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? CropSprayerName { get; set; }
        public string? CropSprayerSerialNumber { get; set; }
        public string? CropSprayerType { get; set; }
        public bool? FinalResult { get; set; }
        public DateTime? ValidUntil { get; set; }
    }

    public class RecentCropSprayerDto
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string SprayerName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ProductionYear { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string? OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpcomingInspectionDto
    {
        public string CropSprayerSerialNumber { get; set; } = string.Empty;
        public string CropSprayerName { get; set; } = string.Empty;
        public string? CropSprayerType { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerId { get; set; }
        public string? OwnerCity { get; set; }
        public string? OwnerStreet { get; set; }
        public string? OwnerBuildingNumber { get; set; }
        public string? OwnerZipCode { get; set; }
        public string? OwnerVoivodeship { get; set; }
        public DateTime? LastInspectionDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int RemainingDays { get; set; }
    }

    public class MonthlyStatDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DashboardStatisticsDto
    {
        public int Year { get; set; }
        public int TotalThisYear { get; set; }
        public List<MonthlyStatDto> Months { get; set; } = new();
    }
}
