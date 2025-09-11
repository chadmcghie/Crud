using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Api.Middleware;

/// <summary>
/// Middleware that handles conditional GET requests (If-None-Match and If-Modified-Since)
/// This runs BEFORE output caching to properly handle 304 responses
/// </summary>
public class ConditionalRequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConditionalRequestMiddleware> _logger;

    public ConditionalRequestMiddleware(RequestDelegate next, ILogger<ConditionalRequestMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        
        // Only process GET and HEAD requests
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await _next(context);
            return;
        }

        // Store original body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Use a memory stream to capture the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware
            await _next(context);

            // Only process successful responses
            if (context.Response.StatusCode != 200)
            {
                // Copy the response back to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                return;
            }

            // Get the response content
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            // Generate ETag from response content
            var etag = GenerateETag(responseContent);
            
            // For simplicity, use current time as Last-Modified
            // In production, this should come from entity timestamps
            var lastModified = DateTime.UtcNow;

            // Check If-None-Match header
            var ifNoneMatch = request.Headers.IfNoneMatch;
            if (!StringValues.IsNullOrEmpty(ifNoneMatch) && CheckIfNoneMatch(ifNoneMatch, etag))
            {
                _logger.LogDebug("Returning 304 Not Modified for {Path} (ETag match)", request.Path);
                await SetNotModifiedResponse(context, originalBodyStream, etag, lastModified);
                return;
            }

            // Check If-Modified-Since header
            var ifModifiedSince = request.Headers.IfModifiedSince;
            if (!StringValues.IsNullOrEmpty(ifModifiedSince) && CheckIfModifiedSince(ifModifiedSince, lastModified))
            {
                _logger.LogDebug("Returning 304 Not Modified for {Path} (Not modified since)", request.Path);
                await SetNotModifiedResponse(context, originalBodyStream, etag, lastModified);
                return;
            }

            // Add cache headers to response
            context.Response.Headers.ETag = $"\"{etag}\"";
            context.Response.Headers.LastModified = lastModified.ToString("R");
            
            // Add Cache-Control if not already present
            if (!context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
            {
                context.Response.Headers.CacheControl = "public, max-age=60";
            }

            // Copy the response back to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static string GenerateETag(string content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool CheckIfNoneMatch(StringValues ifNoneMatch, string etag)
    {
        foreach (var value in ifNoneMatch)
        {
            if (string.IsNullOrEmpty(value))
                continue;

            if (value == "*")
                return true;

            // Remove quotes and weak indicator for comparison
            var cleanValue = value.Trim().Trim('"').TrimStart('W', '/').Trim('"');
            if (cleanValue == etag)
                return true;
        }
        return false;
    }

    private static bool CheckIfModifiedSince(StringValues ifModifiedSince, DateTime lastModified)
    {
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

    private static async Task SetNotModifiedResponse(HttpContext context, Stream originalStream, string etag, DateTime lastModified)
    {
        context.Response.Body = originalStream;
        context.Response.StatusCode = 304;
        context.Response.ContentLength = 0;
        
        // Add headers
        context.Response.Headers.ETag = $"\"{etag}\"";
        context.Response.Headers.LastModified = lastModified.ToString("R");
        
        // Clear content type
        context.Response.ContentType = null;
        
        // Ensure no content is written
        await context.Response.CompleteAsync();
    }
}
