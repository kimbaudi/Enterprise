using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;

namespace Enterprise.Integration.Tests.Infrastructure;

/// <summary>
/// Seeds test data into the in-memory database for integration tests
/// </summary>
public static class TestDataSeeder
{
    public static void SeedTestData(ApplicationDbContext context)
    {
        // Seed test users with roles
        var adminUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Username = "testadmin",
            Email = "testadmin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FirstName = "Test",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var regularUser = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Username = "testuser",
            Email = "testuser@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(adminUser, regularUser);

        // Seed roles
        var adminRole = new Role
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Admin",
            Description = "Administrator role",
            CreatedAt = DateTime.UtcNow
        };

        var userRole = new Role
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name = "User",
            Description = "User role",
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.AddRange(adminRole, userRole);

        // Assign roles to users
        var userRoles = new List<UserRole>
        {
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            },
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = regularUser.Id,
                RoleId = userRole.Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.UserRoles.AddRange(userRoles);

        // Seed test products
        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Test Product 1",
                Description = "Test Description 1",
                Price = 99.99m,
                Stock = 100,
                Category = "Electronics",
                SKU = "TEST-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Test Product 2",
                Description = "Test Description 2",
                Price = 149.99m,
                Stock = 50,
                Category = "Electronics",
                SKU = "TEST-002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Test Product 3",
                Description = "Test Description 3",
                Price = 199.99m,
                Stock = 25,
                Category = "Computers",
                SKU = "TEST-003",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
