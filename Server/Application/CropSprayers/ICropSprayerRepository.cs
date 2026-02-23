#region Imports

using Server.Models;

#endregion

namespace Server.Application.CropSprayers
{
    #region Interfaces

    /// <summary>
    /// Repository for crop sprayer (machine) persistence operations.
    /// </summary>
    public interface ICropSprayerRepository
    {
        /// <summary>
        /// Gets crop sprayers with optional filters.
        /// </summary>
        Task<IReadOnlyList<CropSprayerListItemDto>> GetListAsync(CropSprayerFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a crop sprayer by serial number.
        /// </summary>
        Task<CropSprayer?> GetByIdAsync(string serialNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a crop sprayer exists with the given serial number.
        /// </summary>
        Task<bool> ExistsAsync(string serialNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new crop sprayer.
        /// </summary>
        Task AddAsync(CropSprayer entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing crop sprayer.
        /// </summary>
        Task UpdateAsync(CropSprayer entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a crop sprayer.
        /// </summary>
        Task RemoveAsync(CropSprayer entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the display name of a client by ID.
        /// </summary>
        Task<string?> GetClientDisplayNameAsync(string? clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    #endregion
}
