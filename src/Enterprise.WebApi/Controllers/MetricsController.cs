using Asp.Versioning;
using Enterprise.Application.Common.Interfaces;
using Enterprise.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.WebApi.Controllers;

/// <summary>
/// Business metrics monitoring and reporting controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class MetricsController : ControllerBase
{
    private readonly IBusinessMetricsService _businessMetrics;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IBusinessMetricsService businessMetrics,
        ILogger<MetricsController> logger)
    {
        _businessMetrics = businessMetrics;
        _logger = logger;
    }

    /// <summary>
    /// Get business metrics summary
    /// </summary>
    [HttpGet("summary")]
    public ActionResult<ApiResponse<BusinessMetricsSummary>> GetSummary()
    {
        var summary = new BusinessMetricsSummary
        {
            LoginSuccessRate = _businessMetrics.GetLoginSuccessRate(),
            AverageOrderValue = _businessMetrics.GetAverageOrderValue(),
            Timestamp = DateTime.UtcNow
        };

        return Ok(new ApiResponse<BusinessMetricsSummary>(summary));
    }

    /// <summary>
    /// Test metrics recording (for demonstration)
    /// </summary>
    [HttpPost("test")]
    public ActionResult<ApiResponse<string>> TestMetrics()
    {
        // Simulate various business events
        _businessMetrics.RecordLoginAttempt(success: true, username: "test-user");
        _businessMetrics.RecordOrderCreated(orderValue: 99.99);
        _businessMetrics.RecordProductViewed(Guid.NewGuid());
        _businessMetrics.RecordSearchResults(resultCount: 42, searchTerm: "test-search");
        _businessMetrics.RecordCartSize(itemCount: 3);

        _logger.LogInformation("Test metrics recorded successfully");

        return Ok(new ApiResponse<string>("Test metrics recorded. Check your metrics backend (Prometheus, Grafana, etc.) to view them."));
    }

    /// <summary>
    /// Simulate order creation with metrics
    /// </summary>
    [HttpPost("simulate/order")]
    public ActionResult<ApiResponse<string>> SimulateOrder([FromBody] SimulateOrderRequest request)
    {
        if (request.Fail)
        {
            _businessMetrics.RecordOrderFailed(request.Reason ?? "Simulation");
            return Ok(new ApiResponse<string>("Order failure recorded"));
        }

        _businessMetrics.RecordOrderCreated(request.Value);
        _businessMetrics.RecordOrderCompleted();
        return Ok(new ApiResponse<string>($"Order created with value: ${request.Value}"));
    }

    /// <summary>
    /// Simulate authentication metrics
    /// </summary>
    [HttpPost("simulate/login")]
    public ActionResult<ApiResponse<string>> SimulateLogin([FromBody] SimulateLoginRequest request)
    {
        _businessMetrics.RecordLoginAttempt(request.Success, request.Username);

        if (request.Success)
        {
            return Ok(new ApiResponse<string>($"Successful login recorded for {request.Username}"));
        }

        return Ok(new ApiResponse<string>($"Failed login recorded for {request.Username}"));
    }
}

public class BusinessMetricsSummary
{
    public double LoginSuccessRate { get; set; }
    public double AverageOrderValue { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SimulateOrderRequest
{
    public double Value { get; set; } = 100.0;
    public bool Fail { get; set; }
    public string? Reason { get; set; }
}

public class SimulateLoginRequest
{
    public string Username { get; set; } = "test-user";
    public bool Success { get; set; } = true;
}
