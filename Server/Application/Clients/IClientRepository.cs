#region Imports

using Server.Models;

#endregion

namespace Server.Application.Clients
{
    #region Interfaces

    /// <summary>
    /// Repository for client persistence operations.
    /// </summary>
    public interface IClientRepository
    {
        /// <summary>
        /// Gets clients with optional filters.
        /// </summary>
        Task<IReadOnlyList<ClientListItemDto>> GetListAsync(ClientFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a client by ID.
        /// </summary>
        Task<Client?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new client.
        /// </summary>
        Task AddAsync(Client entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing client.
        /// </summary>
        Task UpdateAsync(Client entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a client.
        /// </summary>
        Task RemoveAsync(Client entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    #endregion
}
