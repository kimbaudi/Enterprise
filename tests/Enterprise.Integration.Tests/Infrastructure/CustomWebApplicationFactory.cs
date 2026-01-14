using Enterprise.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Integration.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that configures the test application
/// to use an in-memory database instead of SQL Server
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Use instance field instead of static to ensure each test class gets its own database
    private readonly string _databaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configure Kestrel to use synchronous IO
            services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // Remove the existing ApplicationDbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add ApplicationDbContext with in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Build service provider and seed test data
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();

            // Seed initial test data only if not already seeded
            if (!db.Users.Any())
            {
                TestDataSeeder.SeedTestData(db);
            }
        });

        builder.UseEnvironment("Testing");
    }
}
