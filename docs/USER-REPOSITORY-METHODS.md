# User Repository Methods - Reference Guide

## Overview

Extended the `IUserRepository` with common and useful query methods for user management, role-based access, and 2FA administration.

## Added Methods

### 📧 Email Lookups

```csharp
Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default)
```

**Purpose**: Get user by email with roles loaded (consistent with `GetByUsernameWithRolesAsync`)  
**Use Case**: Authentication flows that use email instead of username

---

### 👥 Role-Based Queries

```csharp
Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName, CancellationToken cancellationToken = default)
Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
```

**Purpose**: Get all users assigned to a specific role  
**Use Cases**:

- Admin dashboards showing users by role
- Bulk operations on role members
- Role management features

**Example**:

```csharp
var admins = await _userRepository.GetUsersByRoleAsync("Admin", cancellationToken);
var managers = await _userRepository.GetUsersByRoleIdAsync(managerRoleId, cancellationToken);
```

---

### ✅ Status Queries

```csharp
Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default)
```

**Purpose**: Filter users by account status  
**Use Cases**:

- Admin dashboards showing active users
- Security monitoring for locked accounts
- User management reports

**Example**:

```csharp
var activeUsers = await _userRepository.GetActiveUsersAsync(cancellationToken);
var lockedUsers = await _userRepository.GetLockedOutUsersAsync(cancellationToken);
```

---

### 🔐 Two-Factor Authentication Queries

```csharp
Task<IEnumerable<User>> GetUsersWithTwoFactorEnabledAsync(CancellationToken cancellationToken = default)
Task<int> CountUsersWithTwoFactorAsync(CancellationToken cancellationToken = default)
```

**Purpose**: Monitor 2FA adoption across users  
**Use Cases**:

- Security compliance reporting
- 2FA adoption metrics
- Admin dashboards showing 2FA statistics

**Example**:

```csharp
var twoFactorUsers = await _userRepository.GetUsersWithTwoFactorEnabledAsync(cancellationToken);
var twoFactorCount = await _userRepository.CountUsersWithTwoFactorAsync(cancellationToken);
var adoptionRate = (double)twoFactorCount / totalUsers * 100;
```

---

### 🔍 Search with Filters

```csharp
Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(
    string? searchTerm,
    bool? isActive,
    bool? twoFactorEnabled,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
```

**Purpose**: Powerful search and filtering for admin user management  
**Parameters**:

- `searchTerm`: Searches username, email, first name, last name (case-insensitive)
- `isActive`: Filter by active/inactive status (nullable)
- `twoFactorEnabled`: Filter by 2FA status (nullable)
- `pageNumber`: Page number (1-based)
- `pageSize`: Items per page

**Returns**: Tuple with users list and total count for pagination

**Example**:

```csharp
// Search for active users with 2FA enabled
var (users, totalCount) = await _userRepository.SearchUsersAsync(
    searchTerm: "john",
    isActive: true,
    twoFactorEnabled: true,
    pageNumber: 1,
    pageSize: 20,
    cancellationToken);

// Get all inactive users (no search term)
var (inactiveUsers, count) = await _userRepository.SearchUsersAsync(
    searchTerm: null,
    isActive: false,
    twoFactorEnabled: null,
    pageNumber: 1,
    pageSize: 50,
    cancellationToken);
```

---

### 📦 Batch Operations

```csharp
Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
```

**Purpose**: Efficiently fetch multiple users by their IDs  
**Use Cases**:

- Bulk user operations
- Displaying user lists from ID collections
- Resolving user references

**Example**:

```csharp
var userIds = new[] { userId1, userId2, userId3 };
var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
```

---

### 📅 Recent Users

```csharp
Task<IEnumerable<User>> GetRecentlyCreatedUsersAsync(int count, CancellationToken cancellationToken = default)
```

**Purpose**: Get most recently registered users  
**Use Cases**:

- Admin dashboard "Recent Users" widget
- Monitoring new registrations
- User growth analytics

**Example**:

```csharp
// Get last 10 registered users
var recentUsers = await _userRepository.GetRecentlyCreatedUsersAsync(10, cancellationToken);
```

---

## Command/Query Examples

### Create Admin Dashboard Query

```csharp
// Application/Features/Users/Queries/GetUserStatistics/GetUserStatisticsQuery.cs
public record GetUserStatisticsQuery : IRequest<UserStatisticsResponse>;

// Handler
public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, UserStatisticsResponse>
{
    private readonly IUserRepository _userRepository;

    public async Task<UserStatisticsResponse> Handle(
        GetUserStatisticsQuery request, 
        CancellationToken cancellationToken)
    {
        var totalUsers = await _userRepository.CountAsync(cancellationToken: cancellationToken);
        var activeUsers = (await _userRepository.GetActiveUsersAsync(cancellationToken)).Count();
        var lockedUsers = (await _userRepository.GetLockedOutUsersAsync(cancellationToken)).Count();
        var twoFactorCount = await _userRepository.CountUsersWithTwoFactorAsync(cancellationToken);
        var recentUsers = await _userRepository.GetRecentlyCreatedUsersAsync(5, cancellationToken);

        return new UserStatisticsResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            LockedOutUsers = lockedUsers,
            TwoFactorEnabled = twoFactorCount,
            TwoFactorAdoptionRate = (double)twoFactorCount / totalUsers * 100,
            RecentRegistrations = recentUsers.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            }).ToList()
        };
    }
}
```

### Search Users Command

```csharp
// Application/Features/Users/Queries/SearchUsers/SearchUsersQuery.cs
public record SearchUsersQuery(
    string? SearchTerm,
    bool? IsActive,
    bool? TwoFactorEnabled,
    int PageNumber,
    int PageSize) : IRequest<PaginatedResult<UserDto>>;

// Handler
public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, PaginatedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public async Task<PaginatedResult<UserDto>> Handle(
        SearchUsersQuery request, 
        CancellationToken cancellationToken)
    {
        var (users, totalCount) = await _userRepository.SearchUsersAsync(
            request.SearchTerm,
            request.IsActive,
            request.TwoFactorEnabled,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var userDtos = _mapper.Map<List<UserDto>>(users);

        return new PaginatedResult<UserDto>(
            userDtos,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }
}
```

### Get Users by Role Query

```csharp
// Application/Features/Users/Queries/GetUsersByRole/GetUsersByRoleQuery.cs
public record GetUsersByRoleQuery(string RoleName) : IRequest<List<UserDto>>;

// Handler
public class GetUsersByRoleQueryHandler : IRequestHandler<GetUsersByRoleQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public async Task<List<UserDto>> Handle(
        GetUsersByRoleQuery request, 
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetUsersByRoleAsync(
            request.RoleName, 
            cancellationToken);

        return _mapper.Map<List<UserDto>>(users.ToList());
    }
}
```

---

## Controller Examples

### Admin User Management Controller

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserDto>>>> SearchUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isActive,
        [FromQuery] bool? twoFactorEnabled,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken)
    {
        var query = new SearchUsersQuery(
            searchTerm, 
            isActive, 
            twoFactorEnabled, 
            pageNumber, 
            pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<UserDto>>(result));
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<UserStatisticsResponse>>> GetStatistics(
        CancellationToken cancellationToken)
    {
        var query = new GetUserStatisticsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<UserStatisticsResponse>(result));
    }

    [HttpGet("by-role/{roleName}")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByRole(
        string roleName,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersByRoleQuery(roleName);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<List<UserDto>>(result));
    }
}
```

---

## Benefits

✅ **Consistency**: Methods follow established naming patterns  
✅ **Performance**: Optimized queries with proper includes and tracking  
✅ **Flexibility**: Optional parameters for filtering  
✅ **Pagination**: Built-in support for large result sets  
✅ **2FA Support**: Specialized queries for 2FA management  
✅ **Admin Features**: Ready for admin dashboard implementation  

## Performance Notes

- All queries use `AsNoTracking()` for read-only scenarios
- `SearchUsersAsync` includes indexes on Username, Email, FirstName, LastName for optimal performance
- Batch operations (`GetByIdsAsync`) use `Contains` for efficient IN queries
- Role queries include eager loading to avoid N+1 problems

## Testing Examples

```csharp
[Fact]
public async Task SearchUsersAsync_WithSearchTerm_ReturnsMatchingUsers()
{
    // Arrange
    var users = new List<User>
    {
        CreateUser("john.doe", "john@example.com"),
        CreateUser("jane.smith", "jane@example.com"),
        CreateUser("bob.jones", "bob@example.com")
    };
    _context.Users.AddRange(users);
    await _context.SaveChangesAsync();

    // Act
    var (results, totalCount) = await _repository.SearchUsersAsync(
        searchTerm: "john",
        isActive: null,
        twoFactorEnabled: null,
        pageNumber: 1,
        pageSize: 10,
        cancellationToken: default);

    // Assert
    results.Should().HaveCount(1);
    results.First().Username.Should().Be("john.doe");
    totalCount.Should().Be(1);
}

[Fact]
public async Task GetUsersByRoleAsync_ReturnsUsersWithSpecificRole()
{
    // Arrange
    var adminRole = CreateRole("Admin");
    var userRole = CreateRole("User");
    var admin1 = CreateUser("admin1", "admin1@example.com");
    var admin2 = CreateUser("admin2", "admin2@example.com");
    var user1 = CreateUser("user1", "user1@example.com");
    
    admin1.UserRoles.Add(new UserRole { Role = adminRole });
    admin2.UserRoles.Add(new UserRole { Role = adminRole });
    user1.UserRoles.Add(new UserRole { Role = userRole });
    
    _context.Users.AddRange(admin1, admin2, user1);
    await _context.SaveChangesAsync();

    // Act
    var admins = await _repository.GetUsersByRoleAsync("Admin", default);

    // Assert
    admins.Should().HaveCount(2);
    admins.Select(u => u.Username).Should().Contain(new[] { "admin1", "admin2" });
}
```

---

## Summary

Added **11 new methods** to `IUserRepository` covering:

- 📧 Email lookups with roles
- 👥 Role-based filtering (2 methods)
- ✅ Status queries (2 methods)
- 🔐 2FA monitoring (2 methods)
- 🔍 Advanced search with pagination
- 📦 Batch operations
- 📅 Recent user tracking

All methods follow Clean Architecture patterns, CQRS principles, and are ready for production use!
