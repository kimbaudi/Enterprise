using Enterprise.Application.Common.Interfaces;
using Enterprise.Infrastructure.BackgroundServices;
using Enterprise.Infrastructure.Persistence;
using Enterprise.Infrastructure.Policies;
using Enterprise.Infrastructure.Repositories;
using Enterprise.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Resilience Policies (Polly Circuit Breaker, Retry, Timeout)
        services.AddSingleton<IResiliencePolicyProvider, ResiliencePolicyProvider>();

        // Add DbContext with connection resilience
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                    // Enable automatic retry on transient failures
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: new int[]
                        { 
                            // SQL Server transient error numbers
                            -2,     // Timeout
                            -1,     // Connection broken
                            1205,   // Deadlock victim
                            1222,   // Lock request timeout
                            2601,   // Duplicate key (can be transient)
                            2627,   // Unique constraint violation (can be transient)
                            4060,   // Cannot open database
                            40197,  // Service error processing request
                            40501,  // Service busy
                            40613,  // Database unavailable
                            49918,  // Cannot process request, not enough resources
                            49919,  // Cannot process create/update request, too many operations
                            49920   // Cannot process request, too many operations
                        });

                    // Command timeout for long-running queries
                    sqlServerOptions.CommandTimeout(30);
                }));

        // Add Repository Pattern
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Infrastructure Services
        services.AddScoped<IDateTime, DateTimeService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ITwoTierCacheService, TwoTierCacheService>();
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddSingleton<IBusinessMetricsService, BusinessMetricsService>();

        // Background Services
        services.AddHostedService<AuditLogProcessor>();

        return services;
    }
}
