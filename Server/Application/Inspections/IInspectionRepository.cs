#region Imports

using Server.Models;

#endregion

namespace Server.Application.Inspections
{
    #region Interfaces

    /// <summary>
    /// Repository for inspection persistence operations.
    /// </summary>
    public interface IInspectionRepository
    {
        /// <summary>
        /// Gets all inspections with items.
        /// </summary>
        Task<IReadOnlyList<Inspection>> GetListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an inspection by ID.
        /// </summary>
        Task<Inspection?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new inspection with items.
        /// </summary>
        Task<Inspection> AddAsync(Inspection entity, CancellationToken cancellationToken = default);
    }

    #endregion
}
