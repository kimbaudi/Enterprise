namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Service for tracking custom business metrics
/// Provides domain-specific metrics for monitoring and alerting
/// </summary>
public interface IBusinessMetricsService
{
    // Order metrics
    void RecordOrderCreated(double orderValue);
    void RecordOrderFailed(string reason);
    void RecordOrderCompleted();

    // Authentication metrics
    void RecordLoginAttempt(bool success, string? username = null);
    void RecordLogout();
    void RecordRegistration();

    // Product metrics
    void RecordProductViewed(Guid productId);
    void RecordProductCreated();
    void RecordProductUpdated();

    // User metrics
    void RecordUserCreated();
    void RecordUserUpdated();

    // Shopping cart metrics
    void RecordCartSize(int itemCount);

    // Search metrics
    void RecordSearchResults(int resultCount, string searchTerm);

    // Calculated metrics
    double GetLoginSuccessRate();
    double GetAverageOrderValue();
}
