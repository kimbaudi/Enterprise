using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Reflection;

namespace Enterprise.WebApi.HealthChecks;

/// <summary>
/// Enhanced health check that provides application metadata
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly DateTime _startTime;

    public ApplicationHealthCheck()
    {
        _startTime = DateTime.UtcNow;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var uptime = DateTime.UtcNow - _startTime;
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";

        var memoryUsed = GC.GetTotalMemory(false) / 1024.0 / 1024.0; // MB
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64 / 1024.0 / 1024.0; // MB

        var data = new Dictionary<string, object>
        {
            { "version", version },
            { "uptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m" },
            { "uptimeSeconds", (int)uptime.TotalSeconds },
            { "memoryUsageMB", Math.Round(memoryUsed, 2) },
            { "workingSetMB", Math.Round(workingSet, 2) },
            { "startTime", _startTime.ToString("O") },
            { "currentTime", DateTime.UtcNow.ToString("O") },
            { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }
        };

        return Task.FromResult(
            HealthCheckResult.Healthy(
                "Application is healthy",
                data));
    }
}

/// <summary>
/// Health check for external email service
/// </summary>
public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<EmailServiceHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public EmailServiceHealthCheck(
        ILogger<EmailServiceHealthCheck> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["EmailSettings:SendGridApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded(
                        "Email service API key not configured",
                        data: new Dictionary<string, object>
                        {
                            { "configured", false },
                            { "provider", "SendGrid" }
                        }));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Email service configured",
                    data: new Dictionary<string, object>
                    {
                        { "configured", true },
                        { "provider", "SendGrid" },
                        { "fromEmail", _configuration["EmailSettings:FromEmail"] ?? "N/A" }
                    }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email service health");
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Email service health check failed",
                    ex,
                    data: new Dictionary<string, object>
                    {
                        { "error", ex.Message }
                    }));
        }
    }
}
