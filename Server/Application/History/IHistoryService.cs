#region Imports

#endregion

namespace Server.Application.History
{
    #region Interfaces

    /// <summary>
    /// Application service for entity change history.
    /// </summary>
    public interface IHistoryService
    {
        /// <summary>
        /// Gets the change history for an entity.
        /// </summary>
        Task<IReadOnlyList<HistoryEntryDto>> GetHistoryAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
    }

    #endregion
}
