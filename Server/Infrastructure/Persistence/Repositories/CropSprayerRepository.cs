#region Imports

using Microsoft.EntityFrameworkCore;
using Server.Application.CropSprayers;
using Server.Data;
using Server.Models;
using System.Text.RegularExpressions;

#endregion

namespace Server.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of crop sprayer repository.
    /// </summary>
    public class CropSprayerRepository : ICropSprayerRepository
    {
        #region Declarations

        private readonly AppDbContext _context;
        private static readonly Regex YearRegex = new(@"^\d{4}$", RegexOptions.Compiled);

        #endregion

        #region Constructor

        public CropSprayerRepository(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<CropSprayerListItemDto>> GetListAsync(CropSprayerFilterDto filter, CancellationToken cancellationToken = default)
        {
            var joinedQuery = from cs in _context.CropSprayers
                             join c in _context.Clients on cs.OwnerId equals c.Id into clients
                             from client in clients.DefaultIfEmpty()
                             select new { CropSprayer = cs, Client = client };

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var term = filter.Q.Trim().ToLowerInvariant();
                joinedQuery = joinedQuery.Where(mc =>
                    mc.CropSprayer.SerialNumber.ToLower().Contains(term) ||
                    mc.CropSprayer.SprayerName.ToLower().Contains(term) ||
                    mc.CropSprayer.Manufacturer.ToLower().Contains(term) ||
                    mc.CropSprayer.ProductionYear.ToLower().Contains(term) ||
                    (mc.Client != null && mc.Client.DisplayName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
                joinedQuery = joinedQuery.Where(mc => mc.CropSprayer.Type == filter.Type);

            if (!string.IsNullOrWhiteSpace(filter.Kind))
                joinedQuery = joinedQuery.Where(mc => mc.CropSprayer.Kind == filter.Kind);

            if (!string.IsNullOrWhiteSpace(filter.Manufacturer))
            {
                var manufacturerTerm = filter.Manufacturer.Trim().ToLowerInvariant();
                joinedQuery = joinedQuery.Where(mc => mc.CropSprayer.Manufacturer.ToLower().Contains(manufacturerTerm));
            }

            if (!string.IsNullOrWhiteSpace(filter.YearFrom) && YearRegex.IsMatch(filter.YearFrom))
                joinedQuery = joinedQuery.Where(mc => string.Compare(mc.CropSprayer.ProductionYear, filter.YearFrom) >= 0);

            if (!string.IsNullOrWhiteSpace(filter.YearTo) && YearRegex.IsMatch(filter.YearTo))
                joinedQuery = joinedQuery.Where(mc => string.Compare(mc.CropSprayer.ProductionYear, filter.YearTo) <= 0);

            return await joinedQuery
                .OrderByDescending(mc => mc.CropSprayer.CreatedAt)
                .Select(mc => new CropSprayerListItemDto
                {
                    SerialNumber = mc.CropSprayer.SerialNumber,
                    SprayerName = mc.CropSprayer.SprayerName,
                    Manufacturer = mc.CropSprayer.Manufacturer,
                    ProductionYear = mc.CropSprayer.ProductionYear,
                    Type = mc.CropSprayer.Type,
                    Kind = mc.CropSprayer.Kind,
                    OwnerId = mc.CropSprayer.OwnerId,
                    OwnerName = mc.Client != null ? mc.Client.DisplayName : null,
                    CreatedAt = mc.CropSprayer.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<CropSprayer?> GetByIdAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            return await _context.CropSprayers.FindAsync(new object[] { serialNumber }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            return await _context.CropSprayers.AnyAsync(cs => cs.SerialNumber == serialNumber, cancellationToken);
        }

        /// <inheritdoc />
        public async Task AddAsync(CropSprayer entity, CancellationToken cancellationToken = default)
        {
            _context.CropSprayers.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(CropSprayer entity, CancellationToken cancellationToken = default)
        {
            _context.CropSprayers.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveAsync(CropSprayer entity, CancellationToken cancellationToken = default)
        {
            _context.CropSprayers.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string?> GetClientDisplayNameAsync(string? clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(clientId))
                return null;

            var client = await _context.Clients.FindAsync(new object[] { clientId }, cancellationToken);
            return client?.DisplayName;
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        #endregion
    }
}
