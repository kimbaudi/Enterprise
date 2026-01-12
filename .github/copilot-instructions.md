# Project Setup Instructions

## Project Type: .NET Core 8 Web API - Enterprise Edition with CQRS and Best Practices

This is an enterprise-ready .NET Core 8 Web API implementing Clean Architecture with CQRS pattern and MediatR pipeline behaviors.

## Architecture Overview

The application follows Clean Architecture with CQRS (Command Query Responsibility Segregation):

- **API Layer** → Thin controllers dispatching to MediatR
- **Application Layer** → Commands (writes) and Queries (reads) with handlers
  - **Pipeline Behaviors** → Cross-cutting concerns (validation, logging, performance)
- **Infrastructure Layer** → Repositories, Unit of Work, EF Core
- **Domain Layer** → Entities, interfaces, business rules

## MediatR Pipeline Behaviors

All commands and queries flow through these pipeline behaviors in order:

1. **LoggingBehavior** - Logs all requests with execution time
2. **ValidationBehavior** - Automatic FluentValidation before handlers
3. **PerformanceBehavior** - Monitors and logs slow requests (>500ms)

```
Request → Logging → Validation → Performance → Handler → Response
```

## Setup Progress

- [x] Created copilot-instructions.md
- [x] Scaffolded .NET solution and projects
- [x] Created Domain Layer
- [x] Created Application Layer with CQRS
- [x] Created Infrastructure Layer
- [x] Created Web API Layer
- [x] Added Configuration Files
- [x] Implemented MediatR Pipeline Behaviors
- [x] Added Pagination Support
- [x] Created Custom Exceptions (NotFoundException, ValidationException)
- [x] Simplified Controllers (removed manual validation)
- [x] Created Unit Test Project with Examples
- [x] Built Project Successfully
- [x] All Tests Passing ✅
- [x] Completed Documentation

## Project Complete! 🎉

The enterprise-ready .NET Core 8 Web API is now fully set up with best practices including:

### Architecture

- Clean Architecture (Domain, Application, Infrastructure, WebApi layers)
- **CQRS Pattern** using MediatR
- Commands for write operations (Create, Update, Delete)
- Queries for read operations (GetAll, GetById, GetByCategory, GetPaginated)
- Proper dependency flow: API → Application/Infrastructure → Domain

### Features

- Entity Framework Core 8 with Repository and Unit of Work patterns
- **MediatR** for CQRS implementation with **Pipeline Behaviors**
  - **ValidationBehavior** - Automatic input validation
  - **LoggingBehavior** - Request/response logging
  - **PerformanceBehavior** - Slow request monitoring
- JWT Authentication
- Swagger/OpenAPI documentation
- Serilog logging
- FluentValidation (integrated with pipeline)
- AutoMapper
- Global error handling middleware
- Custom exceptions (NotFoundException, ValidationException)
- Health checks
- CORS support
- **Pagination support** with PaginatedResult<T>
- Docker and Docker Compose configuration
- **Comprehensive unit tests** with xUnit, Moq, and FluentAssertions

### Best Practices Implemented

✅ Thin controllers - no business logic
✅ Automatic validation via pipeline behaviors
✅ Structured logging with execution time tracking
✅ Performance monitoring for slow operations
✅ Proper exception handling with custom exceptions
✅ Pagination for list queries
✅ Comprehensive unit tests with examples
✅ Mocking and FluentAssertions for testability

### Test Coverage

The test project includes examples of:

- Command handler tests (CreateProductCommandHandler)
- Query handler tests (GetProductByIdQueryHandler)
- Pipeline behavior tests (ValidationBehavior)
- Proper use of Moq for mocking dependencies
- FluentAssertions for expressive test assertions

### Next Steps

1. **Run migrations** to create the database:

   ```bash
   dotnet ef migrations add InitialCreate --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi
   dotnet ef database update --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi
   ```

2. **Run the application**:

   ```bash
   cd src/EnterpriseApi.WebApi
   dotnet run
   ```

3. **Run tests**:

   ```bash
   dotnet test
   ```

4. **Access Swagger UI** at `https://localhost:5001` to test the API

See the [README.md](../../README.md) for complete documentation.
