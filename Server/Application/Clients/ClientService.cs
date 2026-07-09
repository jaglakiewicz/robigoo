#region Imports

using Server.Models;

#endregion

namespace Server.Application.Clients
{
    /// <summary>
    /// Application service for client (customer) operations.
    /// </summary>
    public class ClientService : IClientService
    {
        #region Declarations

        private readonly IClientRepository _repository;

        #endregion

        #region Constructor

        public ClientService(IClientRepository repository)
        {
            _repository = repository;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public async Task<IReadOnlyList<ClientListItemDto>> GetListAsync(ClientFilterDto filter, CancellationToken cancellationToken = default)
        {
            return await _repository.GetListAsync(filter, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ClientDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return null;

            return ToDetailDto(entity);
        }

        /// <inheritdoc />
        public async Task<(ClientDetailDto? Detail, string? ValidationError)> CreateAsync(ClientCreateUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var displayName = ComputeDisplayName(dto);
            var entity = new Client
            {
                Id = Guid.NewGuid().ToString(),
                ClientType = dto.ClientType,
                DisplayName = displayName,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Pesel = dto.Pesel,
                CompanyName = dto.CompanyName,
                Nip = dto.Nip,
                Regon = dto.Regon,
                Voivodeship = dto.Voivodeship,
                City = dto.City,
                Street = dto.Street,
                BuildingNumber = dto.BuildingNumber,
                ApartmentNumber = dto.ApartmentNumber,
                ZipCode = dto.ZipCode,
                Post = dto.Post,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(entity, cancellationToken);
            return (ToDetailDto(entity), null);
        }

        /// <inheritdoc />
        public async Task<(ClientDetailDto? Detail, string? ValidationError)> UpdateAsync(string id, ClientCreateUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return (null, null);

            entity.ClientType = dto.ClientType;
            entity.DisplayName = ComputeDisplayName(dto);
            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.Pesel = dto.Pesel;
            entity.CompanyName = dto.CompanyName;
            entity.Nip = dto.Nip;
            entity.Regon = dto.Regon;
            entity.Voivodeship = dto.Voivodeship;
            entity.City = dto.City;
            entity.Street = dto.Street;
            entity.BuildingNumber = dto.BuildingNumber;
            entity.ApartmentNumber = dto.ApartmentNumber;
            entity.ZipCode = dto.ZipCode;
            entity.Post = dto.Post;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity, cancellationToken);
            return (ToDetailDto(entity), null);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return false;

            await _repository.RemoveAsync(entity, cancellationToken);
            return true;
        }

        #endregion

        #region Methods - Private

        private static string ComputeDisplayName(ClientCreateUpdateDto dto)
        {
            return dto.ClientType == "company"
                ? dto.CompanyName ?? string.Empty
                : $"{dto.FirstName} {dto.LastName}".Trim();
        }

        private static ClientDetailDto ToDetailDto(Client c)
        {
            return new ClientDetailDto
            {
                Id = c.Id,
                ClientType = c.ClientType,
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
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            };
        }

        #endregion
    }
}
