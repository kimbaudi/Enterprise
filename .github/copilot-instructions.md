# Enterprise API - AI Coding Instructions

## Architecture: Clean Architecture + CQRS Pattern

This is a .NET 8 Web API using **Clean Architecture** with **CQRS** via MediatR. All requests flow through ordered pipeline behaviors before reaching handlers.

### Dependency Flow (Strict Inward Rule)

```text
WebApi → Application + Infrastructure → Domain (no dependencies)
```

### Request Pipeline (Order Matters!)

```text
Controller → MediatR → LoggingBehavior → ValidationBehavior → PerformanceBehavior → Handler
```

**Critical**: Pipeline behaviors in `Application/DependencyInjection.cs` execute in registration order. Never reorder without understanding impact.

## Adding New Features (CQRS Pattern)

### 1. Commands (Write Operations)

Create in `Application/Features/{Feature}/Commands/{Action}/`:

- `{Action}Command.cs` - Record implementing `IRequest<TResponse>`
- `{Action}CommandHandler.cs` - Implements `IRequestHandler<TCommand, TResponse>`
- `{Action}CommandValidator.cs` - FluentValidation rules (optional but recommended)

**Example**: See `Application/Features/Products/Commands/CreateProduct/` for reference pattern.

**Key Pattern**:

- Use record types for commands/queries (immutable)
- Inject `IUnitOfWork` (not DbContext directly)
- Always call `await _unitOfWork.SaveChangesAsync(cancellationToken)` after repository operations
- Return DTOs, never domain entities

### 2. Queries (Read Operations)

Create in `Application/Features/{Feature}/Queries/{Action}/`:

- `{Action}Query.cs` - Record implementing `IRequest<TResponse>`
- `{Action}QueryHandler.cs` - Implements `IRequestHandler<TQuery, TResponse>`

**Example**: See `Application/Features/Products/Queries/GetProductsPaginated/` for pagination pattern.

**Key Pattern**:

- For list queries with many results, return `PaginatedResult<T>` (see `Common/Models/PaginatedResult.cs`)
- Use `IRepository<T>` methods, never direct DbContext access
- Apply `[ResponseCache]` attribute in controller for cacheable queries

### 3. Controllers (Thin Layer)

Controllers only: inject `IMediator`, dispatch commands/queries, wrap in `ApiResponse<T>`.

**Pattern**:

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class {Feature}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TDto>>> Create([FromBody] CreateDto dto, CancellationToken ct)
    {
        var command = new Create{Feature}Command(dto.Property1, dto.Property2);
        var result = await _mediator.Send(command, ct);
        return Ok(new ApiResponse<TDto>(result));
    }
}
```

**Never**: Put validation, business logic, or database access in controllers. Use MediatR pipeline behaviors.

## Validation Strategy

**Do not** validate in controllers. FluentValidation runs automatically via `ValidationBehavior` pipeline.

1. Create `{Action}CommandValidator : AbstractValidator<{Action}Command>` in same folder as command
2. ValidationBehavior auto-discovers and executes validators
3. Throws `Application.Common.Exceptions.ValidationException` on failure
4. Global exception middleware converts to 400 response

## Testing Pattern

**Location**: `tests/EnterpriseApi.Application.Tests/Features/{Feature}/{Commands|Queries}/`

**Setup**:

```csharp
private readonly Mock<IUnitOfWork> _unitOfWorkMock;
private readonly Mock<IRepository<Entity>> _repositoryMock;
private readonly Mock<IMapper> _mapperMock;
```

**Assertions**: Use FluentAssertions (`.Should().Be()`, `.Should().NotBeNull()`) not xUnit asserts.

**Example**: See `CreateProductCommandHandlerTests.cs` for complete handler testing pattern.

## Database Operations

**Migration Commands** (from solution root):

```bash
# Create migration
dotnet ef migrations add {MigrationName} --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi

# Apply migration
dotnet ef database update --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi

# Seed data (10k products, 1k users)
cd src/EnterpriseApi.DataSeeder && dotnet run
```

**Repository Pattern**: Use `IRepository<T>` and `IUnitOfWork`, never inject `ApplicationDbContext` into handlers.

## Key Conventions

- **API Versioning**: All routes use `/api/v{version:apiVersion}/` pattern (currently v1)
- **DTOs**: Map entities to DTOs using AutoMapper (config in `Application/Mappings/MappingProfile.cs`)
- **Exceptions**: Use custom exceptions (`NotFoundException`, `ValidationException`) from `Application/Common/Exceptions/`
- **Logging**: Serilog auto-logs all requests via LoggingBehavior; avoid manual logging in handlers
- **Performance**: PerformanceBehavior logs warnings for requests >500ms
- **Secrets**: JWT secrets via user-secrets (dev) or environment variables (prod), never in appsettings.json

## Authentication

Default seeded users (password: {Role}@123):

- `admin` / Admin@123 - Full access
- `manager` / Manager@123 - Manager role
- `user` / User@123 - Basic user

Login via `POST /api/v1/auth/login` to get JWT token. Use in Swagger or as `Authorization: Bearer {token}` header.

## Common Tasks

**Run & Test**:

```bash
dotnet build          # Build solution
dotnet test           # Run tests
cd src/EnterpriseApi.WebApi && dotnet run  # Start API (https://localhost:5001)
docker-compose up -d  # Run with Docker (includes SQL Server)
```

**Debugging**: Swagger UI at `https://localhost:5001` - use JWT auth button to test secured endpoints.

## Critical Files

- `Application/DependencyInjection.cs` - MediatR + pipeline behavior registration order
- `Infrastructure/DependencyInjection.cs` - Repository + DbContext registration
- `WebApi/Program.cs` - Middleware pipeline, JWT config, Serilog setup
- `Application/Common/Behaviors/` - Pipeline behaviors (logging, validation, performance)
- `docs/CQRS-ARCHITECTURE.md` - Detailed CQRS implementation guide
