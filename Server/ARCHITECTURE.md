## Target backend architecture

This document captures the **target layering** for the `Server` project so that the Web API can be cleanly split from the frontend and deployed independently in the cloud.

The goal is to keep the existing `Server` project as the host for now, but to organize code so that:

- **API/Host layer** is thin and HTTP-focused.
- **Application layer** owns use cases and business orchestration.
- **Domain layer** owns core entities and invariants.
- **Infrastructure layer** owns persistence, logging, and concrete implementations.
- **Cross-cutting layers** handle logging/auditing, sessions, and security in one place.

### Solution / project layout

For now we stay with a single project (`Server`) and introduce folders and namespaces representing the layers. Later these can be split into separate `.csproj` projects without changing internal code structure.

```text
Server/
  Api/
    Controllers/
      AuthController.cs
      UsersController.cs
      CropSprayersController.cs
      InspectionProtocolsController.cs
      HistoryController.cs
    Endpoints/
      ClientsEndpoints.cs
      InspectionsEndpoints.cs
      HistoryEndpoints.cs

  Application/
    Clients/
      IClientService.cs
      ClientService.cs
      Dtos/
        ClientListItemDto.cs
        ClientDetailDto.cs
        ClientCreateUpdateDto.cs
    Inspections/
      IInspectionService.cs
      InspectionService.cs
      Dtos/...
    InspectionProtocols/
      IInspectionProtocolService.cs
      InspectionProtocolService.cs
    CropSprayers/
      ICropSprayerService.cs
      CropSprayerService.cs
    Users/
      IUserManagementService.cs
      UserManagementService.cs
    History/
      IHistoryService.cs
      HistoryService.cs

  Domain/
    Clients/
      Client.cs
    Inspections/
      Inspection.cs
      InspectionItem.cs
    InspectionProtocols/
      InspectionProtocol.cs
    CropSprayers/
      CropSprayer.cs
    Users/
      User.cs
      UserSession.cs
    Security/
      SecurityEventLog.cs
      LoginAttempt.cs
    Common/
      ChangeLog.cs
      ValueObjects, enums, shared primitives

  Infrastructure/
    Persistence/
      AppDbContext.cs
      EntityConfigurations/
        ClientConfiguration.cs
        InspectionConfiguration.cs
        ...
      Repositories/
        ClientRepository.cs
        InspectionRepository.cs
        InspectionProtocolRepository.cs
        CropSprayerRepository.cs
        UserRepository.cs
        SessionRepository.cs
    Security/
      TokenService.cs
      PasswordService.cs
      SqlInjectionDetector.cs
      InputValidationService.cs
      RateLimitBlockingService.cs
    Sessions/
      SessionManagementService.cs
      SessionCleanupService.cs
    Logging/
      SecurityAuditService.cs
      ChangeLogStore.cs
    Http/
      GlobalExceptionHandler.cs
      Middlewares/
        CorrelationIdMiddleware.cs
        SecurityHeadersMiddleware.cs
        ValidationMiddleware.cs

  Common/
    Exceptions/
      DomainException.cs
      ValidationException.cs
      SecurityException.cs
      ConcurrencyException.cs
    Configuration/
      JwtOptions.cs
      RateLimitingOptions.cs
```

### Layer responsibilities

- **Api (Host) layer**
  - Defines HTTP surface (`/api/...`) via controllers and minimal-API endpoint groups.
  - Performs only HTTP concerns: routing, model binding, response codes.
  - Delegates all business work to the Application layer.

- **Application layer**
  - Implements **use cases** such as:
    - Create/update/delete client.
    - Create/update inspection.
    - Generate and update inspection protocols.
    - Manage crop sprayers.
    - Manage users and roles.
  - Coordinates:
    - Domain entities and repositories.
    - Session checks and authorization rules.
    - Logging/auditing calls.
  - Depends on **abstractions** for persistence and logging (interfaces), not directly on EF Core.

- **Domain layer**
  - Contains **entities**, value objects, and core invariants:
    - What makes a client valid.
    - How inspection and protocol states change.
    - How a session is considered active/inactive.
  - No dependencies on infrastructure or HTTP.

- **Infrastructure layer**
  - Implements:
    - `AppDbContext` and entity mappings.
    - Repository implementations for domain entities.
    - Security plumbing (token generation, password hashing, rate-limit blocking).
    - Session storage and cleanup.
    - Logging and auditing storage (e.g. security event logs, change logs).
  - Talks to the actual database and external infrastructure.

- **Common / cross-cutting**
  - Shared exceptions, configuration options, and middleware that are used across layers.

### Mapping existing code to layers

High-level mapping from the current codebase:

- **Controllers** (`Server/Controllers/*.cs`) → `Server/Api/Controllers/`.
- **Minimal APIs in `Program.cs`** (clients, inspections, history) → endpoint classes under `Server/Api/Endpoints/` that call into application services.
- **`AppDbContext` and entity sets** (`Server/Data/AppDbContext.cs`) → `Infrastructure/Persistence/AppDbContext.cs`.
- **Models/Entities** (`Server/Models/*.cs`) → `Domain/*` folders by domain area.
- **Security & sessions** (`TokenService`, `PasswordService`, `SessionManagementService`, `SessionCleanupService`, `SqlInjectionDetector`, `InputValidationService`, `RateLimitBlockingService`) → `Infrastructure/Security` and `Infrastructure/Sessions`.
- **Logging & auditing** (`SecurityAuditService`, change logging in `AppDbContext`, `GlobalExceptionHandler`) → `Infrastructure/Logging` and `Infrastructure/Http`.
- **Middleware** (`CorrelationIdMiddleware`, `SecurityHeadersMiddleware`, `ValidationMiddleware`) → `Infrastructure/Http/Middlewares`.

This structure keeps the **public HTTP API stable at `/api/...`** while allowing the backend to be cleanly separated from the Angular frontend and deployed as an independent service in the cloud.

