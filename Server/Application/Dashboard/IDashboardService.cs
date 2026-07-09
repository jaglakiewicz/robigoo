using Server.Models;

namespace Server.Application.Dashboard
{
    public interface IDashboardService
    {
        Task<IReadOnlyList<RecentInspectionDto>> GetRecentInspectionsAsync(int count = 5, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RecentCropSprayerDto>> GetRecentCropSprayersAsync(int count = 5, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UpcomingInspectionDto>> GetUpcomingInspectionsAsync(int limit = 10, CancellationToken cancellationToken = default);
        Task<DashboardStatisticsDto> GetStatisticsAsync(int year, CancellationToken cancellationToken = default);
    }
}
