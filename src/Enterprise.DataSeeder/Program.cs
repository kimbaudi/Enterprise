using System.CommandLine;
using Enterprise.Application;
using Enterprise.Infrastructure;
using Enterprise.DataSeeder.Data;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/seeder-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var rootCommand = new RootCommand("Enterprise Web API Database Seeder - CLI tool for database operations");

    // Seed command
    var seedCommand = new Command("seed", "Seed the database with sample data");
    var productsOption = new Option<int>(
        name: "--products",
        description: "Number of products to seed",
        getDefaultValue: () => 100);
    var usersOption = new Option<int>(
        name: "--users",
        description: "Number of users to seed",
        getDefaultValue: () => 10);
    var forceOption = new Option<bool>(
        name: "--force",
        description: "Force seeding even if data already exists",
        getDefaultValue: () => false);

    seedCommand.AddOption(productsOption);
    seedCommand.AddOption(usersOption);
    seedCommand.AddOption(forceOption);

    seedCommand.SetHandler(SeedDatabaseAsync, productsOption, usersOption, forceOption);

    // Clear command
    var clearCommand = new Command("clear", "Clear all data from the database");
    var confirmOption = new Option<bool>(
        name: "--confirm",
        description: "Confirm deletion of all data",
        getDefaultValue: () => false);
    clearCommand.AddOption(confirmOption);

    clearCommand.SetHandler(async (bool confirm) =>
    {
        await ClearDatabaseAsync(confirm);
    }, confirmOption);

    // Migrate command
    var migrateCommand = new Command("migrate", "Apply pending database migrations");
    migrateCommand.SetHandler(async () =>
    {
        await MigrateDatabaseAsync();
    });

    // Reset command
    var resetCommand = new Command("reset", "Drop database, recreate, and seed with fresh data");
    var resetProductsOption = new Option<int>(
        name: "--products",
        description: "Number of products to seed",
        getDefaultValue: () => 100);
    var resetUsersOption = new Option<int>(
        name: "--users",
        description: "Number of users to seed",
        getDefaultValue: () => 10);

    resetCommand.AddOption(resetProductsOption);
    resetCommand.AddOption(resetUsersOption);

    resetCommand.SetHandler(async (int products, int users) =>
    {
        await ResetDatabaseAsync(products, users);
    }, resetProductsOption, resetUsersOption);

    rootCommand.AddCommand(seedCommand);
    rootCommand.AddCommand(clearCommand);
    rootCommand.AddCommand(migrateCommand);
    rootCommand.AddCommand(resetCommand);

    return await rootCommand.InvokeAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred in the seeder application");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static IHost BuildHost()
{
    var builder = Host.CreateApplicationBuilder();

    builder.Services.AddSerilog();
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    return builder.Build();
}

static async Task SeedDatabaseAsync(int productCount, int userCount, bool force)
{
    Log.Information("Starting database seeding...");
    Log.Information("Products: {ProductCount}, Users: {UserCount}, Force: {Force}", productCount, userCount, force);

    var host = BuildHost();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Seeding database with {ProductCount} products and {UserCount} users...", productCount, userCount);

        if (force)
        {
            logger.LogWarning("Force flag enabled - will seed even if data exists");
        }

        await DatabaseSeeder.SeedDatabaseAsync(host.Services, productCount, userCount);

        logger.LogInformation("Data seeding completed successfully!");
        Log.Information("✓ Database seeding completed!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during data seeding");
        Log.Fatal(ex, "✗ Data seeding failed");
        throw;
    }
}

static async Task ClearDatabaseAsync(bool confirm)
{
    if (!confirm)
    {
        Log.Warning("Clear operation requires --confirm flag to proceed");
        Log.Information("Usage: dotnet run clear --confirm");
        return;
    }

    Log.Warning("Starting database clear operation...");

    var host = BuildHost();
    var context = host.Services.GetRequiredService<ApplicationDbContext>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogWarning("Clearing all data from database...");

        // Clear tables in correct order (respecting foreign keys)
        await context.Database.ExecuteSqlRawAsync("DELETE FROM UserRoles");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM RefreshTokens");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Users");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Products");
        // Don't delete Roles as they're needed for the application

        logger.LogInformation("All data cleared successfully!");
        Log.Information("✓ Database cleared!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while clearing the database");
        Log.Fatal(ex, "✗ Database clear failed");
        throw;
    }
}

static async Task MigrateDatabaseAsync()
{
    Log.Information("Starting database migration...");

    var host = BuildHost();
    var context = host.Services.GetRequiredService<ApplicationDbContext>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying pending migrations...");

        await context.Database.MigrateAsync();

        logger.LogInformation("Database migration completed successfully!");
        Log.Information("✓ Database migration completed!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration");
        Log.Fatal(ex, "✗ Database migration failed");
        throw;
    }
}

static async Task ResetDatabaseAsync(int productCount, int userCount)
{
    Log.Warning("Starting database reset operation...");
    Log.Warning("This will DROP the entire database and recreate it!");

    var host = BuildHost();
    var context = host.Services.GetRequiredService<ApplicationDbContext>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogWarning("Dropping database...");
        await context.Database.EnsureDeletedAsync();

        logger.LogInformation("Creating database...");
        await context.Database.MigrateAsync();

        logger.LogInformation("Seeding fresh data...");
        await DatabaseSeeder.SeedDatabaseAsync(host.Services, productCount, userCount);

        logger.LogInformation("Database reset completed successfully!");
        Log.Information("✓ Database reset completed!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database reset");
        Log.Fatal(ex, "✗ Database reset failed");
        throw;
    }
}