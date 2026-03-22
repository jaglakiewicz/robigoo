using Server.Data;
using Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Server.Services
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(long userId, ActivityType activityType, string entityType, 
            string? entityId, string description, string? ipAddress, string? userAgent, 
            string? metadata = null);
        
        Task<List<UserActivityLog>> GetUserActivitiesAsync(long userId, int limit = 100);
        
        Task<List<UserActivityLog>> GetAllActivitiesAsync(DateTime? from = null, 
            DateTime? to = null, int limit = 1000);
    }

    public class UserActivityService : IUserActivityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserActivityService> _logger;

        public UserActivityService(AppDbContext context, ILogger<UserActivityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActivityAsync(long userId, ActivityType activityType, 
            string entityType, string? entityId, string description, string? ipAddress, 
            string? userAgent, string? metadata = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot log activity for non-existent user {UserId}", userId);
                    return;
                }

                var activity = new UserActivityLog
                {
                    UserId = userId,
                    UserLogin = user.Login,
                    ActivityType = activityType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    IpAddress = ipAddress?.Length > 50 ? ipAddress.Substring(0, 50) : ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                    Metadata = metadata?.Length > 2000 ? metadata.Substring(0, 2000) : metadata,
                    Timestamp = DateTime.UtcNow
                };

                _context.UserActivityLogs.Add(activity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user activity for user {UserId}", userId);
            }
        }

        public async Task<List<UserActivityLog>> GetUserActivitiesAsync(long userId, int limit = 100)
        {
            return await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<UserActivityLog>> GetAllActivitiesAsync(DateTime? from = null, 
            DateTime? to = null, int limit = 1000)
        {
            var query = _context.UserActivityLogs.AsQueryable();

            if (from.HasValue)
                query = query.Where(a => a.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.Timestamp <= to.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}
