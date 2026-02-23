#region Imports

using Server.Models;
using Server.Services;
using System.Text.RegularExpressions;

#endregion

namespace Server.Application.CropSprayers
{
    /// <summary>
    /// Application service for crop sprayer (machine) operations.
    /// Encapsulates business logic, validation, and orchestration.
    /// </summary>
    public class CropSprayerService : ICropSprayerService
    {
        #region Declarations

        private readonly ICropSprayerRepository _repository;
        private readonly IConcurrencyController _concurrencyController;

        private static readonly Regex YearRegex = new(@"^\d{4}$", RegexOptions.Compiled);

        #endregion

        #region Constructor

        public CropSprayerService(
            ICropSprayerRepository repository,
            IConcurrencyController concurrencyController)
        {
            _repository = repository;
            _concurrencyController = concurrencyController;
        }

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<CropSprayerListItemDto>> GetListAsync(CropSprayerFilterDto filter, CancellationToken cancellationToken = default)
        {
            // Repository doesn't support complex queries - we need to expose a query method.
            // For now, we'll add a GetFilteredListAsync to the repository that returns the list.
            // Actually, the repository pattern typically has the repository do the query.
            // Let me add GetListAsync to the repository that accepts the filter.
            var items = await _repository.GetListAsync(filter, cancellationToken);
            return items;
        }

        /// <inheritdoc />
        public async Task<CropSprayerDetailDto?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(serialNumber, cancellationToken);
            if (entity == null)
                return null;

            var ownerName = await _repository.GetClientDisplayNameAsync(entity.OwnerId, cancellationToken);
            return ToDetailDto(entity, ownerName);
        }

        /// <inheritdoc />
        public async Task<(CropSprayerDetailDto? Detail, string? ValidationError)> CreateAsync(CropSprayerCreateUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var validationError = ValidateDto(dto, isCreate: true);
            if (validationError != null)
                return (null, validationError);

            if (await _repository.ExistsAsync(dto.SerialNumber.Trim(), cancellationToken))
                return (null, "Opryskiwacz o podanym numerze już istnieje");

            var entity = MapToEntity(dto);
            await _repository.AddAsync(entity, cancellationToken);

            var ownerName = await _repository.GetClientDisplayNameAsync(entity.OwnerId, cancellationToken);
            var detail = ToDetailDto(entity, ownerName);
            return (detail, null);
        }

        /// <inheritdoc />
        public async Task<(CropSprayerDetailDto? Detail, string? ValidationError, string? ConcurrencyConflict)> UpdateAsync(string serialNumber, CropSprayerCreateUpdateDto dto, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(serialNumber, dto.SerialNumber, StringComparison.Ordinal))
                return (null, "Nie można zmienić numeru seryjnego opryskiwacza", null);

            var entity = await _repository.GetByIdAsync(serialNumber, cancellationToken);
            if (entity == null)
                return (null, null, null); // Not found - caller will return 404

            var validationError = ValidateDto(dto, isCreate: false);
            if (validationError != null)
                return (null, validationError, null);

            var result = await _concurrencyController.ExecuteWithOptimisticConcurrencyAsync(async () =>
            {
                var current = await _repository.GetByIdAsync(serialNumber, cancellationToken);
                if (current == null)
                    throw new InvalidOperationException("Entity not found during update");

                MapToEntity(dto, current);
                current.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(current, cancellationToken);
                return current;
            });

            if (!result.Success)
            {
                var conflictMsg = result.Conflict?.Message ?? "The record was modified by another user. Please refresh and try again.";
                return (null, null, conflictMsg);
            }

            var ownerName = await _repository.GetClientDisplayNameAsync(result.Result!.OwnerId, cancellationToken);
            var detail = ToDetailDto(result.Result, ownerName);
            return (detail, null, null);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(serialNumber, cancellationToken);
            if (entity == null)
                return false;

            await _repository.RemoveAsync(entity, cancellationToken);
            return true;
        }

        #endregion

        #region Methods - Private

        private static CropSprayerDetailDto ToDetailDto(CropSprayer cs, string? ownerNameOverride = null)
        {
            return new CropSprayerDetailDto
            {
                SerialNumber = cs.SerialNumber,
                SprayerName = cs.SprayerName,
                Type = cs.Type,
                Kind = cs.Kind,
                Manufacturer = cs.Manufacturer,
                ProductionYear = cs.ProductionYear,
                PurchaseDate = cs.PurchaseDate,
                PumpPiston = cs.PumpPiston,
                PumpDiaphragm = cs.PumpDiaphragm,
                PumpOther = cs.PumpOther,
                PumpOtherType = cs.PumpOtherType,
                PumpFlowRate = cs.PumpFlowRate,
                TankCapacity = cs.TankCapacity,
                HasFlushing = cs.HasFlushing,
                HasDiluter = cs.HasDiluter,
                HasWashingDevice = cs.HasWashingDevice,
                HasManometer = cs.HasManometer,
                HasComputer = cs.HasComputer,
                BoomWidth = cs.BoomWidth,
                BoomWet = cs.BoomWet,
                BoomDry = cs.BoomDry,
                BoomDampeningMechanism = cs.BoomDampeningMechanism,
                SectionCount = cs.SectionCount,
                NozzlesFieldFeatures = cs.NozzlesFieldFeatures,
                NozzlesGardenFeatures = cs.NozzlesGardenFeatures,
                FanType = cs.FanType,
                OwnerId = cs.OwnerId,
                OwnerName = ownerNameOverride ?? cs.OwnerName,
                CreatedAt = cs.CreatedAt,
                UpdatedAt = cs.UpdatedAt
            };
        }

        private static CropSprayer MapToEntity(CropSprayerCreateUpdateDto dto)
        {
            var entity = new CropSprayer();
            MapToEntity(dto, entity);
            entity.SerialNumber = dto.SerialNumber.Trim();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = null;
            return entity;
        }

        private static void MapToEntity(CropSprayerCreateUpdateDto dto, CropSprayer entity)
        {
            entity.SprayerName = dto.SprayerName.Trim();
            entity.Type = dto.Type;
            entity.Kind = dto.Kind;
            entity.Manufacturer = dto.Manufacturer.Trim();
            entity.ProductionYear = dto.ProductionYear;
            entity.PurchaseDate = dto.PurchaseDate;
            entity.PumpPiston = dto.PumpPiston;
            entity.PumpDiaphragm = dto.PumpDiaphragm;
            entity.PumpOther = dto.PumpOther;
            entity.PumpOtherType = string.IsNullOrWhiteSpace(dto.PumpOtherType) ? null : dto.PumpOtherType.Trim();
            entity.PumpFlowRate = dto.PumpFlowRate;
            entity.TankCapacity = dto.TankCapacity;
            entity.HasFlushing = dto.HasFlushing;
            entity.HasDiluter = dto.HasDiluter;
            entity.HasWashingDevice = dto.HasWashingDevice;
            entity.HasManometer = dto.HasManometer;
            entity.HasComputer = dto.HasComputer;
            entity.BoomWidth = dto.BoomWidth;
            entity.BoomWet = dto.BoomWet;
            entity.BoomDry = dto.BoomDry;
            entity.BoomDampeningMechanism = dto.BoomDampeningMechanism;
            entity.SectionCount = dto.SectionCount;
            entity.NozzlesFieldFeatures = dto.NozzlesFieldFeatures;
            entity.NozzlesGardenFeatures = dto.NozzlesGardenFeatures;
            entity.FanType = dto.FanType;
            entity.OwnerId = dto.OwnerId;
            entity.OwnerName = dto.OwnerName;
        }

        private static string? ValidateDto(CropSprayerCreateUpdateDto dto, bool isCreate)
        {
            if (string.IsNullOrWhiteSpace(dto.SerialNumber) && isCreate)
                return "Numer seryjny/ewidencyjny jest wymagany";

            if (string.IsNullOrWhiteSpace(dto.SprayerName))
                return "Nazwa opryskiwacza jest wymagana";

            if (string.IsNullOrWhiteSpace(dto.Type) || (dto.Type != "00" && dto.Type != "01"))
                return "Nieprawidłowy typ (dozwolone: 00 - polowy, 01 - sadowniczy)";

            if (string.IsNullOrWhiteSpace(dto.Kind) || (dto.Kind != "00" && dto.Kind != "01" && dto.Kind != "02" && dto.Kind != "03"))
                return "Nieprawidłowy rodzaj (dozwolone: 00, 01, 02, 03)";

            if (string.IsNullOrWhiteSpace(dto.Manufacturer))
                return "Producent jest wymagany";

            if (string.IsNullOrWhiteSpace(dto.ProductionYear) || !YearRegex.IsMatch(dto.ProductionYear))
                return "Rok produkcji musi mieć dokładnie 4 cyfry";

            if (dto.PumpOther && string.IsNullOrWhiteSpace(dto.PumpOtherType))
                return "Dla pompy 'inna' należy podać typ";

            if (!dto.PumpPiston && !dto.PumpDiaphragm && !dto.PumpOther)
                return "Należy wybrać co najmniej jeden typ pompy";

            return null;
        }

        #endregion
    }
}
