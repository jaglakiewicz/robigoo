#region Imports

using Server.Models;

#endregion

namespace Server.Application.Clients
{
    #region Interfaces

    /// <summary>
    /// Application service for client (customer) operations.
    /// </summary>
    public interface IClientService
    {
        /// <summary>
        /// Lists clients with optional filters.
        /// </summary>
        Task<IReadOnlyList<ClientListItemDto>> GetListAsync(ClientFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a client by ID.
        /// </summary>
        Task<ClientDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new client.
        /// </summary>
        Task<(ClientDetailDto? Detail, string? ValidationError)> CreateAsync(ClientCreateUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing client.
        /// </summary>
        Task<(ClientDetailDto? Detail, string? ValidationError)> UpdateAsync(string id, ClientCreateUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a client.
        /// </summary>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }

    #endregion
}
