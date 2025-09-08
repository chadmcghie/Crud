using System;

namespace Infrastructure.Services.Caching;

/// <summary>
/// Interface for cache configuration that provides TTL settings per entity type
/// </summary>
public interface ICacheConfiguration
{
    /// <summary>
    /// Gets cache options for a specific entity type
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <returns>Cache entry options</returns>
    CacheEntryOptions GetCacheOptions<TEntity>() where TEntity : class;

    /// <summary>
    /// Gets cache options for a specific entity type with custom TTL
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="ttl">Custom time-to-live</param>
    /// <returns>Cache entry options</returns>
    CacheEntryOptions GetCacheOptions<TEntity>(TimeSpan ttl) where TEntity : class;
}
