## Endpoint migration plan

This document explains **how existing HTTP endpoints will move** into the new layered architecture (Api → Application → Domain → Infrastructure) without losing behavior.

The focus is on three areas currently implemented as minimal APIs in `Program.cs`:

- `/api/clients`
- `/api/inspections`
- `/api/History/{entityName}/{entityId}`

and on the existing MVC controllers under `Server/Controllers`.

---

## 1. General migration pattern

For each endpoint or controller:

1. **Define an Application service** in `Application/<Area>` (e.g. `Application/Clients/ClientService`).
2. **Define repository interfaces** in `Application/<Area>` (e.g. `IClientRepository`) that express required persistence operations.
3. **Implement repositories** in `Infrastructure/Persistence/Repositories` using `AppDbContext`.
4. **Expose HTTP endpoints** via:
   - Attribute-routed controllers in `Api/Controllers`, or
   - Minimal-API endpoint groups in `Api/Endpoints`.
5. Ensure:
   - **Authorization** is handled by `[Authorize]` / policies and `AuthorizationService`.
   - **Session validity** is enforced centrally by JWT validation + `SessionManagementService`.
   - **Auditing** relies on `AppDbContext` change logging + `SecurityAuditService` where appropriate.

Controllers and endpoint handlers become **thin** and delegate to application services.

---

## 2. `/api/clients` endpoints (minimal APIs → Clients module)

**Current location**

- Minimal APIs in `Program.cs`:
  - `GET /api/clients` (list with filters)
  - `GET /api/clients/{id}` (detail)
  - `POST /api/clients` (create)
  - `PUT /api/clients/{id}` (update)
  - `DELETE /api/clients/{id}` (delete)
- They:
  - Use `AppDbContext` directly.
  - Compute `DisplayName` on create/update.
  - Perform manual session checks via `IsSessionActiveAsync`.

**Target structure**

- `Application/Clients/IClientService.cs`
- `Application/Clients/ClientService.cs`
- `Application/Clients/Dtos/ClientListItemDto.cs`, `ClientDetailDto.cs`, `ClientCreateUpdateDto.cs` (reusing existing DTOs, moved from `Models`).
- `Application/Clients/IClientRepository.cs`
- `Infrastructure/Persistence/Repositories/ClientRepository.cs`
- `Api/Endpoints/ClientsEndpoints.cs` (minimal-API group) **or** `Api/Controllers/ClientsController.cs`.

**Migration steps**

1. **DTOs**
   - Move `ClientListItemDto`, `ClientDetailDto`, `ClientCreateUpdateDto` from `Server/Models` to `Application/Clients/Dtos` (update namespaces).

2. **Repository**
   - Define `IClientRepository` in `Application/Clients`:
     - Methods: `GetClientsAsync(filter)`, `GetByIdAsync(id)`, `AddAsync(Client)`, `UpdateAsync(Client)`, `DeleteAsync(Client)`.
   - Implement `ClientRepository` in `Infrastructure/Persistence/Repositories` using `AppDbContext.Clients`.

3. **Service**
   - Implement `ClientService`:
     - Uses `IClientRepository` and mapping between `Client` and DTOs.
     - Encapsulates:
       - Filter logic (`q`, `clientType`, `city`).
       - `DisplayName` computation for natural vs company clients.
       - Any future business rules (e.g. cannot delete clients with active references).

4. **Endpoints**
   - Create `Api/Endpoints/ClientsEndpoints` with:

     - `MapGroup("/api/clients")` and handlers:
       - `GetClientsAsync(ClientListFilterDto filter, IClientService service)`
       - `GetClientAsync(string id, IClientService service)`
       - `CreateClientAsync(ClientCreateUpdateDto dto, IClientService service)`
       - `UpdateClientAsync(string id, ClientCreateUpdateDto dto, IClientService service)`
       - `DeleteClientAsync(string id, IClientService service)`
     - Apply `.RequireAuthorization()` on the group; **do not** perform manual session checks in handlers (rely on JWT + `SessionManagementService`).

5. **Remove minimal APIs**
   - After endpoints are wired and tested, delete the `/api/clients` minimal APIs from `Program.cs` and register `ClientsEndpoints` from there.

---

## 3. `/api/inspections` endpoints (minimal APIs → Inspections module)

**Current location**

- Minimal APIs in `Program.cs`:
  - `GET /api/inspections`
  - `POST /api/inspections`
- They:
  - Use `AppDbContext.Inspections` and `InspectionItems` directly.
  - Map between `InspectionCreateDto`, `InspectionItemDto`, and entities inline.

**Target structure**

- `Application/Inspections/IInspectionService.cs`
- `Application/Inspections/InspectionService.cs`
- `Application/Inspections/Dtos/InspectionCreateDto.cs`, `InspectionResponseDto.cs`, `InspectionItemDto.cs` (moved from `Models`).
- `Application/Inspections/IInspectionRepository.cs`
- `Infrastructure/Persistence/Repositories/InspectionRepository.cs`
- `Api/Endpoints/InspectionsEndpoints.cs` **or** `Api/Controllers/InspectionsController.cs`.

**Migration steps**

1. **DTOs**
   - Move inspection DTOs from `Server/Models` into `Application/Inspections/Dtos` (update namespaces).

2. **Repository**
   - Define `IInspectionRepository`:
     - Methods for listing inspections (with items) and creating inspections.
   - Implement `InspectionRepository` using `AppDbContext.Inspections` and related entities.

3. **Service**
   - Implement `InspectionService`:
     - Builds `Inspection` entities from `InspectionCreateDto`.
     - Performs validation and any future business rules.
     - Uses `IInspectionRepository` and returns DTOs.

4. **Endpoints**
   - Create `InspectionsEndpoints` with handlers that call `IInspectionService`.
   - Apply `.RequireAuthorization()` and remove manual session checks.

5. **Remove minimal APIs**
   - Delete `/api/inspections` minimal APIs from `Program.cs` once endpoints are wired and tested.

---

## 4. `/api/History/{entityName}/{entityId}` (minimal API → History module)

**Current location**

- Minimal API in `Program.cs`:
  - Joins `ChangeLogs` with `Users` to return a history list with optional user details.

**Target structure**

- `Application/History/IHistoryService.cs`
- `Application/History/HistoryService.cs`
- `Application/History/Dtos/ChangeLogEntryDto.cs`
- `Application/History/IHistoryRepository.cs`
- `Infrastructure/Persistence/Repositories/HistoryRepository.cs`
- `Api/Endpoints/HistoryEndpoints.cs` (or `HistoryController`).

**Migration steps**

1. **DTO**
   - Define `ChangeLogEntryDto` capturing:
     - `Id`, `EntityName`, `EntityId`, `Changes`, `When`, `Who`, `UserFirstName`, `UserLastName`, `UserAvatarData`.

2. **Repository**
   - Implement `IHistoryRepository` / `HistoryRepository` for querying `ChangeLogs` (and joining `Users`).

3. **Service**
   - Implement `HistoryService` with `GetHistoryAsync(string entityName, string entityId)`.

4. **Endpoint**
   - Implement `HistoryEndpoints` with `GET /api/history/{entityName}/{entityId}` calling `IHistoryService`.
   - Keep `.RequireAuthorization()` and no manual session checks.

5. **Remove minimal API**
   - Remove `/api/History/{entityName}/{entityId}` minimal API from `Program.cs` once migrated.

---

## 5. Existing MVC controllers

**Current controllers**

- `AuthController` – authentication and token/session management.
- `UsersController` – user CRUD and admin operations.
- `InspectionProtocolsController` – inspection protocol management and XML/XSL generation.
- `CropSprayersController` – crop sprayer (machine) CRUD.
- `HistoryController` – additional history-related endpoints.

**Migration pattern**

1. **Move controllers** into `Api/Controllers` namespace without changing routes.
2. **Introduce dedicated application services** per domain:
   - `Application/Auth/AuthService` (login, refresh, logout).
   - `Application/Users/UserManagementService`.
   - `Application/InspectionProtocols/InspectionProtocolService`.
   - `Application/CropSprayers/CropSprayerService`.
   - `Application/History/HistoryService` (shared with minimal-history endpoint above).
3. **Refactor controller actions** to:
   - Take DTOs and route parameters.
   - Delegate to application services for all business operations and data access.
   - Keep only HTTP, mapping, and response code concerns in the controller.

Over time, controllers become **thin shells** and the domain/application logic moves to testable services.

---

## 6. Session and authorization behavior during migration

- All endpoints and controllers should:
  - Use `[Authorize]` / `.RequireAuthorization()` for authentication.
  - Rely on **JwtBearer** + `SessionManagementService` (via `OnTokenValidated`) to ensure that only active sessions can call the API.
  - Avoid manual calls to `AppDbContext.UserSessions` or custom `IsSessionActiveAsync` helpers in endpoint handlers.

This keeps **session validation centralized** and simplifies endpoint code, while the existing `AppDbContext` change logging and `History` endpoints provide the full audit trail (who changed what, when, and how).

