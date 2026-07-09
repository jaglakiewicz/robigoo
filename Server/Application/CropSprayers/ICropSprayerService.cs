#region Imports

using Server.Models;

#endregion

namespace Server.Application.CropSprayers
{
    #region Interfaces

    /// <summary>
    /// Application service for crop sprayer (machine) operations.
    /// </summary>
    public interface ICropSprayerService
    {
        /// <summary>
        /// Lists crop sprayers with optional filters.
        /// </summary>
        Task<IReadOnlyList<CropSprayerListItemDto>> GetListAsync(CropSprayerFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a crop sprayer by serial number.
        /// </summary>
        Task<CropSprayerDetailDto?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new crop sprayer.
        /// </summary>
        /// <returns>The created detail DTO, or null if validation failed.</returns>
        Task<(CropSprayerDetailDto? Detail, string? ValidationError)> CreateAsync(CropSprayerCreateUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing crop sprayer.
        /// </summary>
        /// <returns>Tuple of (detail DTO if success, validation error if any, concurrency conflict message if any).</returns>
        Task<(CropSprayerDetailDto? Detail, string? ValidationError, string? ConcurrencyConflict)> UpdateAsync(string serialNumber, CropSprayerCreateUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a crop sprayer.
        /// </summary>
        /// <returns>True if deleted, false if not found.</returns>
        Task<bool> DeleteAsync(string serialNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns distinct non-empty values for a given field (for autosuggestions).
        /// </summary>
        /// <returns>Sorted list of unique values, or null if the field is not supported.</returns>
        Task<IReadOnlyList<string>?> GetDistinctValuesAsync(string field, CancellationToken cancellationToken = default);
    }

    #endregion
}
