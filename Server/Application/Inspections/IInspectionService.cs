#region Imports

using Server.Models;

#endregion

namespace Server.Application.Inspections
{
    #region Interfaces

    /// <summary>
    /// Application service for inspection operations.
    /// </summary>
    public interface IInspectionService
    {
        /// <summary>
        /// Lists all inspections.
        /// </summary>
        Task<IReadOnlyList<InspectionResponseDto>> GetListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an inspection by ID.
        /// </summary>
        Task<InspectionResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new inspection.
        /// </summary>
        Task<InspectionResponseDto> CreateAsync(InspectionCreateDto dto, CancellationToken cancellationToken = default);
    }

    #endregion
}
