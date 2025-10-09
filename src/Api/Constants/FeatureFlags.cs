namespace Api.Constants;

/// <summary>
/// Feature flag constants for Microsoft.FeatureManagement.
/// These constants represent the feature flags defined in the FeatureManagement configuration section.
/// </summary>
/// <remarks>
/// Feature flags are organized into three categories following Martin Fowler's Feature Toggle patterns:
/// - Ops Toggles: Long-lived operational controls for system behavior
/// - Release Toggles: Short-lived toggles for gradual feature rollout and migration
/// - Permission Toggles: Environment-based access controls
/// </remarks>
public static class FeatureFlags
{
    #region Ops Toggles (Long-lived operational controls)

    /// <summary>
    /// Controls Redis caching functionality.
    /// </summary>
    public const string Caching = "Caching";

    /// <summary>
    /// Controls response compression middleware.
    /// </summary>
    public const string Compression = "Compression";

    /// <summary>
    /// Controls rate limiting middleware.
    /// </summary>
    public const string RateLimiting = "RateLimiting";

    /// <summary>
    /// Controls OpenTelemetry observability features.
    /// </summary>
    public const string OpenTelemetry = "OpenTelemetry";

    /// <summary>
    /// Controls CORS policy configuration.
    /// </summary>
    public const string Cors = "Cors";

    /// <summary>
    /// Controls detailed health check responses.
    /// </summary>
    public const string HealthChecksDetailed = "HealthChecksDetailed";

    /// <summary>
    /// Controls dynamic log level adjustment at runtime.
    /// </summary>
    public const string DynamicLogLevel = "DynamicLogLevel";

    #endregion

    #region Release Toggles (Short-lived migration toggles)

    /// <summary>
    /// Controls migration to ASP.NET Core Identity authentication.
    /// </summary>
    public const string IdentityAuthentication = "IdentityAuthentication";

    /// <summary>
    /// Controls email service integration.
    /// </summary>
    public const string EmailService = "EmailService";

    /// <summary>
    /// Controls Polly resilience policies (retry, circuit breaker, timeout).
    /// </summary>
    public const string Resilience = "Resilience";

    #endregion

    #region Permission Toggles (Environment-based access controls)

    /// <summary>
    /// Controls Swagger/OpenAPI documentation endpoints.
    /// Enabled in Development/Testing, disabled in Production.
    /// </summary>
    public const string Swagger = "Swagger";

    /// <summary>
    /// Controls database seeding on application startup.
    /// Enabled in Development/Testing, disabled in Production.
    /// </summary>
    public const string DatabaseSeeding = "DatabaseSeeding";

    /// <summary>
    /// Controls detailed exception information in API responses.
    /// Enabled in Development/Testing, disabled in Production.
    /// </summary>
    public const string DetailedExceptions = "DetailedExceptions";

    /// <summary>
    /// Controls HTTP conditional request headers (ETag, If-Modified-Since).
    /// </summary>
    public const string ConditionalRequests = "ConditionalRequests";

    #endregion
}
