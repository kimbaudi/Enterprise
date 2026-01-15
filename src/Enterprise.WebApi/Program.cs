using Asp.Versioning;
using Enterprise.Application;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Infrastructure;
using Enterprise.Infrastructure.BackgroundJobs;
using Enterprise.WebApi.Common;
using Enterprise.WebApi.Middleware;
using Enterprise.WebApi.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Enterprise Web API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add HttpContextAccessor for CurrentUserService
    builder.Services.AddHttpContextAccessor();

    // Add services to the container
    if (builder.Environment.IsEnvironment("Testing"))
    {
        // Use Newtonsoft.Json in test environment to work around WebApplicationFactory PipeWriter limitations
        // The in-memory test server's PipeWriter doesn't implement UnflushedBytes required for System.Text.Json async
        builder.Services.AddControllers().AddNewtonsoftJson();
    }
    else
    {
        builder.Services.AddControllers();
    }

    // Add Response Caching
    builder.Services.AddResponseCaching();

    // Add API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"),
            new MediaTypeApiVersionReader("ver"));
    }).AddMvc();

    builder.Services.AddEndpointsApiExplorer();

    // Configure Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Enterprise Web API",
            Version = "v1",
            Description = "An enterprise-ready ASP.NET Core Web API with clean architecture",
            Contact = new OpenApiContact
            {
                Name = "Your Company",
                Email = "contact@yourcompany.com"
            }
        });

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Add JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    // Ensure we have a valid key (especially important for Testing environment)
    if (string.IsNullOrWhiteSpace(secretKey))
    {
        secretKey = "YourSuperSecretKeyForJWTTokenGeneration123456789012";
    }

    var key = Encoding.ASCII.GetBytes(secretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = !builder.Environment.IsEnvironment("Testing"),
            ValidIssuer = jwtSettings["Issuer"] ?? "Enterprise",
            ValidateAudience = !builder.Environment.IsEnvironment("Testing"),
            ValidAudience = jwtSettings["Audience"] ?? "EnterpriseUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // Add OpenTelemetry (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: "Enterprise.WebApi", serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddConsoleExporter());
    }

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

        // Production-ready CORS policy (configure allowed origins as needed)
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "https://yourdomain.com" })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
    });

    // Add HSTS
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
    }

    // Add Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limit: 100 requests per minute per IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                }));

        // Specific policy for authentication endpoints - more restrictive
        options.AddFixedWindowLimiter("auth", options =>
        {
            options.PermitLimit = 10;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2;
        });

        // Policy for general API endpoints
        options.AddFixedWindowLimiter("api", options =>
        {
            options.PermitLimit = 60;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 5;
        });

        // Policy for expensive operations (bulk operations, exports)
        options.AddFixedWindowLimiter("expensive", options =>
        {
            options.PermitLimit = 10;
            options.Window = TimeSpan.FromMinutes(5);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 0;
        });

        // Per-user rate limiting (for authenticated requests)
        options.AddPolicy("perUser", httpContext =>
        {
            var username = httpContext.User.Identity?.Name ?? "anonymous";

            return RateLimitPartition.GetTokenBucketLimiter(username, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 50,
                AutoReplenishment = true
            });
        });

        // Sliding window for smoother rate limiting
        options.AddSlidingWindowLimiter("smooth", options =>
        {
            options.PermitLimit = 100;
            options.Window = TimeSpan.FromMinutes(1);
            options.SegmentsPerWindow = 6; // 10-second segments
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 10;
        });

        // Concurrency limiter (max simultaneous requests)
        options.AddConcurrencyLimiter("concurrent", options =>
        {
            options.PermitLimit = 50; // Max 50 concurrent requests
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 100;
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // Add Application and Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add CurrentUserService
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Skip production-only services in Testing environment
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        // Add Health Checks
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddHealthChecks()
            .AddSqlServer(connectionString ?? throw new InvalidOperationException("Connection string not found"),
                name: "database",
                tags: new[] { "db", "sql", "ready" });

        // Add Redis Distributed Cache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration["Redis:Configuration"];
            options.InstanceName = builder.Configuration["Redis:InstanceName"];
        });

        // Add Hangfire services
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        // Add the processing server as IHostedService
        builder.Services.AddHangfireServer();

        // Register background jobs (from Infrastructure layer)
        builder.Services.AddScoped<DatabaseCleanupJob>();
        builder.Services.AddScoped<ReportGenerationJob>();
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Web API v1");
            c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
        });

        // Hangfire Dashboard (development only for security)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });
    }

    // Add Response Caching Middleware
    app.UseResponseCaching();

    // Ensure uploads directory exists
    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }

    // Serve static files from uploads directory
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });

    // Add security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    });

    app.UseCors("AllowAll");

    // Add Rate Limiting Middleware (must be after routing, before auth)
    app.UseRateLimiter();
    app.UseMiddleware<RateLimitMiddleware>(); // Custom response formatting

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoints and Hangfire jobs (skip in Testing environment)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        // Health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.ToString()
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });
        app.MapHealthChecks("/health/live");

        // Configure recurring background jobs
        RecurringJob.AddOrUpdate<DatabaseCleanupJob>(
            "cleanup-expired-tokens",
            job => job.CleanupExpiredTokensAsync(),
            Cron.Daily); // Runs daily at midnight

        RecurringJob.AddOrUpdate<ReportGenerationJob>(
            "daily-summary-report",
            job => job.GenerateDailySummaryAsync(),
            Cron.Daily(8)); // Runs daily at 8 AM
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public for integration tests
public partial class Program { }
