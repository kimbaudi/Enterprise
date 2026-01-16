using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Enterprise.Infrastructure.Services;

/// <summary>
/// Service for tracking custom business metrics
/// Provides domain-specific metrics for monitoring and alerting
/// </summary>
public class BusinessMetricsService : IBusinessMetricsService
{
    private readonly ILogger<BusinessMetricsService> _logger;
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _ordersCreated;
    private readonly Counter<long> _ordersFailed;
    private readonly Counter<long> _loginAttempts;
    private readonly Counter<long> _loginFailures;
    private readonly Counter<long> _registrations;
    private readonly Counter<long> _productsViewed;
    private readonly Counter<long> _productsCreated;
    private readonly Counter<long> _productsUpdated;
    private readonly Counter<long> _usersCreated;
    private readonly Counter<long> _usersUpdated;

    // Histograms for value distributions
    private readonly Histogram<double> _orderValue;
    private readonly Histogram<long> _cartSize;
    private readonly Histogram<long> _searchResultCount;

    // Gauges for current state (using ObservableGauge)
    private long _activeUsers;
    private long _activeOrders;

    public BusinessMetricsService(ILogger<BusinessMetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("Enterprise.Business", "1.0.0");

        // Initialize counters
        _ordersCreated = _meter.CreateCounter<long>(
            "business.orders.created",
            description: "Total number of orders created");

        _ordersFailed = _meter.CreateCounter<long>(
            "business.orders.failed",
            description: "Total number of failed order attempts");

        _loginAttempts = _meter.CreateCounter<long>(
            "business.auth.login_attempts",
            description: "Total number of login attempts");

        _loginFailures = _meter.CreateCounter<long>(
            "business.auth.login_failures",
            description: "Total number of failed login attempts");

        _registrations = _meter.CreateCounter<long>(
            "business.auth.registrations",
            description: "Total number of user registrations");

        _productsViewed = _meter.CreateCounter<long>(
            "business.products.viewed",
            description: "Total number of product views");

        _productsCreated = _meter.CreateCounter<long>(
            "business.products.created",
            description: "Total number of products created");

        _productsUpdated = _meter.CreateCounter<long>(
            "business.products.updated",
            description: "Total number of products updated");

        _usersCreated = _meter.CreateCounter<long>(
            "business.users.created",
            description: "Total number of users created");

        _usersUpdated = _meter.CreateCounter<long>(
            "business.users.updated",
            description: "Total number of users updated");

        // Initialize histograms
        _orderValue = _meter.CreateHistogram<double>(
            "business.orders.value",
            unit: "USD",
            description: "Distribution of order values");

        _cartSize = _meter.CreateHistogram<long>(
            "business.cart.size",
            unit: "items",
            description: "Distribution of cart sizes");

        _searchResultCount = _meter.CreateHistogram<long>(
            "business.search.results",
            unit: "results",
            description: "Distribution of search result counts");

        // Initialize observable gauges
        _meter.CreateObservableGauge(
            "business.users.active",
            () => Interlocked.Read(ref _activeUsers),
            description: "Current number of active users");

        _meter.CreateObservableGauge(
            "business.orders.active",
            () => Interlocked.Read(ref _activeOrders),
            description: "Current number of active orders");
    }

    // Order metrics
    public void RecordOrderCreated(double orderValue)
    {
        _ordersCreated.Add(1);
        _orderValue.Record(orderValue);
        Interlocked.Increment(ref _activeOrders);
        _logger.LogInformation("Order created with value: {OrderValue}", orderValue);
    }

    public void RecordOrderFailed(string reason)
    {
        _ordersFailed.Add(1);
        _logger.LogWarning("Order failed: {Reason}", reason);
    }

    public void RecordOrderCompleted()
    {
        Interlocked.Decrement(ref _activeOrders);
    }

    // Authentication metrics
    public void RecordLoginAttempt(bool success, string? username = null)
    {
        _loginAttempts.Add(1);
        if (!success)
        {
            _loginFailures.Add(1);
            _logger.LogWarning("Failed login attempt for user: {Username}", username ?? "unknown");
        }
        else
        {
            Interlocked.Increment(ref _activeUsers);
        }
    }

    public void RecordLogout()
    {
        Interlocked.Decrement(ref _activeUsers);
    }

    public void RecordRegistration()
    {
        _registrations.Add(1);
        _logger.LogInformation("New user registered");
    }

    // Product metrics
    public void RecordProductViewed(Guid productId)
    {
        _productsViewed.Add(1);
        _logger.LogDebug("Product viewed: {ProductId}", productId);
    }

    public void RecordProductCreated()
    {
        _productsCreated.Add(1);
    }

    public void RecordProductUpdated()
    {
        _productsUpdated.Add(1);
    }

    // User metrics
    public void RecordUserCreated()
    {
        _usersCreated.Add(1);
    }

    public void RecordUserUpdated()
    {
        _usersUpdated.Add(1);
    }

    // Shopping cart metrics
    public void RecordCartSize(int itemCount)
    {
        _cartSize.Record(itemCount);
    }

    // Search metrics
    public void RecordSearchResults(int resultCount, string searchTerm)
    {
        _searchResultCount.Record(resultCount);
        _logger.LogDebug("Search for '{SearchTerm}' returned {ResultCount} results",
            searchTerm, resultCount);
    }

    // Conversion metrics (calculated metrics)
    public double GetLoginSuccessRate()
    {
        // This would typically be calculated from a metrics backend
        // For now, return 0 as placeholder
        return 0.0;
    }

    public double GetAverageOrderValue()
    {
        // This would typically be calculated from a metrics backend
        return 0.0;
    }
}
