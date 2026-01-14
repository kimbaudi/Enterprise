# Soft Delete Flow Diagram

## Soft Delete Flow

```
┌──────────────┐
│   Client     │
│  (Admin)     │
└──────┬───────┘
       │ DELETE /api/v1/products/{id}
       │
       ▼
┌──────────────────────────────┐
│  ProductsController          │
│  DeleteProduct(id)           │
└──────┬───────────────────────┘
       │ Send Command
       ▼
┌──────────────────────────────┐
│  MediatR Pipeline            │
│  ├─ LoggingBehavior          │
│  ├─ ValidationBehavior       │
│  └─ PerformanceBehavior      │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  DeleteProductCommandHandler │
│  1. Get product by ID        │
│  2. Set IsDeleted = true     │
│  3. Call SaveChangesAsync()  │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  ApplicationDbContext        │
│  SaveChangesAsync Override   │
│  ├─ Detect IsDeleted change  │
│  ├─ Set DeletedAt = UtcNow   │
│  └─ Set DeletedBy = Username │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Database (SQL Server)       │
│  UPDATE Products SET         │
│    IsDeleted = 1,            │
│    DeletedAt = '2026-01-14', │
│    DeletedBy = 'admin'       │
│  WHERE Id = {id}             │
└──────────────────────────────┘
```

## Restore Flow

```
┌──────────────┐
│   Client     │
│  (Admin)     │
└──────┬───────┘
       │ POST /api/v1/products/{id}/restore
       │
       ▼
┌──────────────────────────────┐
│  ProductsController          │
│  RestoreProduct(id)          │
└──────┬───────────────────────┘
       │ Send Command
       ▼
┌──────────────────────────────┐
│  MediatR Pipeline            │
│  ├─ LoggingBehavior          │
│  ├─ ValidationBehavior       │
│  └─ PerformanceBehavior      │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  RestoreProductCommandHandler│
│  1. GetDeletedByIdAsync(id)  │
│     (uses IgnoreQueryFilters)│
│  2. Set IsDeleted = false    │
│  3. Clear DeletedAt/DeletedBy│
│  4. Call SaveChangesAsync()  │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Database (SQL Server)       │
│  UPDATE Products SET         │
│    IsDeleted = 0,            │
│    DeletedAt = NULL,         │
│    DeletedBy = NULL          │
│  WHERE Id = {id}             │
└──────────────────────────────┘
```

## Query Flow (Regular vs Deleted)

### Regular Query (Active Products)

```
SELECT * FROM Products
WHERE Category = 'Electronics'
  AND IsDeleted = 0  ← Global Query Filter (Auto-applied)
```

### Deleted Products Query

```
┌──────────────┐
│   Client     │
│  (Admin)     │
└──────┬───────┘
       │ GET /api/v1/products/deleted
       │
       ▼
┌──────────────────────────────┐
│  GetDeletedProductsQuery     │
│  Handler calls:              │
│  GetDeletedPagedAsync()      │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Repository.GetDeletedPaged  │
│  Uses IgnoreQueryFilters()   │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Database (SQL Server)       │
│  SELECT * FROM Products      │
│  WHERE IsDeleted = 1         │
│  ORDER BY DeletedAt DESC     │
│  OFFSET {skip} ROWS          │
│  FETCH NEXT {take} ROWS ONLY │
└──────────────────────────────┘
```

## Data Lifecycle

```
┌─────────────┐
│   Created   │ IsDeleted = false
│             │ DeletedAt = null
│             │ DeletedBy = null
└──────┬──────┘
       │
       │ DELETE command
       ▼
┌─────────────┐
│ Soft Deleted│ IsDeleted = true
│             │ DeletedAt = timestamp
│             │ DeletedBy = username
└──────┬──────┘
       │
       │ GET query (regular)
       │ ❌ Not visible
       │
       │ GET deleted query
       │ ✅ Visible to Admin
       │
       │ RESTORE command
       ▼
┌─────────────┐
│  Restored   │ IsDeleted = false
│  (Active)   │ DeletedAt = null
│             │ DeletedBy = null
└─────────────┘
```

## Security & Authorization Flow

```
┌──────────────────────────────────────────┐
│          Incoming Request                │
└────────────────┬─────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────┐
│     JWT Authentication Middleware        │
│     Validates Bearer Token               │
└────────────────┬─────────────────────────┘
                 │ Token Valid?
                 ▼
         ┌───────────────┐
         │ Authorization │
         │ [Roles="Admin"]│
         └───────┬───────┘
                 │
     ┌───────────┴───────────┐
     │                       │
     ▼                       ▼
┌─────────┐            ┌──────────┐
│ Admin   │            │ Other    │
│ ✅ Allow │            │ ❌ Deny  │
└─────────┘            └──────────┘
     │                       │
     ▼                       ▼
┌──────────────┐      ┌──────────────┐
│ Execute      │      │ Return       │
│ Command      │      │ 403 Forbidden│
└──────────────┘      └──────────────┘
```

## Repository Method Flow

### DeleteAsync (Soft Delete)

```
DeleteAsync(id)
    │
    ├─► GetByIdAsync(id)
    │       │
    │       └─► SELECT * FROM Products 
    │           WHERE Id = {id} AND IsDeleted = 0
    │
    ├─► entity.IsDeleted = true
    │
    └─► UpdateAsync(entity)
            │
            └─► DbContext.Update(entity)
                    │
                    └─► SaveChangesAsync()
                            │
                            └─► Triggers automatic metadata population
```

### RestoreAsync

```
RestoreAsync(id)
    │
    ├─► GetDeletedByIdAsync(id)
    │       │
    │       └─► SELECT * FROM Products 
    │           WHERE Id = {id} AND IsDeleted = 1
    │           (Uses IgnoreQueryFilters)
    │
    ├─► entity.IsDeleted = false
    ├─► entity.DeletedAt = null
    ├─► entity.DeletedBy = null
    │
    └─► UpdateAsync(entity)
            │
            └─► DbContext.Update(entity)
                    │
                    └─► SaveChangesAsync()
```

---

**Note**: All flows are logged via LoggingBehavior and validated via ValidationBehavior in the MediatR pipeline.
