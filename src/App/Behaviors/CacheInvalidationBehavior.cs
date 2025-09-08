using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using App.Interfaces;
using App.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Behaviors;

public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(
        ICacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute the command first
        var response = await next();

        // Check for cache invalidation attributes
        var invalidationAttributes = request.GetType()
            .GetCustomAttributes<InvalidatesCacheAttribute>()
            .ToList();

        if (!invalidationAttributes.Any())
        {
            return response;
        }

        // Process cache invalidations
        foreach (var attribute in invalidationAttributes)
        {
            try
            {
                if (attribute.InvalidateAll)
                {
                    _logger.LogDebug("Invalidating all cache entries");
                    await _cacheService.RemoveByPatternAsync("*", cancellationToken);
                }
                else if (!string.IsNullOrEmpty(attribute.Pattern))
                {
                    _logger.LogDebug("Invalidating cache entries matching pattern: {Pattern}", attribute.Pattern);
                    await _cacheService.RemoveByPatternAsync(attribute.Pattern, cancellationToken);
                }
                else if (attribute.QueryTypes.Any())
                {
                    foreach (var queryType in attribute.QueryTypes)
                    {
                        var pattern = $"{queryType.Name}:*";
                        _logger.LogDebug("Invalidating cache entries for query type: {QueryType}", queryType.Name);
                        await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache");
                // Don't throw, continue processing
            }
        }

        return response;
    }
}
