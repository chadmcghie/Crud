using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly Dictionary<string, RateLimitRule> _rules;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;

        // Define rate limiting rules
        _rules = new Dictionary<string, RateLimitRule>
        {
            { "/api/auth/login", new RateLimitRule { MaxRequests = 5, WindowMinutes = 15 } },
            { "/api/auth/register", new RateLimitRule { MaxRequests = 3, WindowMinutes = 60 } },
            { "/api/auth/refresh", new RateLimitRule { MaxRequests = 10, WindowMinutes = 15 } },
            { "/api/database/reset", new RateLimitRule { MaxRequests = 10, WindowMinutes = 1 } }
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        if (path != null && _rules.TryGetValue(path, out var rule))
        {
            var clientId = GetClientId(context);
            var cacheKey = $"ratelimit_{path}_{clientId}";

            if (_cache.TryGetValue(cacheKey, out int requestCount))
            {
                if (requestCount >= rule.MaxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for {Path} from {ClientId}. Count: {Count}",
                        SanitizeForLogging(path), SanitizeForLogging(clientId), requestCount);

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers["Retry-After"] = (rule.WindowMinutes * 60).ToString();
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }

                _cache.Set(cacheKey, requestCount + 1, TimeSpan.FromMinutes(rule.WindowMinutes));
            }
            else
            {
                _cache.Set(cacheKey, 1, TimeSpan.FromMinutes(rule.WindowMinutes));
            }
        }

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Use IP address as client identifier
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove newlines, carriage returns, and other control characters to prevent log injection
        return input.Replace('\n', '_')
                   .Replace('\r', '_')
                   .Replace('\t', '_')
                   .Trim();
    }
}

public class RateLimitRule
{
    public int MaxRequests { get; set; }
    public int WindowMinutes { get; set; }
}
