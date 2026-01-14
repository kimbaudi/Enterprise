# Soft Delete Recovery - Quick Reference

## Quick Commands

### API Endpoints

```bash
# Soft delete a product
DELETE /api/v1/products/{id}
Role: Admin

# View deleted products
GET /api/v1/products/deleted?pageNumber=1&pageSize=10
Role: Admin

# Restore a product
POST /api/v1/products/{id}/restore
Role: Admin
```

## Repository Methods

```csharp
// Soft delete
await _repository.DeleteAsync(id, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Restore
await _repository.RestoreAsync(id, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Get deleted entity
var deleted = await _repository.GetDeletedByIdAsync(id, cancellationToken);

// Get all deleted
var allDeleted = await _repository.GetAllDeletedAsync(cancellationToken);

// Get deleted paginated
var (items, total) = await _repository.GetDeletedPagedAsync(
    pageNumber, pageSize, orderBy, cancellationToken);
```

## Key Properties

```csharp
public abstract class BaseEntity : ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }  // Auto-populated on delete
    public string? DeletedBy { get; set; }    // Auto-populated on delete
}
```

## How It Works

1. **Delete**: Sets `IsDeleted = true`, auto-populates `DeletedAt` and `DeletedBy`
2. **Query**: Global filters automatically exclude deleted entities
3. **Restore**: Resets `IsDeleted = false`, clears `DeletedAt` and `DeletedBy`
4. **Access Deleted**: Use `.IgnoreQueryFilters()` or `GetDeletedByIdAsync()`

## CQRS Pattern

### Commands

- **DeleteProduct**: Soft deletes product (existing)
- **RestoreProduct**: Restores soft-deleted product (new)

### Queries

- **GetProductsPaginated**: Returns only active products
- **GetDeletedProducts**: Returns only deleted products (new)

## Security

- ✅ Only Admin can view deleted entities
- ✅ Only Admin can restore entities
- ✅ All deletions tracked with timestamp and username
- ✅ Global query filters protect against accidental access

## Testing with Swagger

```json
// 1. Login as Admin
POST /api/v1/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}

// 2. Delete product
DELETE /api/v1/products/{id}

// 3. View deleted
GET /api/v1/products/deleted

// 4. Restore
POST /api/v1/products/{id}/restore
```

## Database Migration

```bash
# Migration already applied: AddSoftDeleteMetadata
# Adds: DeletedAt (datetime2), DeletedBy (nvarchar)
# To all entity tables
```

## Extending to Other Entities

1. Create `Restore{Entity}Command` and handler
2. Create `GetDeleted{Entity}Query` and handler
3. Add controller endpoints (GET deleted, POST restore)
4. Entity already has soft delete support (inherits BaseEntity)

## Common Patterns

```csharp
// Check if exists in deleted
var exists = await _repository.GetDeletedByIdAsync(id, ct) != null;

// Restore multiple entities
await _repository.RestoreRangeAsync(entities, ct);
await _unitOfWork.SaveChangesAsync(ct);

// Query with IgnoreQueryFilters
var allProducts = await _context.Products
    .IgnoreQueryFilters()
    .Where(p => p.IsDeleted)
    .ToListAsync(ct);
```

## See Full Documentation

[SOFT-DELETE-RECOVERY.md](SOFT-DELETE-RECOVERY.md)
