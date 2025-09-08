using App.Common.Attributes;
using App.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace App.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
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
        // Check if request is cacheable
        var cacheableAttribute = request.GetType().GetCustomAttribute<CacheableAttribute>();
        if (cacheableAttribute == null)
        {
            // Not cacheable, proceed without caching
            return await next();
        }

        var cacheKey = _keyGenerator.GenerateKey(request);
        
        try
        {
            // Try to get from cache
            var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {CacheKey}", cacheKey);
            // Continue without cache on error
        }

        // Execute the request
        var response = await next();

        // Cache the response if not null
        if (response != null)
        {
            try
            {
                var ttl = TimeSpan.FromSeconds(cacheableAttribute.DurationInSeconds);
                await _cacheService.SetAsync(cacheKey, response, ttl, cancellationToken);
                _logger.LogDebug("Cached response for key: {CacheKey} with TTL: {TTL}", cacheKey, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching response for key: {CacheKey}", cacheKey);
                // Don't throw, just log the error
            }
        }

        return response;
    }
}