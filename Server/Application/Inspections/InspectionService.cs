#region Imports

using Server.Models;

#endregion

namespace Server.Application.Inspections
{
    /// <summary>
    /// Application service for inspection operations.
    /// </summary>
    public class InspectionService : IInspectionService
    {
        #region Declarations

        private readonly IInspectionRepository _repository;

        #endregion

        #region Constructor

        public InspectionService(IInspectionRepository repository)
        {
            _repository = repository;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<InspectionResponseDto>> GetListAsync(CancellationToken cancellationToken = default)
        {
            var inspections = await _repository.GetListAsync(cancellationToken);
            return inspections.Select(ToDto).ToList();
        }

        /// <inheritdoc />
        public async Task<InspectionResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var inspection = await _repository.GetByIdAsync(id, cancellationToken);
            return inspection == null ? null : ToDto(inspection);
        }

        /// <inheritdoc />
        public async Task<InspectionResponseDto> CreateAsync(InspectionCreateDto dto, CancellationToken cancellationToken = default)
        {
            var inspection = new Inspection
            {
                VehiclePlate = dto.VehiclePlate,
                InspectorName = dto.InspectorName,
                InspectionDate = dto.InspectionDate,
                Notes = dto.Notes
            };

            foreach (var it in dto.Items ?? new List<InspectionItemDto>())
            {
                inspection.Items.Add(new InspectionItem { Description = it.Description, Passed = it.Passed });
            }

            var saved = await _repository.AddAsync(inspection, cancellationToken);
            return ToDto(saved);
        }

        #endregion

        #region Methods - Private

        private static InspectionResponseDto ToDto(Inspection i)
        {
            return new InspectionResponseDto(
                i.Id,
                i.VehiclePlate,
                i.InspectorName,
                i.InspectionDate,
                i.Notes,
                i.Items.Select(it => new InspectionItemDto(it.Description, it.Passed)).ToList()
            );
        }

        #endregion
    }
}
