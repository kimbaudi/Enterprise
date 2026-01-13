using EnterpriseApi.Application;
using EnterpriseApi.Infrastructure;
using EnterpriseApi.Infrastructure.Data;
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
    Log.Information("Starting Database Seeder...");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog - Use Services.AddSerilog() instead of builder.Host.UseSerilog()
    builder.Services.AddSerilog();

    // Configuration is already set up by CreateApplicationBuilder
    // But you can add additional configuration sources if needed
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();

    // Register Application and Infrastructure services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Build and run seeder
    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting data seeding...");

    // Seed with custom counts
    // Arguments: productCount, userCount
    await DatabaseSeeder.SeedDatabaseAsync(host.Services, productCount: 50000, userCount: 5000);

    logger.LogInformation("Data seeding completed successfully!");

    Log.Information("Database seeding completed!");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred during data seeding");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}