using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using App.Behaviors;

namespace App.Services;

public interface ICacheKeyGenerator
{
    string GenerateKey<TRequest>(TRequest request, ClaimsPrincipal? user = null);
    string GenerateKey(Type requestType, object request, ClaimsPrincipal? user = null);
}

public class CacheKeyGenerator : ICacheKeyGenerator
{
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheKeyGenerator()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public string GenerateKey<TRequest>(TRequest request, ClaimsPrincipal? user = null)
    {
        return GenerateKey(typeof(TRequest), request!, user);
    }

    public string GenerateKey(Type requestType, object request, ClaimsPrincipal? user = null)
    {
        var sb = new StringBuilder();
        
        var cacheableAttr = requestType.GetCustomAttribute<CacheableAttribute>();
        
        if (!string.IsNullOrEmpty(cacheableAttr?.CustomCacheKeyPrefix))
        {
            sb.Append(cacheableAttr.CustomCacheKeyPrefix);
            sb.Append(':');
        }
        else
        {
            sb.Append(requestType.Name);
            sb.Append(':');
        }
        
        if (cacheableAttr?.VaryByUserId == true && user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                sb.Append("user:");
                sb.Append(userId);
                sb.Append(':');
            }
        }
        
        var requestJson = JsonSerializer.Serialize(request, requestType, _jsonOptions);
        var requestHash = ComputeHash(requestJson);
        sb.Append(requestHash);
        
        return sb.ToString();
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "")
            .Substring(0, 16); 
    }
}