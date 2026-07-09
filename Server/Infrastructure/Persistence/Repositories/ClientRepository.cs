#region Imports

using Microsoft.EntityFrameworkCore;
using Server.Application.Clients;
using Server.Data;
using Server.Models;

#endregion

namespace Server.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of client repository.
    /// </summary>
    public class ClientRepository : IClientRepository
    {
        #region Declarations

        private readonly AppDbContext _context;

        #endregion

        #region Constructor

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<ClientListItemDto>> GetListAsync(ClientFilterDto filter, CancellationToken cancellationToken = default)
        {
            var queryable = _context.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var term = filter.Q.Trim().ToLowerInvariant();
                queryable = queryable.Where(c =>
                    (c.DisplayName ?? string.Empty).ToLower().Contains(term) ||
                    (c.City != null && c.City.ToLower().Contains(term)) ||
                    (c.Nip != null && c.Nip.Contains(term)) ||
                    (c.Pesel != null && c.Pesel.Contains(term)) ||
                    (c.FirstName != null && c.FirstName.ToLower().Contains(term)) ||
                    (c.LastName != null && c.LastName.ToLower().Contains(term)) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.ClientType))
                queryable = queryable.Where(c => c.ClientType == filter.ClientType);

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                var cityTerm = filter.City.Trim().ToLowerInvariant();
                queryable = queryable.Where(c => c.City != null && c.City.ToLower().Contains(cityTerm));
            }

            return await queryable
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClientListItemDto
                {
                    Id = c.Id,
                    ClientType = c.ClientType,
                    DisplayName = c.DisplayName,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Pesel = c.Pesel,
                    CompanyName = c.CompanyName,
                    Nip = c.Nip,
                    Regon = c.Regon,
                    Voivodeship = c.Voivodeship,
                    City = c.City,
                    Street = c.Street,
                    BuildingNumber = c.BuildingNumber,
                    ApartmentNumber = c.ApartmentNumber,
                    ZipCode = c.ZipCode,
                    Post = c.Post,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Client?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _context.Clients.FindAsync(new object[] { id }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task AddAsync(Client entity, CancellationToken cancellationToken = default)
        {
            _context.Clients.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Client entity, CancellationToken cancellationToken = default)
        {
            _context.Clients.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveAsync(Client entity, CancellationToken cancellationToken = default)
        {
            _context.Clients.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        #endregion
    }
}
