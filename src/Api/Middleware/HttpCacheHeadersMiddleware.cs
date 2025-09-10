using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace Api.Middleware;

/// <summary>
/// Middleware to add HTTP cache headers (Cache-Control, ETag, Last-Modified) to responses
/// </summary>
public class HttpCacheHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpCacheHeadersMiddleware> _logger;

    public HttpCacheHeadersMiddleware(RequestDelegate next, ILogger<HttpCacheHeadersMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check for conditional request headers
        var ifNoneMatch = context.Request.Headers.IfNoneMatch;
        var ifModifiedSince = context.Request.Headers.IfModifiedSince;

        // Track if this is the first request (for cache tracking)
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        // Buffer the response to calculate ETag
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Only add headers for successful responses
        if (context.Response.StatusCode == 200)
        {
            // Calculate ETag from response body
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            var etag = GenerateETag(responseContent);

            // Set Last-Modified to current time (in production, this would come from entity timestamps)
            var lastModified = DateTime.UtcNow;

            // Check conditional requests
            if (CheckIfNoneMatch(ifNoneMatch, etag) || CheckIfModifiedSince(ifModifiedSince, lastModified))
            {
                // Return 304 Not Modified
                context.Response.StatusCode = 304;
                context.Response.ContentLength = 0;
                context.Response.Body = originalBodyStream;
                
                // Add headers
                context.Response.Headers.ETag = $"\"{etag}\"";
                context.Response.Headers.LastModified = lastModified.ToString("R");
                
                _logger.LogDebug("Returning 304 Not Modified for {Path}", context.Request.Path);
                return;
            }

            // Add cache headers
            AddCacheHeaders(context, etag, lastModified);

            // Add custom cache status header
            // For now, we'll always report MISS since we're generating the response
            // The actual output caching happens at a different layer
            context.Response.Headers["X-Cache"] = "MISS";
        }

        // Copy the response body back to the original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }

    private void AddCacheHeaders(HttpContext context, string etag, DateTime lastModified)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        // Determine cache duration based on endpoint
        int maxAge;
        if (path.Contains("/people"))
            maxAge = 300; // 5 minutes
        else if (path.Contains("/roles"))
            maxAge = 3600; // 1 hour
        else if (path.Contains("/walls"))
            maxAge = 600; // 10 minutes
        else if (path.Contains("/windows"))
            maxAge = 600; // 10 minutes
        else
            maxAge = 300; // Default 5 minutes

        // Set Cache-Control header
        context.Response.Headers.CacheControl = $"public, max-age={maxAge}";
        
        // Set ETag header
        context.Response.Headers.ETag = $"\"{etag}\"";
        
        // Set Last-Modified header
        context.Response.Headers.LastModified = lastModified.ToString("R");
        
        // Set Vary header to indicate cache varies by these headers
        context.Response.Headers.Vary = "Accept, Accept-Encoding";
    }

    private string GenerateETag(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private bool CheckIfNoneMatch(StringValues ifNoneMatch, string currentEtag)
    {
        if (StringValues.IsNullOrEmpty(ifNoneMatch))
            return false;

        foreach (var etag in ifNoneMatch)
        {
            if (etag == "*" || etag.Trim('"') == currentEtag)
                return true;
        }

        return false;
    }

    private bool CheckIfModifiedSince(StringValues ifModifiedSince, DateTime lastModified)
    {
        if (StringValues.IsNullOrEmpty(ifModifiedSince))
            return false;

        if (DateTime.TryParse(ifModifiedSince, out var sinceDate))
        {
            // Remove milliseconds for comparison
            lastModified = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day,
                lastModified.Hour, lastModified.Minute, lastModified.Second, DateTimeKind.Utc);
            sinceDate = sinceDate.ToUniversalTime();
            sinceDate = new DateTime(sinceDate.Year, sinceDate.Month, sinceDate.Day,
                sinceDate.Hour, sinceDate.Minute, sinceDate.Second, DateTimeKind.Utc);

            return lastModified <= sinceDate;
        }

        return false;
    }
}
