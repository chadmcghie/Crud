namespace Api;

/// <summary>
/// Configuration settings for output caching
/// </summary>
public class CachingSettings
{
    /// <summary>
    /// Whether to use Redis for distributed caching
    /// </summary>
    public bool UseRedis { get; set; }

    /// <summary>
    /// Whether to enable output caching for API responses
    /// </summary>
    public bool UseOutputCaching { get; set; } = true;

    /// <summary>
    /// Cache duration for People endpoints in seconds
    /// </summary>
    public int PeopleCacheDurationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Cache duration for Roles endpoints in seconds
    /// </summary>
    public int RolesCacheDurationSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Cache duration for Walls endpoints in seconds
    /// </summary>
    public int WallsCacheDurationSeconds { get; set; } = 600; // 10 minutes

    /// <summary>
    /// Cache duration for Windows endpoints in seconds
    /// </summary>
    public int WindowsCacheDurationSeconds { get; set; } = 600; // 10 minutes
}
