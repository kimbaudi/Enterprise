# Soft Delete with Recovery - Implementation Guide

## Overview

The Enterprise Web API now implements a comprehensive **Soft Delete with Recovery** pattern across all entities. Instead of permanently removing records from the database, entities are marked as deleted and can be restored later. This approach provides data safety, audit trails, and recovery capabilities.

## Architecture

### Core Components

#### 1. ISoftDeletable Interface

**Location**: `Domain/Interfaces/ISoftDeletable.cs`

Defines the contract for entities that support soft delete:

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

#### 2. BaseEntity Enhancement

**Location**: `Domain/Common/BaseEntity.cs`

All entities inherit from `BaseEntity`, which now implements `ISoftDeletable`:

- `IsDeleted` - Flag indicating if entity is soft-deleted
- `DeletedAt` - Timestamp of when entity was deleted
- `DeletedBy` - Username of who deleted the entity

#### 3. Repository Pattern Extensions

**Location**: `Application/Common/Interfaces/IRepository.cs`

New methods added to the repository interface:

- `RestoreAsync(Guid id)` - Restore a single soft-deleted entity
- `RestoreRangeAsync(IEnumerable<T> entities)` - Restore multiple entities
- `GetDeletedByIdAsync(Guid id)` - Get a specific deleted entity
- `GetAllDeletedAsync()` - Get all deleted entities
- `GetDeletedPagedAsync(...)` - Get paginated deleted entities

## How It Works

### 1. Soft Delete Process

When an entity is deleted:

1. **DeleteAsync()** is called on the repository
2. Entity's `IsDeleted` property is set to `true`
3. **SaveChangesAsync()** in DbContext detects the change
4. Automatically sets:
   - `DeletedAt` = Current UTC timestamp
   - `DeletedBy` = Current authenticated user (from `ICurrentUserService`)

**Example**:

```csharp
await _productRepository.DeleteAsync(productId, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
// Product is now soft-deleted with DeletedAt and DeletedBy populated
```

### 2. Global Query Filters

**Location**: `Infrastructure/Persistence/ApplicationDbContext.cs`

All entity configurations include:

```csharp
entity.HasQueryFilter(e => !e.IsDeleted);
```

This means:

- ✅ Regular queries automatically exclude soft-deleted entities
- ✅ No code changes needed in existing queries
- ✅ Protection against accidentally accessing deleted data

### 3. Accessing Deleted Entities

To query soft-deleted entities, use `IgnoreQueryFilters()`:

```csharp
// Get deleted entity by ID
var deleted = await _dbSet.IgnoreQueryFilters()
    .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, cancellationToken);

// Get all deleted entities
var allDeleted = await _dbSet.IgnoreQueryFilters()
    .Where(e => e.IsDeleted)
    .ToListAsync(cancellationToken);
```

### 4. Restore Process

When an entity is restored:

1. **RestoreAsync()** is called on the repository
2. Entity's properties are reset:
   - `IsDeleted` = `false`
   - `DeletedAt` = `null`
   - `DeletedBy` = `null`
3. Entity becomes visible in regular queries again

**Example**:

```csharp
await _productRepository.RestoreAsync(productId, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
// Product is now restored and visible
```

## CQRS Implementation

### Product Commands & Queries

#### Delete Product (Soft Delete)

**Existing**: `Features/Products/Commands/DeleteProduct/`

- Command: `DeleteProductCommand(Guid Id)`
- Handler: Sets `IsDeleted = true`
- Authorization: Admin only
- Endpoint: `DELETE /api/v1/products/{id}`

#### Restore Product

**New**: `Features/Products/Commands/RestoreProduct/`

- Command: `RestoreProductCommand(Guid Id)`
- Handler: Restores soft-deleted product
- Validator: Ensures product ID is valid
- Authorization: Admin only
- Endpoint: `POST /api/v1/products/{id}/restore`

**Handler Example**:

```csharp
public async Task<bool> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
{
    var product = await _productRepository.GetDeletedByIdAsync(request.Id, cancellationToken);
    
    if (product == null)
        throw new NotFoundException("Deleted Product", request.Id);
    
    await _productRepository.RestoreAsync(request.Id, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return true;
}
```

#### Get Deleted Products

**New**: `Features/Products/Queries/GetDeletedProducts/`

- Query: `GetDeletedProductsQuery(int PageNumber, int PageSize)`
- Handler: Returns paginated list of soft-deleted products
- Validator: Validates pagination parameters
- Authorization: Admin only
- Endpoint: `GET /api/v1/products/deleted?pageNumber=1&pageSize=10`

**Response Example**:

```json
{
  "data": {
    "items": [
      {
        "id": "guid",
        "name": "Deleted Product",
        "price": 99.99,
        "isDeleted": true,
        "deletedAt": "2026-01-14T12:00:00Z",
        "deletedBy": "admin"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 5,
    "totalPages": 1
  },
  "success": true
}
```

## API Endpoints

### Delete Product (Soft Delete)

```http
DELETE /api/v1/products/{id}
Authorization: Bearer {token}
Required Role: Admin
```

### Get Deleted Products

```http
GET /api/v1/products/deleted?pageNumber=1&pageSize=10
Authorization: Bearer {token}
Required Role: Admin
```

### Restore Product

```http
POST /api/v1/products/{id}/restore
Authorization: Bearer {token}
Required Role: Admin
```

## Database Schema

### Migration: `AddSoftDeleteMetadata`

Added to all entity tables:

- `DeletedAt` (datetime2, nullable)
- `DeletedBy` (nvarchar(max), nullable)
- `IsDeleted` default value set to `false`

**Tables Updated**:

- Products
- Users
- Roles
- UserRoles
- RefreshTokens
- AuditLogs

## Usage Examples

### Swagger UI Testing

1. **Login as Admin**:

   ```json
   POST /api/v1/auth/login
   {
     "username": "admin",
     "password": "Admin@123"
   }
   ```

2. **Delete a Product** (soft delete):

   ```json
   DELETE /api/v1/products/{productId}
   ```

3. **View Deleted Products**:

   ```json
   GET /api/v1/products/deleted?pageNumber=1&pageSize=10
   ```

4. **Restore a Product**:

   ```json
   POST /api/v1/products/{productId}/restore
   ```

### Programmatic Usage

#### Soft Delete an Entity

```csharp
// In any command handler
await _repository.DeleteAsync(entityId, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
// DeletedAt and DeletedBy are automatically set
```

#### Restore an Entity

```csharp
await _repository.RestoreAsync(entityId, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

#### Query Deleted Entities

```csharp
var deletedProducts = await _repository.GetAllDeletedAsync(cancellationToken);
```

#### Check if Entity is Deleted

```csharp
var product = await _repository.GetDeletedByIdAsync(productId, cancellationToken);
if (product != null)
{
    // Product exists and is soft-deleted
}
```

## Extending to Other Entities

To add soft delete recovery to other entities (e.g., Users, Categories):

### 1. Create Restore Command

```bash
Features/{Entity}/Commands/Restore{Entity}/
├── Restore{Entity}Command.cs
├── Restore{Entity}CommandHandler.cs
└── Restore{Entity}CommandValidator.cs
```

### 2. Create GetDeleted Query

```bash
Features/{Entity}/Queries/GetDeleted{Entity}/
├── GetDeleted{Entity}Query.cs
├── GetDeleted{Entity}QueryHandler.cs
└── GetDeleted{Entity}QueryValidator.cs
```

### 3. Add Controller Endpoints

```csharp
[HttpGet("deleted")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<PaginatedResult<EntityDto>>>> GetDeletedEntities(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
{
    var query = new GetDeletedEntitiesQuery(pageNumber, pageSize);
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(new ApiResponse<PaginatedResult<EntityDto>>(result));
}

[HttpPost("{id}/restore")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<bool>>> RestoreEntity(
    Guid id, 
    CancellationToken cancellationToken)
{
    var command = new RestoreEntityCommand(id);
    var result = await _mediator.Send(command, cancellationToken);
    return Ok(new ApiResponse<bool>(result));
}
```

## Best Practices

### ✅ DO

- Use soft delete for business-critical entities (Products, Users, Orders)
- Restrict restore operations to Admin role
- Include deleted entities in audit logs
- Periodically archive old soft-deleted records

### ❌ DON'T

- Soft delete temporary or transient data (sessions, tokens)
- Expose deleted entities to regular users
- Forget to use `IgnoreQueryFilters()` when accessing deleted entities
- Restore entities without validation

## Security Considerations

1. **Authorization**: Only Admins can view and restore deleted entities
2. **Audit Trail**: All deletions track who deleted and when
3. **Query Filters**: Prevent accidental exposure of deleted data
4. **Validation**: Restore operations validate entity exists before restoration

## Performance Notes

- Global query filters are applied at SQL level (efficient)
- Deleted entities remain in tables (disk space consideration)
- Indexes on `IsDeleted` column may improve query performance
- Consider archiving strategy for old soft-deleted records

## Troubleshooting

### Issue: Can't find deleted entity

**Solution**: Use `GetDeletedByIdAsync()` or `IgnoreQueryFilters()`

### Issue: Entity not being soft-deleted

**Solution**: Ensure entity inherits from `BaseEntity` and query filter is configured

### Issue: DeletedAt/DeletedBy not populating

**Solution**: Verify `ICurrentUserService` is properly injected and SaveChangesAsync logic is correct

## Migration Commands

```bash
# Create migration
dotnet ef migrations add AddSoftDeleteMetadata --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi

# Apply migration
dotnet ef database update --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi

# Rollback migration (if needed)
dotnet ef database update PreviousMigrationName --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
```

## Summary

Soft delete with recovery is now fully implemented across the Enterprise Web API. All entities support:

- ✅ Automatic soft deletion with metadata tracking
- ✅ Global query filters to hide deleted entities
- ✅ Repository methods for accessing deleted entities
- ✅ CQRS commands for restore operations
- ✅ Admin-only endpoints for managing deleted entities
- ✅ Full audit trail of deletions and restorations

The implementation follows Clean Architecture principles and integrates seamlessly with the existing CQRS pattern.
