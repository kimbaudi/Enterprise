namespace Enterprise.WebApi.FeatureFlags;

/// <summary>
/// Enum of all available feature flags in the application.
/// Use these constants to check feature availability at runtime.
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Enable advanced audit logging with detailed tracking
    /// </summary>
    public const string EnhancedAuditLogging = "EnhancedAuditLogging";

    /// <summary>
    /// Enable two-factor authentication for all users
    /// </summary>
    public const string TwoFactorAuthentication = "TwoFactorAuthentication";

    /// <summary>
    /// Enable Redis-based output caching
    /// </summary>
    public const string OutputCaching = "OutputCaching";

    /// <summary>
    /// Enable response compression (Brotli/Gzip)
    /// </summary>
    public const string ResponseCompression = "ResponseCompression";

    /// <summary>
    /// Enable YARP reverse proxy
    /// </summary>
    public const string ReverseProxy = "ReverseProxy";

    /// <summary>
    /// Enable streaming responses for large datasets
    /// </summary>
    public const string StreamingResponses = "StreamingResponses";

    /// <summary>
    /// Enable Hangfire background job processing
    /// </summary>
    public const string BackgroundJobs = "BackgroundJobs";

    /// <summary>
    /// Enable beta features for testing
    /// </summary>
    public const string BetaFeatures = "BetaFeatures";

    /// <summary>
    /// Enable premium features for paid users
    /// </summary>
    public const string PremiumFeatures = "PremiumFeatures";
}
