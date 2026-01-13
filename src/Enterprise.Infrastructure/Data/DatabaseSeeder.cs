using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.Data;

public static class DatabaseSeeder
{
    private static readonly Random _random = new Random();
    private static readonly string[] _categories = { "Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys", "Food", "Beauty", "Automotive", "Health" };
    private static readonly string[] _adjectives = { "Premium", "Deluxe", "Professional", "Ultra", "Advanced", "Classic", "Modern", "Vintage", "Eco-Friendly", "Smart" };
    private static readonly string[] _productTypes = { "Widget", "Gadget", "Tool", "Device", "Accessory", "Kit", "System", "Solution", "Bundle", "Set" };
    private static readonly string[] _firstNames = { "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Jennifer", "William", "Lisa", "James", "Mary", "Christopher", "Patricia", "Daniel", "Linda" };
    private static readonly string[] _lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Wilson", "Anderson", "Thomas", "Taylor" };

    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider, int productCount = 100, int userCount = 10)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed roles first (required for users)
            var roles = await SeedRolesAsync(context, logger);

            // Seed default admin users
            await SeedDefaultUsersAsync(context, passwordHasher, logger, roles);

            // Seed large amount of users
            await SeedBulkUsersAsync(context, passwordHasher, logger, userCount, roles);

            // Seed large amount of products
            await SeedBulkProductsAsync(context, logger, productCount);

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task<Dictionary<string, Role>> SeedRolesAsync(ApplicationDbContext context, ILogger logger)
    {
        var roleDict = new Dictionary<string, Role>();

        if (!await context.Roles.AnyAsync())
        {
            logger.LogInformation("Roles already seeded by migration");
        }

        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var userRole = await context.Roles.FirstAsync(r => r.Name == "User");
        var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");

        roleDict["Admin"] = adminRole;
        roleDict["User"] = userRole;
        roleDict["Manager"] = managerRole;

        return roleDict;
    }

    private static async Task SeedDefaultUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        Dictionary<string, Role> roles)
    {
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Default users already exist, skipping...");
            return;
        }

        logger.LogInformation("Seeding default users...");

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@enterprise.com",
            PasswordHash = passwordHasher.HashPassword("Admin@123"),
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var regularUser = new User
        {
            Username = "user",
            Email = "user@enterprise.com",
            PasswordHash = passwordHasher.HashPassword("User@123"),
            FirstName = "Regular",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var managerUser = new User
        {
            Username = "manager",
            Email = "manager@enterprise.com",
            PasswordHash = passwordHasher.HashPassword("Manager@123"),
            FirstName = "Department",
            LastName = "Manager",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(adminUser, regularUser, managerUser);
        await context.SaveChangesAsync();

        // Assign roles to users
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = adminUser.Id, RoleId = roles["Admin"].Id },
            new UserRole { UserId = regularUser.Id, RoleId = roles["User"].Id },
            new UserRole { UserId = managerUser.Id, RoleId = roles["Manager"].Id }
        };

        await context.UserRoles.AddRangeAsync(userRoles);
        await context.SaveChangesAsync();

        logger.LogInformation("Default users seeded successfully");
        logger.LogInformation("Admin User - Username: admin, Password: Admin@123");
        logger.LogInformation("Regular User - Username: user, Password: User@123");
        logger.LogInformation("Manager User - Username: manager, Password: Manager@123");
    }

    private static async Task SeedBulkUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        int count,
        Dictionary<string, Role> roles)
    {
        var existingCount = await context.Users.CountAsync();
        if (existingCount >= count)
        {
            logger.LogInformation($"Database already has {existingCount} users, skipping bulk user seeding...");
            return;
        }

        logger.LogInformation($"Seeding {count} users in bulk...");
        var startTime = DateTime.UtcNow;

        var batchSize = 1000;
        var totalBatches = (int)Math.Ceiling((double)count / batchSize);

        for (int batch = 0; batch < totalBatches; batch++)
        {
            var currentBatchSize = Math.Min(batchSize, count - (batch * batchSize));
            var users = new List<User>(currentBatchSize);
            var userRolesList = new List<UserRole>(currentBatchSize);

            for (int i = 0; i < currentBatchSize; i++)
            {
                var globalIndex = batch * batchSize + i;
                var firstName = _firstNames[_random.Next(_firstNames.Length)];
                var lastName = _lastNames[_random.Next(_lastNames.Length)];

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"user{globalIndex + 1}",
                    Email = $"user{globalIndex + 1}@example.com",
                    PasswordHash = passwordHasher.HashPassword("Password@123"),
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = _random.Next(100) < 95, // 95% active
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365))
                };

                users.Add(user);

                // Assign random role (70% User, 20% Manager, 10% Admin)
                var roleChance = _random.Next(100);
                var assignedRole = roleChance < 70 ? roles["User"] :
                                  roleChance < 90 ? roles["Manager"] :
                                  roles["Admin"];

                userRolesList.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = assignedRole.Id
                });
            }

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            await context.UserRoles.AddRangeAsync(userRolesList);
            await context.SaveChangesAsync();

            logger.LogInformation($"Seeded batch {batch + 1}/{totalBatches} ({users.Count} users)");
        }

        var elapsed = DateTime.UtcNow - startTime;
        logger.LogInformation($"Bulk user seeding completed in {elapsed.TotalSeconds:F2} seconds");
    }

    private static async Task SeedBulkProductsAsync(ApplicationDbContext context, ILogger logger, int count)
    {
        var existingCount = await context.Products.CountAsync();
        if (existingCount >= count)
        {
            logger.LogInformation($"Database already has {existingCount} products, skipping bulk product seeding...");
            return;
        }

        logger.LogInformation($"Seeding {count} products in bulk...");
        var startTime = DateTime.UtcNow;

        var batchSize = 1000;
        var totalBatches = (int)Math.Ceiling((double)count / batchSize);

        for (int batch = 0; batch < totalBatches; batch++)
        {
            var currentBatchSize = Math.Min(batchSize, count - (batch * batchSize));
            var products = new List<Product>(currentBatchSize);

            for (int i = 0; i < currentBatchSize; i++)
            {
                var globalIndex = batch * batchSize + i;
                var category = _categories[_random.Next(_categories.Length)];
                var adjective = _adjectives[_random.Next(_adjectives.Length)];
                var productType = _productTypes[_random.Next(_productTypes.Length)];

                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = $"{adjective} {productType} {globalIndex + 1}",
                    Description = $"High-quality {adjective.ToLower()} {productType.ToLower()} in {category} category. Perfect for all your needs.",
                    Price = Math.Round((decimal)(_random.NextDouble() * 999 + 1), 2),
                    Stock = _random.Next(0, 1000),
                    Category = category,
                    SKU = $"SKU-{category.ToUpper().Substring(0, 3)}-{globalIndex + 1:D6}",
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(730)),
                    UpdatedAt = _random.Next(100) < 30 ? DateTime.UtcNow.AddDays(-_random.Next(30)) : null,
                    IsDeleted = false
                };

                products.Add(product);
            }

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            logger.LogInformation($"Seeded batch {batch + 1}/{totalBatches} ({products.Count} products)");
        }

        var elapsed = DateTime.UtcNow - startTime;
        logger.LogInformation($"Bulk product seeding completed in {elapsed.TotalSeconds:F2} seconds");
    }
}
