namespace Api.Middleware;

/// <summary>
/// Simple middleware to track cache status by monitoring response generation time
/// </summary>
public class CacheStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CacheStatusMiddleware> _logger;
    private static readonly Dictionary<string, DateTime> _requestTracker = new();
    private static readonly object _lock = new();

    public CacheStatusMiddleware(RequestDelegate next, ILogger<CacheStatusMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only track GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path + context.Request.QueryString;
        var isLikelyHit = false;

        // Track request patterns
        lock (_lock)
        {
            if (_requestTracker.TryGetValue(path, out var lastRequest))
            {
                var timeSinceLast = DateTime.UtcNow - lastRequest;
                // If the same resource was requested recently, subsequent request is likely cached
                isLikelyHit = timeSinceLast.TotalSeconds < 30;
            }
            _requestTracker[path] = DateTime.UtcNow;

            // Clean up old entries
            if (_requestTracker.Count > 100)
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var toRemove = _requestTracker.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
                foreach (var key in toRemove)
                {
                    _requestTracker.Remove(key);
                }
            }
        }

        // Start timing
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Call the next middleware
        await _next(context);

        stopwatch.Stop();

        // Only add header for successful responses
        if (context.Response.StatusCode == 200)
        {
            // Simple heuristic: if this is a repeated request and it was fast, it's likely cached
            var cacheStatus = isLikelyHit && stopwatch.ElapsedMilliseconds < 50 ? "HIT" : "MISS";

            // Add X-Cache header if not already present
            if (!context.Response.Headers.ContainsKey("X-Cache"))
            {
                context.Response.Headers["X-Cache"] = cacheStatus;
                _logger.LogDebug("Request to {Path} took {ElapsedMs}ms - Cache {Status}",
                    context.Request.Path, stopwatch.ElapsedMilliseconds, cacheStatus);
            }
        }
    }
}
