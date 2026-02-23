#region Imports

using Microsoft.EntityFrameworkCore;
using Server.Application.Inspections;
using Server.Data;
using Server.Models;

#endregion

namespace Server.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of inspection repository.
    /// </summary>
    public class InspectionRepository : IInspectionRepository
    {
        #region Declarations

        private readonly AppDbContext _context;

        #endregion

        #region Constructor

        public InspectionRepository(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<Inspection>> GetListAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Inspections
                .Include(i => i.Items)
                .OrderByDescending(i => i.Id)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Inspection?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Inspections
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Inspection> AddAsync(Inspection entity, CancellationToken cancellationToken = default)
        {
            _context.Inspections.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        #endregion
    }
}
