using Asp.Versioning;
using Enterprise.Application;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Infrastructure;
using Enterprise.Infrastructure.BackgroundJobs;
using Enterprise.WebApi.BackgroundJobs;
using Enterprise.WebApi.Common;
using Enterprise.WebApi.FeatureFlags;
using Enterprise.WebApi.Middleware;
using Enterprise.WebApi.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement;
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

    // Add Response Compression (Gzip and Brotli)
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/json", "application/xml", "text/plain", "text/css", "text/javascript" });
    });

    builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Fastest;
    });

    builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Fastest;
    });

    // Add Response Caching (client-side)
    builder.Services.AddResponseCaching();

    // Add Output Caching (server-side with Redis)
    builder.Services.AddOutputCache(options =>
    {
        // Default policy: 60 seconds cache
        options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));

        // Products list: 2 minutes, vary by query string
        options.AddPolicy("products-list", builder => builder
            .Expire(TimeSpan.FromMinutes(2))
            .SetVaryByQuery("pageNumber", "pageSize", "searchTerm", "sortBy")
            .Tag("products"));

        // Product by ID: 5 minutes
        options.AddPolicy("product-details", builder => builder
            .Expire(TimeSpan.FromMinutes(5))
            .SetVaryByRouteValue("id")
            .Tag("products"));

        // Products by category: 3 minutes
        options.AddPolicy("products-category", builder => builder
            .Expire(TimeSpan.FromMinutes(3))
            .SetVaryByRouteValue("category")
            .SetVaryByQuery("pageNumber", "pageSize")
            .Tag("products"));

        // Search results: 1 minute (more dynamic)
        options.AddPolicy("products-search", builder => builder
            .Expire(TimeSpan.FromMinutes(1))
            .SetVaryByQuery("searchTerm", "minPrice", "maxPrice", "category", "pageNumber", "pageSize")
            .Tag("products"));

        // Users list: 30 seconds (more sensitive data)
        options.AddPolicy("users-list", builder => builder
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("pageNumber", "pageSize", "searchTerm", "isActive")
            .Tag("users"));

        // User details: 1 minute
        options.AddPolicy("user-details", builder => builder
            .Expire(TimeSpan.FromMinutes(1))
            .SetVaryByRouteValue("id")
            .Tag("users"));
    });

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

    // Add YARP Reverse Proxy (API Gateway)
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddConfigFilter<YarpConfigFilter>();

    // Configure Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Enterprise Web API",
            Version = "v1",
            Description = @"An enterprise-ready ASP.NET Core Web API with:
- Clean Architecture & CQRS Pattern with MediatR
- JWT Authentication & Two-Factor Authentication
- Redis Distributed Caching & Output Caching
- Response Compression (Brotli & Gzip)
- Rate Limiting & Circuit Breaker Patterns
- Audit Logging & Soft Delete Support
- Background Jobs with Hangfire
- Feature Flags for A/B Testing
- OpenTelemetry Observability
- YARP Reverse Proxy
- Comprehensive Unit & Integration Tests",
            Contact = new OpenApiContact
            {
                Name = "Enterprise Development Team",
                Email = "api-support@enterprise.com",
                Url = new Uri("https://enterprise.com/api-docs")
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            },
            TermsOfService = new Uri("https://enterprise.com/terms")
        });

        // Include XML documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }

        // Enable annotations for better documentation
        options.EnableAnnotations();

        // Add operation filters for enhanced documentation  
        options.OperationFilter<Swashbuckle.AspNetCore.Filters.AppendAuthorizeToSummaryOperationFilter>();
        options.OperationFilter<Swashbuckle.AspNetCore.Filters.SecurityRequirementsOperationFilter>();

        // Order actions by HTTP method
        options.OrderActionsBy((apiDesc) => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");

        // Use full schema names to avoid conflicts
        options.CustomSchemaIds(type => type.FullName);

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme. 
                          Enter 'Bearer' [space] and then your token in the text input below.
                          Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
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
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header
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
            .WithMetrics(metrics => metrics
                .AddMeter("Enterprise.WebApi")
                .AddMeter("System.Net.Http")
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel"))
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

    // Add Feature Management
    builder.Services.AddFeatureManagement()
        .AddFeatureFilter<RoleFeatureFilter>()
        .AddFeatureFilter<PercentageFeatureFilter>();

    // Add CurrentUserService
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Skip production-only services in Testing environment
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        // Add Health Checks
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var redisConfiguration = builder.Configuration["Redis:Configuration"];

        builder.Services.AddHealthChecks()
            .AddSqlServer(
                connectionString ?? throw new InvalidOperationException("Connection string not found"),
                name: "sql-server",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" })
            .AddRedis(
                redisConfiguration ?? "localhost:6379",
                name: "redis-cache",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "cache", "redis", "ready" })
            .AddHangfire(options =>
            {
                options.MinimumAvailableServers = 1;
            },
                name: "hangfire",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "jobs", "hangfire", "ready" });

        // Add Redis Distributed Cache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration["Redis:Configuration"];
            options.InstanceName = builder.Configuration["Redis:InstanceName"];
        });

        // Add Hangfire services
        builder.Services.AddHangfire((serviceProvider, configuration) => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseFilter(new HangfireMetricsFilter(
                serviceProvider.GetRequiredService<IMetricsService>(),
                serviceProvider.GetRequiredService<ILogger<HangfireMetricsFilter>>()))
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
    else
    {
        // Testing environment: use in-memory distributed cache
        builder.Services.AddDistributedMemoryCache();
    }

    var app = builder.Build();

    // Determine if we're running in a testing environment
    var isTestingEnvironment = app.Environment.IsEnvironment("Testing");

    // Configure the HTTP request pipeline
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Metrics tracking (skip in Testing environment)
    if (!isTestingEnvironment)
    {
        app.UseMiddleware<MetricsMiddleware>();
    }

    // Request/Response logging (skip in Testing environment for cleaner test output)
    if (!isTestingEnvironment)
    {
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Response compression (must be early in pipeline) - controlled by feature flag
    var featureManager = app.Services.GetRequiredService<IFeatureManager>();
    if (await featureManager.IsEnabledAsync("ResponseCompression"))
    {
        app.UseResponseCompression();
    }

    // Output caching (must be before response caching)
    app.UseOutputCache();

    // Cache eviction middleware (after output caching)
    app.UseMiddleware<CacheEvictionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Web API v1");
            c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
            c.DocumentTitle = "Enterprise Web API - Documentation";
            c.DefaultModelsExpandDepth(2);
            c.DefaultModelExpandDepth(2);
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
            c.EnableValidator();
            c.SupportedSubmitMethods(Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete);

            // Enable "Try it out" for all operations by default
            c.ConfigObject.AdditionalItems["tryItOutEnabled"] = true;

            // Persist authorization data
            c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
        });
    }

    // Hangfire Dashboard (available in all non-Testing environments)
    // Requires Admin role for access
    if (!isTestingEnvironment)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            StatsPollingInterval = 2000, // Update stats every 2 seconds
            DisplayStorageConnectionString = false // Hide connection string for security
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

    // YARP Reverse Proxy - Map gateway routes
    app.MapReverseProxy();

    // Health check endpoints and Hangfire jobs (skip in Testing environment)
    if (!isTestingEnvironment)
    {
        // Health check endpoints
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description ?? "No description",
                        duration = e.Value.Duration.TotalMilliseconds,
                        tags = e.Value.Tags
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description ?? "No description",
                        duration = e.Value.Duration.TotalMilliseconds,
                        tags = e.Value.Tags
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false, // Exclude all checks, just return 200 if app is running
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"status\":\"Healthy\"}");
            }
        });

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
