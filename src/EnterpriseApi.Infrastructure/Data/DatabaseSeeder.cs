using EnterpriseApi.Application.Common.Interfaces;
using EnterpriseApi.Domain.Entities;
using EnterpriseApi.Domain.Interfaces;
using EnterpriseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnterpriseApi.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed users if none exist
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Seeding default users...");

                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@enterpriseapi.com",
                    PasswordHash = passwordHasher.HashPassword("Admin@123"),
                    FirstName = "System",
                    LastName = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var regularUser = new User
                {
                    Username = "user",
                    Email = "user@enterpriseapi.com",
                    PasswordHash = passwordHasher.HashPassword("User@123"),
                    FirstName = "Regular",
                    LastName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var managerUser = new User
                {
                    Username = "manager",
                    Email = "manager@enterpriseapi.com",
                    PasswordHash = passwordHasher.HashPassword("Manager@123"),
                    FirstName = "Department",
                    LastName = "Manager",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddRangeAsync(adminUser, regularUser, managerUser);
                await context.SaveChangesAsync();

                // Get roles (should be seeded by migration)
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                var userRole = await context.Roles.FirstAsync(r => r.Name == "User");
                var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");

                // Assign roles to users
                var userRoles = new List<UserRole>
                {
                    new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id },
                    new UserRole { UserId = regularUser.Id, RoleId = userRole.Id },
                    new UserRole { UserId = managerUser.Id, RoleId = managerRole.Id }
                };

                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();

                logger.LogInformation("Default users seeded successfully");
                logger.LogInformation("Admin User - Username: admin, Password: Admin@123");
                logger.LogInformation("Regular User - Username: user, Password: User@123");
                logger.LogInformation("Manager User - Username: manager, Password: Manager@123");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
