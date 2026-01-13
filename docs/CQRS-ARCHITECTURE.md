# CQRS Architecture Guide

## Overview

This application implements **CQRS (Command Query Responsibility Segregation)** pattern using **MediatR** library, providing a clean separation between operations that modify data (Commands) and operations that read data (Queries).

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                   (Enterprise.WebApi)                 │
│  Controllers → IMediator → Commands/Queries              │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│              (Enterprise.Application)                 │
│                                                           │
│  ┌──────────────────┐        ┌──────────────────┐      │
│  │    Commands      │        │     Queries      │      │
│  │  - Create        │        │  - GetAll        │      │
│  │  - Update        │        │  - GetById       │      │
│  │  - Delete        │        │  - GetByCategory │      │
│  └──────────────────┘        └──────────────────┘      │
│           ↓                           ↓                  │
│  ┌──────────────────┐        ┌──────────────────┐      │
│  │ Command Handlers │        │ Query Handlers   │      │
│  └──────────────────┘        └──────────────────┘      │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                     │
│             (Enterprise.Infrastructure)               │
│  Repositories → Unit of Work → Database                 │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                     Domain Layer                         │
│                (Enterprise.Domain)                    │
│  Entities, Interfaces, Business Rules                    │
└─────────────────────────────────────────────────────────┘
```

## Dependency Flow

The architecture follows strict dependency rules:

```
API Layer
    ↓ (depends on)
Application Layer + Infrastructure Layer
    ↓ (depends on)
Domain Layer
```

**Key Principle**: Dependencies flow inward. The Domain layer has NO dependencies on any other layer.

## CQRS Components

### Commands (Write Operations)

Commands represent operations that **change** the system state.

**Location**: `Application/Features/Products/Commands/`

**Structure**:

```
Commands/
├── CreateProduct/
│   ├── CreateProductCommand.cs          # Command definition
│   ├── CreateProductCommandHandler.cs   # Handler logic
│   └── CreateProductCommandValidator.cs # Validation rules
├── UpdateProduct/
│   ├── UpdateProductCommand.cs
│   ├── UpdateProductCommandHandler.cs
│   └── UpdateProductCommandValidator.cs
└── DeleteProduct/
    ├── DeleteProductCommand.cs
    └── DeleteProductCommandHandler.cs
```

**Example Command**:

```csharp
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string SKU
) : IRequest<ProductDto>;
```

**Example Handler**:

```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Business logic
        var product = new Product { /* ... */ };
        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProductDto>(product);
    }
}
```

### Queries (Read Operations)

Queries represent operations that **retrieve** data without modifying state.

**Location**: `Application/Features/Products/Queries/`

**Structure**:

```
Queries/
├── GetAllProducts/
│   ├── GetAllProductsQuery.cs
│   └── GetAllProductsQueryHandler.cs
├── GetProductById/
│   ├── GetProductByIdQuery.cs
│   └── GetProductByIdQueryHandler.cs
└── GetProductsByCategory/
    ├── GetProductsByCategoryQuery.cs
    └── GetProductsByCategoryQueryHandler.cs
```

**Example Query**:

```csharp
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
```

**Example Handler**:

```csharp
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }
}
```

## Controller Usage

Controllers act as **thin layers** that dispatch requests to MediatR:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        if (product == null)
            return NotFound(new ApiResponse<ProductDto>($"Product with ID {id} not found"));
        
        return Ok(new ApiResponse<ProductDto>(product, "Product retrieved successfully"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(dto.Name, dto.Description, dto.Price, dto.Stock, dto.Category, dto.SKU);
        var product = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, 
            new ApiResponse<ProductDto>(product, "Product created successfully"));
    }
}
```

## Benefits of CQRS

### 1. **Separation of Concerns**

- Read and write operations are completely separated
- Each operation has its own model and logic
- Easier to understand and maintain

### 2. **Single Responsibility**

- Each handler does one thing and does it well
- Commands modify state, Queries read state
- Clear responsibilities

### 3. **Scalability**

- Read and write operations can be scaled independently
- Queries can be optimized separately from commands
- Can use different data stores for reads vs writes (if needed)

### 4. **Testability**

- Handlers can be unit tested in isolation
- Mock dependencies easily
- Clear input/output contracts

### 5. **Flexibility**

- Easy to add cross-cutting concerns (logging, validation, caching)
- MediatR pipeline behaviors for common functionality
- Handlers are independent and decoupled

## Adding New Features

### Adding a New Command

1. Create folder: `Features/Products/Commands/YourCommand/`
2. Create command record: `YourCommand.cs`
3. Create handler: `YourCommandHandler.cs`
4. Create validator (optional): `YourCommandValidator.cs`
5. Add endpoint in controller

Example:

```csharp
// 1. Command
public record UpdatePriceCommand(Guid ProductId, decimal NewPrice) : IRequest;

// 2. Handler
public class UpdatePriceCommandHandler : IRequestHandler<UpdatePriceCommand>
{
    // Implementation
}

// 3. Controller
[HttpPatch("{id}/price")]
public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] decimal newPrice)
{
    await _mediator.Send(new UpdatePriceCommand(id, newPrice));
    return NoContent();
}
```

### Adding a New Query

1. Create folder: `Features/Products/Queries/YourQuery/`
2. Create query record: `YourQuery.cs`
3. Create handler: `YourQueryHandler.cs`
4. Add endpoint in controller

Example:

```csharp
// 1. Query
public record GetLowStockProductsQuery(int Threshold) : IRequest<IEnumerable<ProductDto>>;

// 2. Handler
public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, IEnumerable<ProductDto>>
{
    // Implementation
}

// 3. Controller
[HttpGet("low-stock")]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts([FromQuery] int threshold = 10)
{
    var products = await _mediator.Send(new GetLowStockProductsQuery(threshold));
    return Ok(products);
}
```

## Best Practices

### ✅ Do

- Keep handlers focused and small
- Use records for commands/queries (immutability)
- Validate commands before handling
- Return DTOs from handlers, not domain entities
- Use meaningful names for commands/queries
- Group related features in folders

### ❌ Don't

- Put business logic in controllers
- Return domain entities directly from API
- Mix read and write logic in the same handler
- Create generic "UpdateEntity" commands
- Skip validation on commands

## MediatR Pipeline Behaviors

You can add cross-cutting concerns using MediatR behaviors:

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Validate before handling
        var failures = _validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

Register in `DependencyInjection.cs`:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

## Testing

### Unit Testing Handlers

```csharp
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProduct()
    {
        // Arrange
        var repository = Mock.Of<IRepository<Product>>();
        var unitOfWork = Mock.Of<IUnitOfWork>();
        var mapper = Mock.Of<IMapper>();
        var handler = new CreateProductCommandHandler(repository, unitOfWork, mapper);
        
        var command = new CreateProductCommand("Test", "Description", 10.0m, 100, "Category", "SKU-001");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Mock.Get(repository).Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Summary

This CQRS implementation provides:

- ✅ Clean separation between reads and writes
- ✅ Testable, maintainable code
- ✅ Single Responsibility Principle
- ✅ Easy to extend with new features
- ✅ Proper dependency flow (API → Application → Domain)
- ✅ Type-safe request/response patterns
- ✅ Built-in validation support
- ✅ Scalable architecture

The architecture ensures that business logic stays in the Application layer (handlers), while the API layer remains thin and focused on HTTP concerns.
