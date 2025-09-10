using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Api.Filters;

/// <summary>
/// Action filter that handles conditional GET requests (If-None-Match and If-Modified-Since)
/// </summary>
public class ConditionalRequestFilter : IAsyncActionFilter
{
    private readonly ILogger<ConditionalRequestFilter> _logger;

    public ConditionalRequestFilter(ILogger<ConditionalRequestFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        // Only process GET and HEAD requests
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await next();
            return;
        }

        // Execute the action to get the result
        var executedContext = await next();

        // Only process successful responses
        if (executedContext.Result is not OkObjectResult okResult || okResult.Value == null)
        {
            return;
        }

        // Generate ETag from the response data
        var etag = GenerateETag(okResult.Value);

        // Determine Last-Modified date
        var lastModified = GetLastModified(okResult.Value);

        // Check If-None-Match header
        var ifNoneMatch = request.Headers.IfNoneMatch;
        if (!string.IsNullOrEmpty(ifNoneMatch))
        {
            if (CheckIfNoneMatch(ifNoneMatch, etag))
            {
                // Sanitize the path to prevent log injection attacks
                var sanitizedPath = request.Path.Value?.Replace("\r", "").Replace("\n", "");
                _logger.LogDebug("Returning 304 Not Modified for {Path} (ETag match)", sanitizedPath);
                SetNotModifiedResponse(context, etag, lastModified);
                return;
            }
        }

        // Check If-Modified-Since header
        var ifModifiedSinceHeader = request.Headers.IfModifiedSince;
        if (!string.IsNullOrEmpty(ifModifiedSinceHeader))
        {
            if (DateTimeOffset.TryParse(ifModifiedSinceHeader, out var ifModifiedSince))
            {
                if (lastModified.HasValue && lastModified.Value <= ifModifiedSince)
                {
                    // Sanitize the path to prevent log injection attacks
                    var sanitizedPath = request.Path.Value?.Replace("\r", "").Replace("\n", "");
                    _logger.LogDebug("Returning 304 Not Modified for {Path} (Not modified since {Date})",
                        sanitizedPath, ifModifiedSince);
                    SetNotModifiedResponse(context, etag, lastModified);
                    return;
                }
            }
        }

        // Add ETag and Last-Modified headers to response
        response.Headers.ETag = $"\"{etag}\"";
        if (lastModified.HasValue)
        {
            response.Headers.LastModified = lastModified.Value.ToString("R");
        }
    }

    private static string GenerateETag(object data)
    {
        // Serialize the object to JSON and compute hash
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static DateTimeOffset? GetLastModified(object data)
    {
        // Try to extract UpdatedAt or CreatedAt from the entity
        var type = data.GetType();

        // Check if it's a collection
        if (data is System.Collections.IEnumerable enumerable && !(data is string))
        {
            DateTimeOffset? latestDate = null;
            foreach (var item in enumerable)
            {
                var itemDate = GetEntityDate(item);
                if (itemDate.HasValue && (!latestDate.HasValue || itemDate.Value > latestDate.Value))
                {
                    latestDate = itemDate;
                }
            }
            return latestDate;
        }

        // Single entity
        return GetEntityDate(data);
    }

    private static DateTimeOffset? GetEntityDate(object entity)
    {
        if (entity == null)
            return null;

        var type = entity.GetType();

        // Try UpdatedAt first
        var updatedAtProp = type.GetProperty("UpdatedAt");
        if (updatedAtProp != null)
        {
            var value = updatedAtProp.GetValue(entity);
            if (value is DateTime dt && dt != default)
                return new DateTimeOffset(dt.ToUniversalTime());
            if (value is DateTimeOffset dto)
                return dto;
        }

        // Fall back to CreatedAt
        var createdAtProp = type.GetProperty("CreatedAt");
        if (createdAtProp != null)
        {
            var value = createdAtProp.GetValue(entity);
            if (value is DateTime dt && dt != default)
                return new DateTimeOffset(dt.ToUniversalTime());
            if (value is DateTimeOffset dto)
                return dto;
        }

        // Default to current time if no timestamp found
        return DateTimeOffset.UtcNow;
    }

    private static bool CheckIfNoneMatch(StringValues ifNoneMatch, string etag)
    {
        foreach (var value in ifNoneMatch)
        {
            if (value == "*")
                return true;

            // Remove quotes and weak indicator for comparison
            var cleanValue = value?.Trim().Trim('"').TrimStart('W', '/').Trim('"');
            if (cleanValue == etag)
                return true;
        }
        return false;
    }

    private static void SetNotModifiedResponse(ActionExecutingContext context, string etag, DateTimeOffset? lastModified)
    {
        var response = context.HttpContext.Response;

        // Set 304 status
        context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);

        // Add headers
        response.Headers.ETag = $"\"{etag}\"";
        if (lastModified.HasValue)
        {
            response.Headers.LastModified = lastModified.Value.ToString("R");
        }

        // Clear any content
        response.ContentLength = 0;
    }
}
