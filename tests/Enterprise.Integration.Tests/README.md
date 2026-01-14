# Integration Tests

## Overview

The `Enterprise.Integration.Tests` project contains comprehensive integration tests for the Enterprise Web API. These tests verify the complete request-response pipeline, including middleware, authentication, validation, and database interactions using an in-memory database.

## Test Structure

```
tests/Enterprise.Integration.Tests/
├── Infrastructure/
│   ├── CustomWebApplicationFactory.cs   # Custom test host configuration
│   ├── TestDataSeeder.cs                # Seeds test data into in-memory DB
│   ├── IntegrationTestBase.cs           # Base class for all integration tests
│   └── AuthenticationHelper.cs          # Helper methods for JWT authentication
├── Features/
│   ├── Auth/
│   │   └── AuthenticationTests.cs       # Tests for auth endpoints
│   ├── Products/
│   │   └── ProductTests.cs              # Tests for product endpoints
│   └── Users/
│       └── UserTests.cs                 # Tests for user management endpoints
└── Enterprise.Integration.Tests.csproj
```

## Key Components

### CustomWebApplicationFactory

Configures the test host to use an in-memory database instead of SQL Server. This ensures tests:

- Run in isolation without affecting the production database
- Execute quickly without I/O overhead
- Can run in parallel without conflicts
- Reset to a clean state for each test class

### TestDataSeeder

Seeds the in-memory database with test data:

- **Users**: `testadmin` (Admin role) and `testuser` (User role)
- **Roles**: Admin and User
- **Products**: 3 sample products with different categories
- **Passwords**: Hashed using BCrypt for realistic authentication tests

### IntegrationTestBase

Base class providing common utilities:

- `GetAuthenticatedAdminClientAsync()` - Returns HttpClient with admin JWT token
- `GetAuthenticatedUserClientAsync()` - Returns HttpClient with user JWT token
- `GetJwtTokenAsync(username, password)` - Gets a JWT token for any user

### AuthenticationHelper

Static helper class for authentication operations:

- `GetJwtTokenAsync()` - Authenticates and retrieves JWT token
- `SetBearerToken()` - Adds bearer token to HttpClient headers
- `GetAuthenticatedClientAsync()` - Creates authenticated HttpClient in one call

## Test Categories

### Authentication Tests (12 tests)

Tests for `/api/v1/auth` endpoints:

- ✅ Login with valid/invalid credentials
- ✅ Registration with valid/invalid data
- ✅ JWT token generation and validation
- ✅ Unauthorized access without tokens
- ✅ Token uniqueness across logins

### Product Tests (13 tests)

Tests for `/api/v1/products` endpoints:

- ✅ CRUD operations (Create, Read, Update, Delete)
- ✅ Pagination and filtering
- ✅ Search functionality
- ✅ Authorization checks
- ✅ Validation error handling

### User Tests (14 tests)

Tests for `/api/v1/users` endpoints:

- ✅ User management CRUD operations
- ✅ Role-based access control (Admin vs User)
- ✅ Pagination and search
- ✅ Active/inactive user filtering

## Running the Tests

### Run All Integration Tests

```bash
dotnet test tests/Enterprise.Integration.Tests/Enterprise.Integration.Tests.csproj
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~AuthenticationTests"
dotnet test --filter "FullyQualifiedName~ProductTests"
dotnet test --filter "FullyQualifiedName~UserTests"
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~Login_WithValidCredentials_ReturnsTokenAndUserInfo"
```

### Run from Solution Root

```bash
cd c:/Users/Paul/Desktop/Enterprise
dotnet test
```

### Run with Detailed Output

```bash
dotnet test --verbosity detailed
```

## Test Data

### Seeded Users

| Username   | Password     | Role  | User ID                              |
|------------|--------------|-------|--------------------------------------|
| testadmin  | Admin@123    | Admin | 11111111-1111-1111-1111-111111111111 |
| testuser   | User@123     | User  | 22222222-2222-2222-2222-222222222222 |

### Seeded Products

| Name           | Price   | Stock | Category    | Product ID                           |
|----------------|---------|-------|-------------|--------------------------------------|
| Test Product 1 | $99.99  | 100   | Electronics | 33333333-3333-3333-3333-333333333333 |
| Test Product 2 | $149.99 | 50    | Electronics | 44444444-4444-4444-4444-444444444444 |
| Test Product 3 | $199.99 | 25    | Computers   | 55555555-5555-5555-5555-555555555555 |

## Key Patterns

### Testing Authenticated Endpoints

```csharp
[Fact]
public async Task GetProducts_WithValidToken_ReturnsProductList()
{
    // Arrange
    var client = await GetAuthenticatedAdminClientAsync();

    // Act
    var response = await client.GetAsync("/api/v1/products");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Testing Authorization

```csharp
[Fact]
public async Task GetUsers_WithRegularUser_ReturnsForbidden()
{
    // Arrange
    var client = await GetAuthenticatedUserClientAsync();

    // Act
    var response = await client.GetAsync("/api/v1/users");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Testing Validation

```csharp
[Fact]
public async Task CreateProduct_WithInvalidData_ReturnsBadRequest()
{
    // Arrange
    var client = await GetAuthenticatedAdminClientAsync();
    var invalidProduct = new { Name = "", Price = -10m };

    // Act
    var response = await client.PostAsJsonAsync("/api/v1/products", invalidProduct);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

## Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="Bogus" Version="35.6.1" />
```

## Best Practices

1. **Isolation**: Each test class gets a fresh in-memory database via `IClassFixture<CustomWebApplicationFactory>`
2. **Arrange-Act-Assert**: All tests follow the AAA pattern for clarity
3. **FluentAssertions**: Use readable assertions like `.Should().Be()` instead of `Assert.Equal()`
4. **Realistic Scenarios**: Tests use actual HTTP requests through the full middleware pipeline
5. **Named Tests**: Test names follow the pattern `MethodName_Scenario_ExpectedResult`

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:

- No external dependencies (uses in-memory database)
- Fast execution (typically <10 seconds for all tests)
- Deterministic results (no flaky tests)
- Clear failure messages with FluentAssertions

## Extending the Tests

To add tests for new features:

1. Create a new test class in `Features/{FeatureName}/`
2. Inherit from `IntegrationTestBase`
3. Use the authentication helpers for secured endpoints
4. Follow existing naming and pattern conventions
5. Add test data to `TestDataSeeder` if needed

Example:

```csharp
public class OrderTests : IntegrationTestBase
{
    public OrderTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreated()
    {
        var client = await GetAuthenticatedUserClientAsync();
        // ... test implementation
    }
}
```

## Coverage

Current test coverage for integration tests:

- **Authentication**: 12 test cases covering login, registration, and token validation
- **Products**: 13 test cases covering full CRUD, pagination, filtering, and validation
- **Users**: 14 test cases covering user management and role-based access control

**Total: 39 integration tests**

## Troubleshooting

### Tests Fail with "Program is inaccessible"

Solution: Ensure `Program.cs` in WebApi project has `public partial class Program { }` at the end.

### Authentication Tests Fail

Solution: Check that `TestDataSeeder` is properly seeding users with correct passwords.

### Tests Run Slowly

Solution: Ensure using in-memory database, not connecting to SQL Server. Check `CustomWebApplicationFactory` configuration.

### Database Conflicts Between Tests

Solution: Each test class should use `IClassFixture<CustomWebApplicationFactory>` to get isolated database instances.

## Related Documentation

- [CQRS Architecture](../docs/CQRS-ARCHITECTURE.md)
- [Authentication Guide](../docs/AUTHENTICATION.md)
- [Unit Tests](../tests/Enterprise.Application.Tests/README.md)
