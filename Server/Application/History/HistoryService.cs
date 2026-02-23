#region Imports

using Microsoft.EntityFrameworkCore;
using Server.Data;
using System;

#endregion

namespace Server.Application.History
{
    /// <summary>
    /// Application service for entity change history.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        #region Declarations

        private readonly AppDbContext _context;

        #endregion

        #region Constructor

        public HistoryService(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEntryDto>> GetHistoryAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
        {
            var logs = await _context.ChangeLogs
                .Where(l => l.EntityName == entityName && l.EntityId == entityId)
                .GroupJoin(
                    _context.Users,
                    log => log.Who,
                    user => user.Login,
                    (log, users) => new { log, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(),
                    (x, user) => new HistoryEntryDto
                    {
                        Id = x.log.Id,
                        EntityName = x.log.EntityName,
                        EntityId = x.log.EntityId,
                        Changes = x.log.Changes,
                        Who = x.log.Who,
                        When = x.log.When,
                        UserFirstName = user != null ? user.FirstName : null,
                        UserLastName = user != null ? user.LastName : null,
                        UserAvatarData = user != null && user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
                    }
                )
                .OrderByDescending(l => l.When)
                .ToListAsync(cancellationToken);

            return logs;
        }

        #endregion
    }
}
